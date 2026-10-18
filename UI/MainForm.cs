using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Can_Bootloader_GUI.Driver;
using Can_Bootloader_GUI.Models;
using Can_Bootloader_GUI.Parsers;
using Can_Bootloader_GUI.Protocol;

namespace Can_Bootloader_GUI.UI
{
    public partial class MainForm : Form
    {
        private readonly WaveshareUsbCanDriver _driver;
        private readonly BootloaderProtocol _bootloader;
        private FirmwareInfo? _loadedFirmware;
        private CancellationTokenSource? _flashCts;

        // Diagnostic traffic log buffer
        private readonly ConcurrentQueue<CanMessage> _logQueue = new ConcurrentQueue<CanMessage>();
        private readonly List<CanMessage> _allLogs = new List<CanMessage>();
        private readonly System.Windows.Forms.Timer _uiRefreshTimer;
        private const int MAX_LOG_ROWS = 5000;

        public MainForm()
        {
            InitializeComponent();

            _driver = new WaveshareUsbCanDriver();
            _bootloader = new BootloaderProtocol(_driver);

            // Wire driver events
            _driver.MessageTransmitted += OnMessageTransmitted;
            _driver.MessageReceived += OnMessageReceived;
            _driver.CommunicationError += OnCommunicationError;

            // Wire bootloader protocol events
            _bootloader.ProgressChanged += OnFlashProgressChanged;
            _bootloader.StatusChanged += OnFlashStatusChanged;
            _bootloader.FlashCompleted += OnFlashCompleted;
            _bootloader.FlashFailed += OnFlashFailed;

            // Wire UI events
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
            this.btnRefreshPorts.Click += (s, e) => ScanAvailableComPorts();
            this.btnConnect.Click += BtnConnect_Click;
            this.btnSelectFile.Click += BtnSelectFile_Click;
            this.btnFlash.Click += BtnFlash_Click;
            this.btnCancel.Click += BtnCancel_Click;
            this.btnSendBlink.Click += BtnSendBlink_Click;
            this.btnClearLogs.Click += (s, e) => ClearTrafficLogs();
            this.btnExportCsv.Click += BtnExportCsv_Click;

            // Smooth UI Refresh Timer for high throughput DataGridView logging
            _uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _uiRefreshTimer.Tick += OnUiRefreshTimerTick;
            _uiRefreshTimer.Start();
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            PopulateBaudRates();
            ScanAvailableComPorts();
            UpdateConnectionUiState(false);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _flashCts?.Cancel();
            _uiRefreshTimer.Stop();
            _driver.Disconnect();
            _driver.Dispose();
        }

        private void PopulateBaudRates()
        {
            // USB Serial Baud Rates
            cmbSerialBaud.Items.Clear();
            cmbSerialBaud.Items.AddRange(new object[] { "2000000", "1000000", "921600", "460800", "115200" });
            cmbSerialBaud.SelectedIndex = 0; // Default 2 Mbps

            // CAN Bus Baud Rates
            cmbCanBaud.Items.Clear();
            cmbCanBaud.Items.AddRange(new object[]
            {
                "1 Mbps",
                "800 kbps",
                "500 kbps",
                "250 kbps",
                "125 kbps",
                "100 kbps",
                "50 kbps",
                "20 kbps",
                "10 kbps",
                "5 kbps"
            });
            cmbCanBaud.SelectedIndex = 0; // Default 1 Mbps
        }

        private void ScanAvailableComPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            cmbComPort.Items.Clear();
            if (ports.Length > 0)
            {
                cmbComPort.Items.AddRange(ports);
                cmbComPort.SelectedIndex = 0;
            }
            else
            {
                cmbComPort.Items.Add("No COM Ports");
                cmbComPort.SelectedIndex = 0;
            }
        }

