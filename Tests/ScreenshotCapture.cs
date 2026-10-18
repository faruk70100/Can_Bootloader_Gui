using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using Can_Bootloader_GUI.UI;

namespace Can_Bootloader_GUI.Tests
{
    public static class ScreenshotCapture
    {
        public static void Capture(string savePath)
        {
            var thread = new Thread(() =>
            {
                using (var form = new MainForm())
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(50, 50);
                    form.Show();
                    form.Refresh();
                    Application.DoEvents();
                    Thread.Sleep(300);

                    using (var bmp = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
                        bmp.Save(savePath, ImageFormat.Png);
                    }
                    form.Close();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
