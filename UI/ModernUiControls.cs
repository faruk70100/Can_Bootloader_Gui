using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Can_Bootloader_GUI.UI
{
    public static class UiTheme
    {
        // Color Palette
        public static readonly Color BgDark = Color.FromArgb(15, 17, 23);
        public static readonly Color CardBg = Color.FromArgb(22, 25, 34);
        public static readonly Color CardHeaderBg = Color.FromArgb(28, 32, 44);
        public static readonly Color BorderColor = Color.FromArgb(38, 44, 60);
        public static readonly Color InputBg = Color.FromArgb(17, 19, 27);
        public static readonly Color InputBorder = Color.FromArgb(48, 56, 76);

        // Accent Colors
        public static readonly Color CyanAccent = Color.FromArgb(0, 210, 255);
        public static readonly Color TealSuccess = Color.FromArgb(0, 190, 140);
        public static readonly Color EmeraldSuccess = Color.FromArgb(34, 197, 94);
        public static readonly Color RedDanger = Color.FromArgb(239, 68, 68);
        public static readonly Color AmberWarning = Color.FromArgb(245, 158, 11);
        public static readonly Color IndigoAction = Color.FromArgb(99, 102, 241);

        // Text Colors
        public static readonly Color TextPrimary = Color.FromArgb(243, 244, 246);
        public static readonly Color TextSecondary = Color.FromArgb(156, 163, 175);
        public static readonly Color TextMuted = Color.FromArgb(107, 114, 128);

        // Fonts
        public static readonly Font FontTitle = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        public static readonly Font FontSubtitle = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font FontSection = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font FontBody = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font FontBodyBold = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font FontSmall = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font FontMonospace = new Font("Consolas", 9F, FontStyle.Regular);
        public static readonly Font FontLargeMetric = new Font("Segoe UI", 17F, FontStyle.Bold);

        public static GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ModernCardPanel : Panel
    {
        public int BorderRadius { get; set; } = 8;
        public Color BorderColor { get; set; } = UiTheme.BorderColor;
        public string CardTitle { get; set; } = string.Empty;
        public Color HeaderAccentColor { get; set; } = UiTheme.CyanAccent;

        public ModernCardPanel()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = UiTheme.CardBg;
            this.Padding = new Padding(16, 44, 16, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(this.Parent?.BackColor ?? UiTheme.BgDark);

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = UiTheme.GetRoundedRectangle(rect, BorderRadius))
            {
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                if (!string.IsNullOrEmpty(CardTitle))
                {
                    Rectangle headerRect = new Rectangle(0, 0, this.Width, 38);
                    using (GraphicsPath headerPath = new GraphicsPath())
                    {
                        headerPath.AddArc(0, 0, BorderRadius * 2, BorderRadius * 2, 180, 90);
                        headerPath.AddArc(this.Width - 1 - BorderRadius * 2, 0, BorderRadius * 2, BorderRadius * 2, 270, 90);
                        headerPath.AddLine(this.Width - 1, 38, 0, 38);
                        headerPath.CloseFigure();

                        using (SolidBrush headerBrush = new SolidBrush(UiTheme.CardHeaderBg))
                        {
                            e.Graphics.FillPath(headerBrush, headerPath);
                        }
                    }

                    using (SolidBrush accentBrush = new SolidBrush(HeaderAccentColor))
                    {
                        e.Graphics.FillRectangle(accentBrush, 14, 11, 4, 16);
                    }

                    using (SolidBrush textBrush = new SolidBrush(UiTheme.TextPrimary))
                    {
                        e.Graphics.DrawString(CardTitle, UiTheme.FontSection, textBrush, new PointF(24, 10));
                    }

                    using (Pen divPen = new Pen(UiTheme.BorderColor))
                    {
                        e.Graphics.DrawLine(divPen, 0, 38, this.Width, 38);
                    }
                }

                using (Pen pen = new Pen(BorderColor, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    public class ModernStatCard : Panel
    {
        public int BorderRadius { get; set; } = 6;
        public Color BorderColor { get; set; } = UiTheme.BorderColor;

        public ModernStatCard()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(17, 20, 28);
            this.Margin = new Padding(6);
            this.Padding = new Padding(8);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = UiTheme.GetRoundedRectangle(rect, BorderRadius))
            {
                using (SolidBrush brush = new SolidBrush(this.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                using (Pen pen = new Pen(BorderColor, 1f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }
        }
    }

    public class ModernButton : Button
    {
        public int BorderRadius { get; set; } = 6;
        public Color NormalColor { get; set; } = Color.FromArgb(40, 48, 64);
        public Color HoverColor { get; set; } = Color.FromArgb(55, 66, 88);
        public Color PressedColor { get; set; } = Color.FromArgb(30, 36, 48);
        public Color BorderNormalColor { get; set; } = Color.Transparent;

        private bool _isHovered = false;
        private bool _isPressed = false;

        public ModernButton()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = UiTheme.FontBodyBold;
            this.ForeColor = Color.White;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            this.Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _isPressed = true;
            this.Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            this.Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color currentBg = !this.Enabled ? Color.FromArgb(36, 40, 50) :
                              _isPressed ? PressedColor :
                              _isHovered ? HoverColor : NormalColor;

            Color textCol = !this.Enabled ? UiTheme.TextMuted : this.ForeColor;

            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (GraphicsPath path = UiTheme.GetRoundedRectangle(rect, BorderRadius))
            {
                using (SolidBrush brush = new SolidBrush(currentBg))
                {
                    pevent.Graphics.FillPath(brush, path);
                }

                if (BorderNormalColor != Color.Transparent && this.Enabled)
                {
                    using (Pen pen = new Pen(BorderNormalColor, 1f))
                    {
                        pevent.Graphics.DrawPath(pen, path);
                    }
                }

                TextRenderer.DrawText(
                    pevent.Graphics,
                    this.Text,
                    this.Font,
                    rect,
                    textCol,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak
                );
            }
        }
    }

    public class ModernProgressBar : Control
    {
        private int _value = 0;
        private int _maximum = 100;
        public int BorderRadius { get; set; } = 4;
        public Color BarColor { get; set; } = UiTheme.CyanAccent;
        public Color BarBackgroundColor { get; set; } = UiTheme.InputBg;

        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Max(0, Math.Min(_maximum, value));
                this.Invalidate();
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                _maximum = Math.Max(1, value);
                this.Invalidate();
            }
        }

        public ModernProgressBar()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.Height = 14;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);

            using (GraphicsPath bgPath = UiTheme.GetRoundedRectangle(rect, BorderRadius))
            {
                using (SolidBrush bgBrush = new SolidBrush(BarBackgroundColor))
                {
                    e.Graphics.FillPath(bgBrush, bgPath);
                }

                if (_value > 0)
                {
                    int fillWidth = (int)((float)_value / _maximum * (this.Width - 2));
                    if (fillWidth > BorderRadius * 2)
                    {
                        Rectangle fillRect = new Rectangle(1, 1, fillWidth, this.Height - 3);
                        using (GraphicsPath fillPath = UiTheme.GetRoundedRectangle(fillRect, BorderRadius))
                        using (LinearGradientBrush fillBrush = new LinearGradientBrush(fillRect, UiTheme.CyanAccent, UiTheme.TealSuccess, LinearGradientMode.Horizontal))
                        {
                            e.Graphics.FillPath(fillBrush, fillPath);
                        }
                    }
                    else if (fillWidth > 0)
                    {
                        using (SolidBrush fillBrush = new SolidBrush(BarColor))
                        {
                            e.Graphics.FillRectangle(fillBrush, 1, 1, fillWidth, this.Height - 3);
                        }
                    }
                }

                using (Pen borderPen = new Pen(UiTheme.BorderColor))
                {
                    e.Graphics.DrawPath(borderPen, bgPath);
                }
            }
        }
    }
}
