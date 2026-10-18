using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Threading;
using Can_Bootloader_GUI.Models;

namespace Can_Bootloader_GUI.Driver
{
    /// <summary>
    /// Pure C# Driver for Waveshare USB-CAN-A (USB-Serial CAN Adapter).
    /// Features:
    /// - Serial baudrate 2,000,000 bps 8N1 default.
    /// - 20-byte Configuration frame generation for CAN bus baudrate.
    /// - Standard CAN 2.0A frame framing & streaming deframer (supports both 20-byte fixed and variable length framing).
    /// - Thread-safe transmit and receive event dispatching.
    /// </summary>
    public class WaveshareUsbCanDriver : IDisposable
    {
        private SerialPort? _serialPort;
        private readonly object _txLock = new object();
        private readonly byte[] _rxRingBuffer = new byte[8192];
        private int _rxBufferCount = 0;
        private readonly object _rxLock = new object();

        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;
        public string PortName => _serialPort?.PortName ?? string.Empty;

        public event Action<CanMessage>? MessageReceived;
        public event Action<CanMessage>? MessageTransmitted;
        public event Action<string>? LogMessage;
        public event Action<Exception>? CommunicationError;

        /// <summary>
        /// Baudrate code map for Waveshare USB-CAN-A
        /// </summary>
        public enum CanBaudrate
        {
            Baud1Mbps = 0x01,
            Baud800kbps = 0x02,
            Baud500kbps = 0x03,
            Baud250kbps = 0x04,
            Baud125kbps = 0x05,
            Baud100kbps = 0x06,
            Baud50kbps = 0x07,
            Baud20kbps = 0x08,
            Baud10kbps = 0x09,
            Baud5kbps = 0x0A
        }

        public WaveshareUsbCanDriver()
        {
        }

        /// <summary>
        /// Connects to the Waveshare USB-CAN-A adapter and configures the CAN controller.
        /// </summary>
        public bool Connect(string portName, int serialBaudRate = 2000000, CanBaudrate canBaud = CanBaudrate.Baud1Mbps)
        {
            Disconnect();

            try
            {
                _serialPort = new SerialPort(portName, serialBaudRate, Parity.None, 8, StopBits.One)
                {
                    ReadBufferSize = 65536,
                    WriteBufferSize = 65536,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    DtrEnable = true,
                    RtsEnable = true
                };

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();

                lock (_rxLock)
                {
                    _rxBufferCount = 0;
                }

                // Send 20-byte configuration frame to set CAN baudrate
                SendConfigFrame(canBaud);
                LogMessage?.Invoke($"Connected to {portName} at {serialBaudRate} bps. CAN Baudrate set to {canBaud}.");
                return true;
            }
            catch (Exception ex)
            {
                Disconnect();
                CommunicationError?.Invoke(ex);
                return false;
            }
        }

        /// <summary>
        /// Sends 20-byte hardware configuration frame to Waveshare USB-CAN-A
        /// Structure:
        /// [0]: 0xAA
        /// [1]: 0x55
        /// [2]: 0x12 (Config type)
        /// [3]: CAN Baudrate code (0x01 = 1M, 0x02 = 800k, 0x03 = 500k, 0x04 = 250k etc.)
        /// [4]: 0x01 (Standard CAN 2.0A frame format)
        /// [5..8]: Filter ID (0x00 0x00 0x00 0x00 = Accept all)
        /// [9..12]: Mask (0x00 0x00 0x00 0x00 = Accept all)
        /// [13]: 0x01 (CAN mode: 0=Normal, 1=Loopback/Normal depending on firmware, standard normal is 0x00/0x01)
        /// [14..18]: Reserved 0x00
        /// [19]: 0x55 (End marker)
        /// </summary>
        public void SendConfigFrame(CanBaudrate canBaud)
        {
            byte[] cfg = new byte[20];
            cfg[0] = 0xAA;
            cfg[1] = 0x55;
            cfg[2] = 0x12;
            cfg[3] = (byte)canBaud;
            cfg[4] = 0x01; // Standard frame 2.0A
            cfg[5] = 0x00; // Filter ID B0
            cfg[6] = 0x00; // Filter ID B1
            cfg[7] = 0x00; // Filter ID B2
            cfg[8] = 0x00; // Filter ID B3
            cfg[9] = 0x00; // Mask B0
            cfg[10] = 0x00; // Mask B1
            cfg[11] = 0x00; // Mask B2
            cfg[12] = 0x00; // Mask B3
            cfg[13] = 0x00; // Mode: Normal
            cfg[14] = 0x00;
            cfg[15] = 0x00;
            cfg[16] = 0x00;
            cfg[17] = 0x00;
            cfg[18] = 0x00;
            cfg[19] = 0x55;

            lock (_txLock)
            {
                if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.Write(cfg, 0, cfg.Length);
                }
            }
            Thread.Sleep(50); // Allow hardware to initialize registers
        }

