using System;
using System.Drawing;
using System.Windows.Forms;

namespace Can_Bootloader_GUI.UI
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Top Header
        private Panel pnlHeader;
        private Label lblAppTitle;
        private Label lblAppSubtitle;
        private Panel pnlHeaderStatusBadge;
        private Panel pnlStatusIndicatorDot;
        private Label lblHeaderConnectionStatus;

        // Main Layout Split
        private TableLayoutPanel tblMainLayout;

        // Section 1: Connection Panel
        private ModernCardPanel cardConnection;
        private Label lblComPort;
        private ComboBox cmbComPort;
        private ModernButton btnRefreshPorts;
        private Label lblSerialBaud;
        private ComboBox cmbSerialBaud;
        private Label lblCanBaud;
        private ComboBox cmbCanBaud;
        private ModernButton btnConnect;
        private Label lblPortHint;

        // Section 2: Bootloader Programming Panel
        private ModernCardPanel cardBootloader;
        private ModernButton btnSelectFile;
        private TextBox txtFilePath;
        private Panel pnlFileInfoContainer;
        private Label lblFileInfo;
        private Label lblDelay;
        private NumericUpDown numPacketDelay;
        private Label lblDelayUnit;
        private ModernButton btnFlash;
        private ModernButton btnCancel;
        private ModernProgressBar prgFlash;
        private Label lblProgressPercent;
        private Label lblFlashStatus;

        // Section 3: Target Test & Live Telemetry Panel
        private ModernCardPanel cardTelemetry;
        private Label lblBlinkPeriod;
        private NumericUpDown numBlinkPeriod;
        private Label lblBlinkUnit;
        private ModernButton btnSendBlink;
        private TableLayoutPanel tblTelemetryCards;
        private ModernStatCard pnlCardCounter;
        private Label lblCardCounterTitle;
        private Label lblCardCounterValue;
        private ModernStatCard pnlCardBlink;
        private Label lblCardBlinkTitle;
        private Label lblCardBlinkValue;
        private ModernStatCard pnlCardState;
        private Label lblCardStateTitle;
        private Label lblCardStateValue;

        // Section 4: CAN Traffic Monitor
        private ModernCardPanel cardMonitor;
        private Panel pnlMonitorToolbar;
        private CheckBox chkAutoScroll;
        private ModernButton btnClearLogs;
        private ModernButton btnExportCsv;
        private Label lblTrafficCount;
        private DataGridView dgvTraffic;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // Form settings
            this.Text = "Can Bootloader GUI - STM32 & Waveshare USB-CAN-A";
            this.Size = new Size(1280, 860);
            this.MinimumSize = new Size(1100, 750);
            this.BackColor = UiTheme.BgDark;
            this.ForeColor = UiTheme.TextPrimary;
            this.Font = UiTheme.FontBody;
            this.StartPosition = FormStartPosition.CenterScreen;

            // 1. Top Header Bar
            this.pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(20, 23, 31),
                Padding = new Padding(24, 12, 24, 12)
            };

            this.lblAppTitle = new Label
            {
                Text = "CAN BOOTLOADER GUI",
                Font = UiTheme.FontTitle,
                ForeColor = UiTheme.CyanAccent,
                AutoSize = true,
                Location = new Point(24, 12)
            };

            this.lblAppSubtitle = new Label
            {
                Text = "Professional Flash Programmer for STM32 targets via Waveshare USB-CAN-A adapter",
                Font = UiTheme.FontSubtitle,
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(25, 38)
            };

            // Status Badge on the right of header
            this.pnlHeaderStatusBadge = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(1020, 18),
                Size = new Size(230, 32),
                BackColor = Color.FromArgb(26, 31, 42),
                BorderStyle = BorderStyle.None
            };

            this.pnlStatusIndicatorDot = new Panel
            {
                Location = new Point(14, 11),
                Size = new Size(10, 10),
                BackColor = UiTheme.TextMuted
            };

            this.lblHeaderConnectionStatus = new Label
            {
                Text = "ADAPTER DISCONNECTED",
                Location = new Point(32, 7),
                AutoSize = true,
                Font = UiTheme.FontSmall,
                ForeColor = UiTheme.TextSecondary
            };

            this.pnlHeaderStatusBadge.Controls.Add(this.pnlStatusIndicatorDot);
            this.pnlHeaderStatusBadge.Controls.Add(this.lblHeaderConnectionStatus);

            this.pnlHeader.Controls.Add(this.lblAppTitle);
            this.pnlHeader.Controls.Add(this.lblAppSubtitle);
            this.pnlHeader.Controls.Add(this.pnlHeaderStatusBadge);

            // 2. Main 2x2 Grid Layout
            this.tblMainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(16, 12, 16, 16),
                BackColor = UiTheme.BgDark
            };
            this.tblMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            this.tblMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            this.tblMainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 350F));
            this.tblMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Section 1: Connection
            this.cardConnection = new ModernCardPanel
            {
                Dock = DockStyle.Fill,
                CardTitle = "1. HARDWARE & ADAPTER CONNECTION",
                HeaderAccentColor = UiTheme.CyanAccent,
                Margin = new Padding(8)
            };
            InitConnectionControls();

            // Section 2: Firmware Flash
            this.cardBootloader = new ModernCardPanel
            {
                Dock = DockStyle.Fill,
                CardTitle = "2. FIRMWARE PROGRAMMING (.ELF / .HEX / .BIN)",
                HeaderAccentColor = UiTheme.TealSuccess,
                Margin = new Padding(8)
            };
            InitBootloaderControls();

            // Section 3: Telemetry & Test
            this.cardTelemetry = new ModernCardPanel
            {
                Dock = DockStyle.Fill,
                CardTitle = "3. TARGET DIAGNOSTICS & TELEMETRY",
                HeaderAccentColor = UiTheme.IndigoAction,
                Margin = new Padding(8)
            };
            InitTelemetryControls();

            // Section 4: Real-time Monitor
            this.cardMonitor = new ModernCardPanel
            {
                Dock = DockStyle.Fill,
                CardTitle = "4. REAL-TIME CAN TRAFFIC & DIAGNOSTIC MONITOR",
                HeaderAccentColor = UiTheme.AmberWarning,
                Margin = new Padding(8)
            };
            InitMonitorControls();

            this.tblMainLayout.Controls.Add(this.cardConnection, 0, 0);
            this.tblMainLayout.Controls.Add(this.cardBootloader, 1, 0);
            this.tblMainLayout.Controls.Add(this.cardTelemetry, 0, 1);
            this.tblMainLayout.Controls.Add(this.cardMonitor, 1, 1);

            this.Controls.Add(this.tblMainLayout);
            this.Controls.Add(this.pnlHeader);
        }

        private void InitConnectionControls()
        {
            int lblX = 24;
            int ctrlX = 160;
            int ctrlW = 210;

            // COM Port
            this.lblComPort = new Label
            {
                Text = "COM Port:",
                Location = new Point(lblX, 58),
                AutoSize = true,
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontBody
            };

            this.cmbComPort = new ComboBox
            {
                Location = new Point(ctrlX, 54),
                Width = ctrlW,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UiTheme.InputBg,
                ForeColor = UiTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = UiTheme.FontBody
            };

            this.btnRefreshPorts = new ModernButton
            {
                Text = "Scan Ports",
                Location = new Point(ctrlX + ctrlW + 12, 53),
                Width = 105,
                Height = 31,
                NormalColor = Color.FromArgb(36, 44, 60),
                HoverColor = Color.FromArgb(50, 60, 82),
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontSmall
            };

            // USB Serial Baudrate
            this.lblSerialBaud = new Label
            {
                Text = "USB Baudrate:",
                Location = new Point(lblX, 102),
                AutoSize = true,
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontBody
            };

            this.cmbSerialBaud = new ComboBox
            {
                Location = new Point(ctrlX, 98),
                Width = ctrlW,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UiTheme.InputBg,
                ForeColor = UiTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = UiTheme.FontBody
            };

            // CAN Bus Baudrate
            this.lblCanBaud = new Label
            {
                Text = "CAN Bus Speed:",
                Location = new Point(lblX, 146),
                AutoSize = true,
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontBody
            };

            this.cmbCanBaud = new ComboBox
            {
                Location = new Point(ctrlX, 142),
                Width = ctrlW,
                Height = 30,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = UiTheme.InputBg,
                ForeColor = UiTheme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Font = UiTheme.FontBody
            };

            // Connect Button
            this.btnConnect = new ModernButton
            {
                Text = "Connect Adapter",
                Location = new Point(lblX, 204),
                Width = ctrlW + ctrlX - lblX + 117,
                Height = 44,
                NormalColor = Color.FromArgb(0, 168, 132),
                HoverColor = Color.FromArgb(0, 196, 154),
                PressedColor = Color.FromArgb(0, 140, 110),
                Font = UiTheme.FontSection,
                ForeColor = Color.White
            };

            this.lblPortHint = new Label
            {
                Text = "Requires Waveshare USB-CAN-A. Default speed: 2,000,000 bps serial, 1 Mbps CAN.",
                Location = new Point(lblX, 268),
                Size = new Size(490, 36),
                ForeColor = UiTheme.TextMuted,
                Font = UiTheme.FontSmall
            };

            this.cardConnection.Controls.Add(this.lblComPort);
            this.cardConnection.Controls.Add(this.cmbComPort);
            this.cardConnection.Controls.Add(this.btnRefreshPorts);
            this.cardConnection.Controls.Add(this.lblSerialBaud);
            this.cardConnection.Controls.Add(this.cmbSerialBaud);
            this.cardConnection.Controls.Add(this.lblCanBaud);
            this.cardConnection.Controls.Add(this.cmbCanBaud);
            this.cardConnection.Controls.Add(this.btnConnect);
            this.cardConnection.Controls.Add(this.lblPortHint);
        }

        private void InitBootloaderControls()
        {
            int startX = 24;

            this.btnSelectFile = new ModernButton
            {
                Text = "Select File...",
                Location = new Point(startX, 54),
                Width = 125,
                Height = 32,
                NormalColor = Color.FromArgb(42, 52, 72),
                HoverColor = Color.FromArgb(56, 68, 94),
                Font = UiTheme.FontBodyBold
            };

            this.txtFilePath = new TextBox
            {
                Location = new Point(startX + 135, 56),
                Width = 435,
                Height = 28,
                ReadOnly = true,
                BackColor = UiTheme.InputBg,
                ForeColor = UiTheme.TextSecondary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = UiTheme.FontMonospace
            };

            // File summary container badge
            this.pnlFileInfoContainer = new Panel
            {
                Location = new Point(startX, 94),
                Width = 570,
                Height = 44,
                BackColor = Color.FromArgb(17, 20, 28),
                Padding = new Padding(12, 6, 12, 6)
            };

            this.lblFileInfo = new Label
            {
                Text = "Select a valid .elf, .hex, or .bin firmware file.",
                Dock = DockStyle.Fill,
                ForeColor = UiTheme.CyanAccent,
                Font = UiTheme.FontSmall,
                TextAlign = ContentAlignment.MiddleLeft
            };
            this.pnlFileInfoContainer.Controls.Add(this.lblFileInfo);

            // Delay controls
            this.lblDelay = new Label
            {
                Text = "Packet Delay:",
                Location = new Point(startX, 152),
                AutoSize = true,
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontBody
            };

            this.numPacketDelay = new NumericUpDown
            {
                Location = new Point(startX + 105, 149),
                Width = 70,
                Height = 28,
                Minimum = 0,
                Maximum = 100,
                Value = 2,
                BackColor = UiTheme.InputBg,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontBodyBold
            };

            this.lblDelayUnit = new Label
            {
                Text = "ms (Inter-packet delay for reliable buffer reception)",
                Location = new Point(startX + 185, 152),
                AutoSize = true,
                ForeColor = UiTheme.TextMuted,
                Font = UiTheme.FontSmall
            };

            // Action Buttons
            this.btnFlash = new ModernButton
            {
                Text = "Flash Firmware",
                Location = new Point(startX, 194),
                Width = 220,
                Height = 42,
                NormalColor = Color.FromArgb(16, 150, 90),
                HoverColor = Color.FromArgb(20, 180, 110),
                PressedColor = Color.FromArgb(12, 120, 70),
                Font = UiTheme.FontSection,
                Enabled = false
            };

            this.btnCancel = new ModernButton
            {
                Text = "Cancel",
                Location = new Point(startX + 235, 194),
                Width = 110,
                Height = 42,
                NormalColor = Color.FromArgb(180, 45, 55),
                HoverColor = Color.FromArgb(215, 55, 68),
                PressedColor = Color.FromArgb(150, 35, 45),
                Font = UiTheme.FontSection,
                Enabled = false
            };

            // Custom sleek progress bar
            this.prgFlash = new ModernProgressBar
            {
                Location = new Point(startX, 252),
                Width = 510,
                Height = 16,
                Value = 0,
                Maximum = 100
            };

            this.lblProgressPercent = new Label
            {
                Text = "0%",
                Location = new Point(startX + 520, 250),
                AutoSize = true,
                ForeColor = UiTheme.CyanAccent,
                Font = UiTheme.FontBodyBold
            };

            this.lblFlashStatus = new Label
            {
                Text = "Ready to flash.",
                Location = new Point(startX, 280),
                Size = new Size(570, 32),
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontSmall
            };

            this.cardBootloader.Controls.Add(this.btnSelectFile);
            this.cardBootloader.Controls.Add(this.txtFilePath);
            this.cardBootloader.Controls.Add(this.pnlFileInfoContainer);
            this.cardBootloader.Controls.Add(this.lblDelay);
            this.cardBootloader.Controls.Add(this.numPacketDelay);
            this.cardBootloader.Controls.Add(this.lblDelayUnit);
            this.cardBootloader.Controls.Add(this.btnFlash);
            this.cardBootloader.Controls.Add(this.btnCancel);
            this.cardBootloader.Controls.Add(this.prgFlash);
            this.cardBootloader.Controls.Add(this.lblProgressPercent);
            this.cardBootloader.Controls.Add(this.lblFlashStatus);
        }

        private void InitTelemetryControls()
        {
            int startX = 24;

            this.lblBlinkPeriod = new Label
            {
                Text = "LED Blink Period:",
                Location = new Point(startX, 54),
                AutoSize = true,
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontBody
            };

            this.numBlinkPeriod = new NumericUpDown
            {
                Location = new Point(startX + 130, 51),
                Width = 85,
                Minimum = 50,
                Maximum = 5000,
                Value = 500,
                BackColor = UiTheme.InputBg,
                ForeColor = UiTheme.TextPrimary,
                Font = UiTheme.FontBodyBold
            };

            this.lblBlinkUnit = new Label
            {
                Text = "ms",
                Location = new Point(startX + 220, 54),
                AutoSize = true,
                ForeColor = UiTheme.TextMuted,
                Font = UiTheme.FontBody
            };

            this.btnSendBlink = new ModernButton
            {
                Text = "Send LED Command",
                Location = new Point(startX + 255, 49),
                Width = 165,
                Height = 32,
                NormalColor = Color.FromArgb(36, 46, 68),
                HoverColor = Color.FromArgb(50, 64, 94),
                Font = UiTheme.FontSmall
            };

            // Telemetry Cards Table (Responsive layout)
            this.tblTelemetryCards = new TableLayoutPanel
            {
                Location = new Point(startX, 102),
                Size = new Size(510, 140),
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            this.tblTelemetryCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.tblTelemetryCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            this.tblTelemetryCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            this.tblTelemetryCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            this.pnlCardCounter = CreateModernStatCard("HEARTBEAT SAYAÇ", out this.lblCardCounterTitle, out this.lblCardCounterValue, UiTheme.CyanAccent);
            this.pnlCardBlink = CreateModernStatCard("AKTİF BLINK", out this.lblCardBlinkTitle, out this.lblCardBlinkValue, UiTheme.TealSuccess);
            this.pnlCardState = CreateModernStatCard("DURUM KODU", out this.lblCardStateTitle, out this.lblCardStateValue, UiTheme.EmeraldSuccess);

            this.tblTelemetryCards.Controls.Add(this.pnlCardCounter, 0, 0);
            this.tblTelemetryCards.Controls.Add(this.pnlCardBlink, 1, 0);
            this.tblTelemetryCards.Controls.Add(this.pnlCardState, 2, 0);

            this.cardTelemetry.Controls.Add(this.lblBlinkPeriod);
            this.cardTelemetry.Controls.Add(this.numBlinkPeriod);
            this.cardTelemetry.Controls.Add(this.lblBlinkUnit);
            this.cardTelemetry.Controls.Add(this.btnSendBlink);
            this.cardTelemetry.Controls.Add(this.tblTelemetryCards);
        }

        private ModernStatCard CreateModernStatCard(string title, out Label lblTitle, out Label lblValue, Color accentColor)
        {
            var pnl = new ModernStatCard
            {
                Dock = DockStyle.Fill
            };

            lblTitle = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 32,
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontSmall,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblValue = new Label
            {
                Text = "--",
                Dock = DockStyle.Fill,
                ForeColor = accentColor,
                Font = UiTheme.FontLargeMetric,
                TextAlign = ContentAlignment.MiddleCenter
            };

            pnl.Controls.Add(lblValue);
            pnl.Controls.Add(lblTitle);
            return pnl;
        }

        private void InitMonitorControls()
        {
            this.pnlMonitorToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(6, 4, 6, 6),
                BackColor = Color.Transparent
            };

            this.chkAutoScroll = new CheckBox
            {
                Text = "Auto Scroll",
                Checked = true,
                ForeColor = UiTheme.TextSecondary,
                Font = UiTheme.FontSmall,
                Location = new Point(8, 10),
                AutoSize = true
            };

            this.btnClearLogs = new ModernButton
            {
                Text = "Clear",
                Location = new Point(115, 6),
                Width = 72,
                Height = 28,
                NormalColor = Color.FromArgb(36, 44, 60),
                HoverColor = Color.FromArgb(50, 60, 82),
                Font = UiTheme.FontSmall
            };

            this.btnExportCsv = new ModernButton
            {
                Text = "Export CSV",
                Location = new Point(195, 6),
                Width = 92,
                Height = 28,
                NormalColor = Color.FromArgb(36, 44, 60),
                HoverColor = Color.FromArgb(50, 60, 82),
                Font = UiTheme.FontSmall
            };

            this.lblTrafficCount = new Label
            {
                Text = "Packets: 0",
                Location = new Point(305, 10),
                AutoSize = true,
                ForeColor = UiTheme.TextMuted,
                Font = UiTheme.FontSmall
            };

            this.pnlMonitorToolbar.Controls.Add(this.chkAutoScroll);
            this.pnlMonitorToolbar.Controls.Add(this.btnClearLogs);
            this.pnlMonitorToolbar.Controls.Add(this.btnExportCsv);
            this.pnlMonitorToolbar.Controls.Add(this.lblTrafficCount);

            // DataGridView Configuration
            this.dgvTraffic = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(16, 18, 25),
                ForeColor = UiTheme.TextPrimary,
                GridColor = Color.FromArgb(30, 36, 48),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                Font = UiTheme.FontMonospace,
                RowTemplate = { Height = 25 }
            };

            this.dgvTraffic.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(25, 30, 42);
            this.dgvTraffic.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.CyanAccent;
            this.dgvTraffic.ColumnHeadersDefaultCellStyle.Font = UiTheme.FontBodyBold;
            this.dgvTraffic.ColumnHeadersHeight = 32;

            this.dgvTraffic.DefaultCellStyle.BackColor = Color.FromArgb(16, 18, 25);
            this.dgvTraffic.DefaultCellStyle.SelectionBackColor = Color.FromArgb(38, 54, 78);
            this.dgvTraffic.DefaultCellStyle.SelectionForeColor = Color.White;

            this.dgvTraffic.Columns.Add("colTime", "Time");
            this.dgvTraffic.Columns["colTime"].Width = 100;

            this.dgvTraffic.Columns.Add("colDir", "Dir");
            this.dgvTraffic.Columns["colDir"].Width = 50;

            this.dgvTraffic.Columns.Add("colId", "ID");
            this.dgvTraffic.Columns["colId"].Width = 70;

            this.dgvTraffic.Columns.Add("colDlc", "DLC");
            this.dgvTraffic.Columns["colDlc"].Width = 45;

            this.dgvTraffic.Columns.Add("colData", "Data");
            this.dgvTraffic.Columns["colData"].Width = 200;

            this.dgvTraffic.Columns.Add("colDesc", "Description");
            this.dgvTraffic.Columns["colDesc"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            this.cardMonitor.Controls.Add(this.dgvTraffic);
            this.cardMonitor.Controls.Add(this.pnlMonitorToolbar);
        }
    }
}
