using System;
using System.Threading;
using System.Threading.Tasks;
using Can_Bootloader_GUI.Driver;
using Can_Bootloader_GUI.Models;
using Can_Bootloader_GUI.Parsers;

namespace Can_Bootloader_GUI.Protocol
{
    /// <summary>
    /// CAN Bootloader Protocol State Machine for STM32 targets.
    /// Handles Erase (0x700 / 0x703), Chunked Data Transfer (0x701 / 0x704),
    /// Finish & Verification (0x702 / 0x705), and Device Error reporting (0x706).
    /// </summary>
    public class BootloaderProtocol
    {
        // CAN IDs
        public const uint CAN_ID_ERASE_CMD      = 0x700; // PC -> Target (DLC 5)
        public const uint CAN_ID_DATA_TRANSFER  = 0x701; // PC -> Target (DLC <= 8)
        public const uint CAN_ID_FINISH_CMD     = 0x702; // PC -> Target (DLC 1)
        public const uint CAN_ID_ERASE_ACK      = 0x703; // Target -> PC (DLC 1)
        public const uint CAN_ID_WRITE_ACK      = 0x704; // Target -> PC (DLC 4)
        public const uint CAN_ID_CRC_RESPONSE   = 0x705; // Target -> PC (DLC 4)
        public const uint CAN_ID_ERROR_REPORT   = 0x706; // Target -> PC (DLC 2)
        public const uint CAN_ID_TEST_LED_CMD   = 0x710; // PC -> Target (DLC 2)
        public const uint CAN_ID_TELEMETRY      = 0x720; // Target -> PC (DLC 8)

        private readonly WaveshareUsbCanDriver _driver;
        private TaskCompletionSource<bool>? _eraseAckTcs;
        private TaskCompletionSource<uint>? _crcAckTcs;
        private volatile bool _isFlashing = false;

        public event Action<int, int>? ProgressChanged; // writtenBytes, totalBytes
        public event Action<string>? StatusChanged;
        public event Action<string>? FlashCompleted;
        public event Action<string>? FlashFailed;

        public bool IsFlashing => _isFlashing;

        public BootloaderProtocol(WaveshareUsbCanDriver driver)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _driver.MessageReceived += OnCanMessageReceived;
        }

        private void OnCanMessageReceived(CanMessage msg)
        {
            switch (msg.Id)
            {
                case CAN_ID_ERASE_ACK:
                    if (msg.Data.Length >= 1)
                    {
                        bool success = (msg.Data[0] == 0x01);
                        _eraseAckTcs?.TrySetResult(success);
                    }
                    break;

                case CAN_ID_WRITE_ACK:
                    if (msg.Data.Length >= 4)
                    {
                        uint writtenBytes = BitConverter.ToUInt32(msg.Data, 0);
                        ProgressChanged?.Invoke((int)writtenBytes, 0);
                    }
                    break;

                case CAN_ID_CRC_RESPONSE:
                    if (msg.Data.Length >= 4)
                    {
                        uint targetCrc = BitConverter.ToUInt32(msg.Data, 0);
                        _crcAckTcs?.TrySetResult(targetCrc);
                    }
                    break;

                case CAN_ID_ERROR_REPORT:
                    if (msg.Data.Length >= 1)
                    {
                        byte errCode = msg.Data[0];
                        string errDesc = errCode switch
                        {
                            0x01 => "Flash Erase Failure",
                            0x02 => "Flash Write / Programming Failure",
                            0x03 => "Firmware Size Overflow",
                            0x04 => "Packet Sequence Out-of-Order",
                            _ => $"Unknown Target Error Code: 0x{errCode:X2}"
                        };

                        StatusChanged?.Invoke($"Target Error: {errDesc}");
                        _eraseAckTcs?.TrySetException(new InvalidOperationException($"Target reported: {errDesc}"));
                        _crcAckTcs?.TrySetException(new InvalidOperationException($"Target reported: {errDesc}"));
                        FlashFailed?.Invoke($"Target Error: {errDesc}");
                    }
                    break;
            }
        }