        private WaveshareUsbCanDriver.CanBaudrate GetSelectedCanBaud()
        {
            return cmbCanBaud.SelectedIndex switch
            {
                0 => WaveshareUsbCanDriver.CanBaudrate.Baud1Mbps,
                1 => WaveshareUsbCanDriver.CanBaudrate.Baud800kbps,
                2 => WaveshareUsbCanDriver.CanBaudrate.Baud500kbps,
                3 => WaveshareUsbCanDriver.CanBaudrate.Baud250kbps,
                4 => WaveshareUsbCanDriver.CanBaudrate.Baud125kbps,
                5 => WaveshareUsbCanDriver.CanBaudrate.Baud100kbps,
                6 => WaveshareUsbCanDriver.CanBaudrate.Baud500kbps,
                7 => WaveshareUsbCanDriver.CanBaudrate.Baud20kbps,
                8 => WaveshareUsbCanDriver.CanBaudrate.Baud10kbps,
                9 => WaveshareUsbCanDriver.CanBaudrate.Baud5kbps,
                _ => WaveshareUsbCanDriver.CanBaudrate.Baud1Mbps
            };
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (_driver.IsOpen)
            {
                _driver.Disconnect();
                UpdateConnectionUiState(false);
            }
            else
            {
                string port = cmbComPort.SelectedItem?.ToString() ?? "";
                if (string.IsNullOrEmpty(port) || port.StartsWith("No COM"))
                {
                    MessageBox.Show("Lütfen geçerli bir COM port seçin.", "Bağlantı Uyarısı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(cmbSerialBaud.SelectedItem?.ToString(), out int serialBaud))
                {
                    serialBaud = 2000000;
                }

                var canBaud = GetSelectedCanBaud();

                bool ok = _driver.Connect(port, serialBaud, canBaud);
                if (ok)
                {
                    UpdateConnectionUiState(true);
                }
            }
        }

        private void UpdateConnectionUiState(bool connected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateConnectionUiState(connected)));
                return;
            }