        /// <summary>
        /// Transmits a Standard CAN 2.0A frame via serial.
        /// Waveshare Standard Frame Format (Variable length or 20-byte fixed):
        /// [0]: 0xAA (Frame Header)
        /// [1]: 0xC0 | DLC (0xC0 indicates Standard Frame, Bit 7: Format, Bit 6: RTR, Bits 3..0: DLC)
        /// [2]: CAN ID Low Byte (ID & 0xFF)
        /// [3]: CAN ID High Byte ((ID >> 8) & 0x07 for 11-bit standard ID)
        /// [4..4+DLC-1]: Data Bytes (0 to 8 bytes)
        /// [Last]: 0x55 (Frame Tail)
        /// </summary>
        public bool SendCanMessage(uint id, byte[]? data, string description = "")
        {
            if (!IsOpen || _serialPort == null)
                return false;

            byte dlc = (byte)(data != null ? Math.Min(data.Length, 8) : 0);
            int frameLen = 5 + dlc; // 0xAA + Type/DLC + ID_L + ID_H + Data[dlc] + 0x55
            byte[] frame = new byte[frameLen];

            frame[0] = 0xAA;
            frame[1] = (byte)(0xC0 | (dlc & 0x0F));
            frame[2] = (byte)(id & 0xFF);
            frame[3] = (byte)((id >> 8) & 0x07);

            if (dlc > 0 && data != null)
            {
                Array.Copy(data, 0, frame, 4, dlc);
            }
            frame[frameLen - 1] = 0x55;

            try
            {
                lock (_txLock)
                {
                    _serialPort.Write(frame, 0, frame.Length);
                }

                var msg = new CanMessage(id, data ?? Array.Empty<byte>(), isTx: true, description);
                MessageTransmitted?.Invoke(msg);
                return true;
            }
            catch (Exception ex)
            {
                CommunicationError?.Invoke(ex);
                return false;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return;

            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead <= 0) return;

                byte[] temp = new byte[bytesToRead];
                int read = _serialPort.Read(temp, 0, bytesToRead);

                lock (_rxLock)
                {
                    if (_rxBufferCount + read > _rxRingBuffer.Length)
                    {
                        // Buffer overflow guard: shift or reset
                        _rxBufferCount = 0;
                    }

                    Array.Copy(temp, 0, _rxRingBuffer, _rxBufferCount, read);
                    _rxBufferCount += read;

                    ParseRxBuffer();
                }
            }
            catch (Exception ex)
            {
                CommunicationError?.Invoke(ex);
            }
        }

        /// <summary>
        /// Sliding window parser supporting both variable-length frames and fixed 20-byte frames.
        /// </summary>
        private void ParseRxBuffer()
        {
            int index = 0;
            while (index < _rxBufferCount)
            {
                // Look for start marker 0xAA
                if (_rxRingBuffer[index] != 0xAA)
                {
                    index++;
                    continue;
                }

                // Check if enough bytes are left for at least header inspection (5 bytes min)
                int remaining = _rxBufferCount - index;
                if (remaining < 5)
                    break;

                // Check for 20-byte Config response or Fixed Frame
                // Fixed frame: if remaining >= 20 and _rxRingBuffer[index + 19] == 0x55
                // Check standard frame: _rxRingBuffer[index + 1] contains frame descriptor
                byte desc = _rxRingBuffer[index + 1];

                // If it's a fixed 20-byte heartbeat/config response
                if (desc == 0x55 && index + 2 < _rxBufferCount && _rxRingBuffer[index + 2] == 0x12)
                {
                    if (remaining >= 20 && _rxRingBuffer[index + 19] == 0x55)
                    {
                        index += 20;
                        continue;
                    }
                    else if (remaining < 20)
                    {
                        break; // Wait for full frame
                    }
                }

                // Standard Data Frame check
                byte dlc = (byte)(desc & 0x0F);
                if (dlc > 8)
                {
                    // Invalid DLC, advance index by 1 to resynchronize
                    index++;
                    continue;
                }

                int variableLength = 5 + dlc; // 0xAA, desc, id_l, id_h, [data...], 0x55

                // Test if variable length frame matches
                if (remaining >= variableLength && _rxRingBuffer[index + variableLength - 1] == 0x55)
                {
                    // Valid variable length frame
                    byte idL = _rxRingBuffer[index + 2];
                    byte idH = _rxRingBuffer[index + 3];
                    uint canId = (uint)(idL | ((idH & 0x07) << 8));

                    byte[] data = new byte[dlc];
                    if (dlc > 0)
                    {
                        Array.Copy(_rxRingBuffer, index + 4, data, 0, dlc);
                    }

                    var msg = new CanMessage(canId, data, isTx: false);
                    MessageReceived?.Invoke(msg);

                    index += variableLength;
                    continue;
                }
                // Test if fixed 20-byte frame format matches
                else if (remaining >= 20 && _rxRingBuffer[index + 19] == 0x55)
                {
                    byte idL = _rxRingBuffer[index + 2];
                    byte idH = _rxRingBuffer[index + 3];
                    uint canId = (uint)(idL | ((idH & 0x07) << 8));

                    byte[] data = new byte[dlc];
                    if (dlc > 0)
                    {
                        Array.Copy(_rxRingBuffer, index + 4, data, 0, dlc);
                    }

                    var msg = new CanMessage(canId, data, isTx: false);
                    MessageReceived?.Invoke(msg);

                    index += 20;
                    continue;
                }
                else
                {
                    // Incomplete packet in buffer; wait for next read unless remaining is unusually large
                    if (remaining > 64)
                    {
                        index++; // Corrupt start byte, resynchronize
                        continue;
                    }
                    break;
                }
            }

            // Compact leftover bytes in the buffer
            if (index > 0)
            {
                int remainingBytes = _rxBufferCount - index;
                if (remainingBytes > 0)
                {
                    Array.Copy(_rxRingBuffer, index, _rxRingBuffer, 0, remainingBytes);
                }
                _rxBufferCount = remainingBytes;
            }
        }

        public void Disconnect()
        {
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DataReceived -= OnDataReceived;
                        _serialPort.DiscardInBuffer();
                        _serialPort.DiscardOutBuffer();
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                    _serialPort = null;
                }
                LogMessage?.Invoke("Serial port closed.");
            }
            catch (Exception ex)
            {
                CommunicationError?.Invoke(ex);
            }
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