        /// <summary>
        /// Executes full async firmware flash sequence.
        /// </summary>
        public async Task<bool> FlashFirmwareAsync(FirmwareInfo firmware, int packetDelayMs, CancellationToken ct)
        {
            if (_isFlashing)
                throw new InvalidOperationException("Flash operation already in progress.");

            if (!_driver.IsOpen)
                throw new InvalidOperationException("CAN driver is not connected.");

            _isFlashing = true;
            try
            {
                StatusChanged?.Invoke("Initiating flash erase...");

                // 1. Send Erase Command: ID 0x700, DLC 5: Data[0] = 0x01, Data[1..4] = Total firmware size (uint32 LE)
                uint totalBytes = (uint)firmware.TotalBytes;
                byte[] eraseData = new byte[5];
                eraseData[0] = 0x01;
                Array.Copy(BitConverter.GetBytes(totalBytes), 0, eraseData, 1, 4);

                _eraseAckTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                _driver.SendCanMessage(CAN_ID_ERASE_CMD, eraseData, $"Erase Command (Size: {totalBytes} bytes)");

                // Wait for Erase ACK (0x703) with 4-second timeout
                using (var eraseTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(4)))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, eraseTimeoutCts.Token))
                {
                    try
                    {
                        using (linkedCts.Token.Register(() => _eraseAckTcs.TrySetCanceled()))
                        {
                            bool eraseOk = await _eraseAckTcs.Task;
                            if (!eraseOk)
                            {
                                FlashFailed?.Invoke("Target rejected erase command (ACK = 0x00).");
                                return false;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (eraseTimeoutCts.IsCancellationRequested)
                        {
                            FlashFailed?.Invoke("Erase command timed out (4s exceeded, no ACK from target).");
                            return false;
                        }
                        FlashFailed?.Invoke("Operation cancelled by user.");
                        return false;
                    }
                }

                StatusChanged?.Invoke("Flash erased successfully. Transferring firmware packets...");

                // 2. Data Transfer (0x701) in 8-byte chunks
                byte[] buffer = firmware.RawData;
                int totalPackets = firmware.TotalPackets;
                int sentBytes = 0;

                for (int i = 0; i < totalBytes; i += 8)
                {
                    ct.ThrowIfCancellationRequested();

                    int chunkSize = Math.Min(8, (int)totalBytes - i);
                    byte[] chunk = new byte[chunkSize];
                    Array.Copy(buffer, i, chunk, 0, chunkSize);

                    _driver.SendCanMessage(CAN_ID_DATA_TRANSFER, chunk, $"Firmware Data [{i}/{totalBytes}]");
                    sentBytes += chunkSize;

                    // Trigger progress indication
                    ProgressChanged?.Invoke(sentBytes, (int)totalBytes);

                    if (packetDelayMs > 0)
                    {
                        await Task.Delay(packetDelayMs, ct);
                    }
                }

                StatusChanged?.Invoke("Firmware data sent. Requesting verification and application jump...");

                // 3. Send Transfer Completed Command: ID 0x702, DLC 1, Data[0] = 0x01
                _crcAckTcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
                _driver.SendCanMessage(CAN_ID_FINISH_CMD, new byte[] { 0x01 }, "Finish Transfer Command");

                // Wait for CRC32 Response: ID 0x705 with 3-second timeout
                using (var crcTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, crcTimeoutCts.Token))
                {
                    try
                    {
                        using (linkedCts.Token.Register(() => _crcAckTcs.TrySetCanceled()))
                        {
                            uint targetCrc = await _crcAckTcs.Task;
                            uint calculatedCrc = firmware.CalculatedCrc32;

                            if (targetCrc == calculatedCrc)
                            {
                                FlashCompleted?.Invoke($"Firmware verified successfully! CRC32: 0x{targetCrc:X8} matches.");
                                return true;
                            }
                            else
                            {
                                FlashFailed?.Invoke($"CRC32 Mismatch! Target: 0x{targetCrc:X8}, Host: 0x{calculatedCrc:X8}.");
                                return false;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (crcTimeoutCts.IsCancellationRequested)
                        {
                            FlashFailed?.Invoke("CRC verification timed out (no response on ID 0x705).");
                            return false;
                        }
                        FlashFailed?.Invoke("Operation cancelled by user.");
                        return false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                FlashFailed?.Invoke("Programming aborted by user.");
                return false;
            }
            catch (Exception ex)
            {
                FlashFailed?.Invoke($"Error during flash: {ex.Message}");
                return false;
            }
            finally
            {
                _isFlashing = false;
            }
        }

        /// <summary>
        /// Sends LED Blink test command (ID 0x710, DLC 2, period in uint16 LE milliseconds)
        /// </summary>
        public bool SendBlinkTest(ushort periodMs)
        {
            if (!_driver.IsOpen) return false;
            byte[] data = BitConverter.GetBytes(periodMs);
            return _driver.SendCanMessage(CAN_ID_TEST_LED_CMD, data, $"LED Blink Test: {periodMs} ms");
        }
    }
}