            if (connected)
            {
                btnConnect.Text = "Disconnect Adapter";
                btnConnect.NormalColor = Color.FromArgb(220, 38, 38);
                btnConnect.HoverColor = Color.FromArgb(239, 68, 68);
                pnlStatusIndicatorDot.BackColor = UiTheme.EmeraldSuccess;
                lblHeaderConnectionStatus.Text = $"CONNECTED: {_driver.PortName}";
                lblHeaderConnectionStatus.ForeColor = UiTheme.EmeraldSuccess;
                cmbComPort.Enabled = false;
                cmbSerialBaud.Enabled = false;
                cmbCanBaud.Enabled = false;
                btnRefreshPorts.Enabled = false;
                btnFlash.Enabled = (_loadedFirmware != null && !_bootloader.IsFlashing);
                btnSendBlink.Enabled = true;
            }
            else
            {
                btnConnect.Text = "Connect Adapter";
                btnConnect.NormalColor = Color.FromArgb(0, 168, 132);
                btnConnect.HoverColor = Color.FromArgb(0, 196, 154);
                pnlStatusIndicatorDot.BackColor = UiTheme.TextMuted;
                lblHeaderConnectionStatus.Text = "ADAPTER DISCONNECTED";
                lblHeaderConnectionStatus.ForeColor = UiTheme.TextSecondary;
                cmbComPort.Enabled = true;
                cmbSerialBaud.Enabled = true;
                cmbCanBaud.Enabled = true;
                btnRefreshPorts.Enabled = true;
                btnFlash.Enabled = false;
                btnSendBlink.Enabled = false;
            }
        }

        private void BtnSelectFile_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Firmware Files (*.elf;*.hex;*.bin)|*.elf;*.hex;*.bin|ELF Executable (*.elf)|*.elf|Intel HEX (*.hex)|*.hex|Raw Binary (*.bin)|*.bin|All Files (*.*)|*.*";
                ofd.Title = "Select Firmware File";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        txtFilePath.Text = ofd.FileName;
                        _loadedFirmware = HexBinParser.LoadFirmware(ofd.FileName);

                        lblFileInfo.Text = $"Format: {_loadedFirmware.FileType}  |  Base: 0x{_loadedFirmware.BaseAddress:X8}  |  Size: {_loadedFirmware.TotalBytes:N0} bytes ({_loadedFirmware.TotalPackets} frames)  |  CRC32: 0x{_loadedFirmware.CalculatedCrc32:X8}";
                        lblFileInfo.ForeColor = UiTheme.CyanAccent;

                        btnFlash.Enabled = _driver.IsOpen && !_bootloader.IsFlashing;
                        lblFlashStatus.Text = "Firmware verified and ready to flash.";
                        lblFlashStatus.ForeColor = UiTheme.EmeraldSuccess;
                    }
                    catch (Exception ex)
                    {
                        _loadedFirmware = null;
                        btnFlash.Enabled = false;
                        lblFileInfo.Text = $"Parse Error: {ex.Message}";
                        lblFileInfo.ForeColor = UiTheme.RedDanger;
                        MessageBox.Show($"Firmware parsing error:\n{ex.Message}", "Parser Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void BtnFlash_Click(object? sender, EventArgs e)
        {
            if (_loadedFirmware == null)
            {
                MessageBox.Show("Please select a firmware file first.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_driver.IsOpen)
            {
                MessageBox.Show("CAN Adapter is not connected.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnFlash.Enabled = false;
            btnCancel.Enabled = true;
            btnSelectFile.Enabled = false;
            prgFlash.Value = 0;
            lblProgressPercent.Text = "0%";

            _flashCts = new CancellationTokenSource();
            int delayMs = (int)numPacketDelay.Value;

            try
            {
                await _bootloader.FlashFirmwareAsync(_loadedFirmware, delayMs, _flashCts.Token);
            }
            catch (Exception ex)
            {
                lblFlashStatus.Text = $"Error: {ex.Message}";
                lblFlashStatus.ForeColor = UiTheme.RedDanger;
            }
            finally
            {
                btnFlash.Enabled = _driver.IsOpen;
                btnCancel.Enabled = false;
                btnSelectFile.Enabled = true;
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e)
        {
            _flashCts?.Cancel();
            lblFlashStatus.Text = "Flashing cancellation requested...";
            lblFlashStatus.ForeColor = UiTheme.AmberWarning;
        }

        private void OnFlashProgressChanged(int currentBytes, int totalBytes)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnFlashProgressChanged(currentBytes, totalBytes)));
                return;
            }

            int targetTotal = (totalBytes > 0) ? totalBytes : (_loadedFirmware?.TotalBytes ?? 1);
            int percent = (int)Math.Min(100, (long)currentBytes * 100 / targetTotal);

            prgFlash.Value = Math.Max(0, Math.Min(100, percent));
            lblProgressPercent.Text = $"{percent}%";
            lblFlashStatus.Text = $"Flashing progress: {currentBytes:N0} / {targetTotal:N0} bytes ({percent}%)";
        }

        private void OnFlashStatusChanged(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnFlashStatusChanged(status)));
                return;
            }
            lblFlashStatus.Text = status;
            lblFlashStatus.ForeColor = UiTheme.TextPrimary;
        }

        private void OnFlashCompleted(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnFlashCompleted(message)));
                return;
            }
            prgFlash.Value = 100;
            lblProgressPercent.Text = "100%";
            lblFlashStatus.Text = message;
            lblFlashStatus.ForeColor = UiTheme.EmeraldSuccess;
            MessageBox.Show(message, "Programming Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnFlashFailed(string error)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnFlashFailed(error)));
                return;
            }
            lblFlashStatus.Text = error;
            lblFlashStatus.ForeColor = UiTheme.RedDanger;
            MessageBox.Show(error, "Programming Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnSendBlink_Click(object? sender, EventArgs e)
        {
            if (!_driver.IsOpen) return;
            ushort period = (ushort)numBlinkPeriod.Value;
            _bootloader.SendBlinkTest(period);
        }

        private void OnMessageTransmitted(CanMessage msg)
        {
            EnrichDescription(msg);
            _logQueue.Enqueue(msg);
        }

        private void OnMessageReceived(CanMessage msg)
        {
            EnrichDescription(msg);
            _logQueue.Enqueue(msg);

            // Handle Telemetry frame
            if (msg.Id == BootloaderProtocol.CAN_ID_TELEMETRY && msg.Data.Length >= 8)
            {
                uint counter = BitConverter.ToUInt32(msg.Data, 0);
                ushort blinkPeriod = BitConverter.ToUInt16(msg.Data, 4);
                byte statusCode = msg.Data[6];
                byte tail = msg.Data[7];

                BeginInvoke(new Action(() =>
                {
                    lblCardCounterValue.Text = counter.ToString("N0");
                    lblCardBlinkValue.Text = $"{blinkPeriod} ms";

                    if (statusCode == 0xAA && tail == 0x55)
                    {
                        lblCardStateValue.Text = "OK (0xAA)";
                        lblCardStateValue.ForeColor = UiTheme.EmeraldSuccess;
                    }
                    else
                    {
                        lblCardStateValue.Text = $"0x{statusCode:X2}";
                        lblCardStateValue.ForeColor = UiTheme.RedDanger;
                    }
                }));
            }
        }

        private void EnrichDescription(CanMessage msg)
        {
            if (!string.IsNullOrEmpty(msg.Description))
                return;

            msg.Description = msg.Id switch
            {
                BootloaderProtocol.CAN_ID_ERASE_CMD => "Erase Flash Command (0x700)",
                BootloaderProtocol.CAN_ID_DATA_TRANSFER => "Firmware Data Packet (0x701)",
                BootloaderProtocol.CAN_ID_FINISH_CMD => "Transfer Completed Command (0x702)",
                BootloaderProtocol.CAN_ID_ERASE_ACK => $"Erase ACK: {(msg.Data.Length > 0 && msg.Data[0] == 0x01 ? "Success (0x01)" : "Failed (0x00)")}",
                BootloaderProtocol.CAN_ID_WRITE_ACK => $"Periodic Write ACK: {(msg.Data.Length >= 4 ? BitConverter.ToUInt32(msg.Data, 0) : 0)} bytes written",
                BootloaderProtocol.CAN_ID_CRC_RESPONSE => $"Hardware CRC32 Response: 0x{(msg.Data.Length >= 4 ? BitConverter.ToUInt32(msg.Data, 0) : 0):X8}",
                BootloaderProtocol.CAN_ID_ERROR_REPORT => $"Target Error Code: {(msg.Data.Length > 0 ? msg.Data[0].ToString() : "?")}",
                BootloaderProtocol.CAN_ID_TEST_LED_CMD => $"LED Blink Command: {(msg.Data.Length >= 2 ? BitConverter.ToUInt16(msg.Data, 0) : 0)} ms",
                BootloaderProtocol.CAN_ID_TELEMETRY => "Target Telemetry Packet",
                _ => $"Standard CAN Frame (0x{msg.Id:X3})"
            };
        }

        private void OnCommunicationError(Exception ex)
        {
            BeginInvoke(new Action(() =>
            {
                lblHeaderConnectionStatus.Text = $"Comm Error: {ex.Message}";
                lblHeaderConnectionStatus.ForeColor = UiTheme.RedDanger;
                pnlStatusIndicatorDot.BackColor = UiTheme.RedDanger;
            }));
        }

        private void OnUiRefreshTimerTick(object? sender, EventArgs e)
        {
            if (_logQueue.IsEmpty) return;

            var batch = new List<CanMessage>();
            while (_logQueue.TryDequeue(out var msg) && batch.Count < 100)
            {
                batch.Add(msg);
            }

            if (batch.Count == 0) return;

            dgvTraffic.SuspendLayout();
            foreach (var msg in batch)
            {
                _allLogs.Add(msg);

                int rowIndex = dgvTraffic.Rows.Add(
                    msg.TimestampStr,
                    msg.DirectionStr,
                    msg.IdHex,
                    msg.Dlc,
                    msg.DataHex,
                    msg.Description
                );

                var row = dgvTraffic.Rows[rowIndex];
                if (msg.IsTx)
                {
                    row.Cells["colDir"].Style.ForeColor = UiTheme.CyanAccent;
                }
                else
                {
                    row.Cells["colDir"].Style.ForeColor = UiTheme.EmeraldSuccess;
                }

                if (msg.Id == BootloaderProtocol.CAN_ID_ERROR_REPORT)
                {
                    row.DefaultCellStyle.ForeColor = UiTheme.RedDanger;
                }
            }

            // Cap row count
            while (dgvTraffic.Rows.Count > MAX_LOG_ROWS)
            {
                dgvTraffic.Rows.RemoveAt(0);
            }

            if (_allLogs.Count > MAX_LOG_ROWS * 2)
            {
                _allLogs.RemoveRange(0, MAX_LOG_ROWS);
            }

            dgvTraffic.ResumeLayout();

            lblTrafficCount.Text = $"Packets: {_allLogs.Count:N0}";

            if (chkAutoScroll.Checked && dgvTraffic.Rows.Count > 0)
            {
                dgvTraffic.FirstDisplayedScrollingRowIndex = dgvTraffic.Rows.Count - 1;
            }
        }

        private void ClearTrafficLogs()
        {
            _allLogs.Clear();
            dgvTraffic.Rows.Clear();
            lblTrafficCount.Text = "Packets: 0";
        }

        private void BtnExportCsv_Click(object? sender, EventArgs e)
        {
            if (_allLogs.Count == 0)
            {
                MessageBox.Show("No log entries to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "CSV File (*.csv)|*.csv";
                sfd.FileName = $"CAN_Traffic_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sb = new StringBuilder();
                        sb.AppendLine("Timestamp,Direction,CAN_ID,DLC,Data,Description");

                        foreach (var log in _allLogs)
                        {
                            sb.AppendLine($"\"{log.TimestampStr}\",\"{log.DirectionStr}\",\"{log.IdHex}\",{log.Dlc},\"{log.DataHex}\",\"{log.Description.Replace("\"", "\"\"")}\"");
                        }

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                        MessageBox.Show($"Exported {_allLogs.Count} records successfully to:\n{sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to export CSV: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
