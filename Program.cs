using System;
using System.IO;
using System.Windows.Forms;
using Can_Bootloader_GUI.UI;
using Can_Bootloader_GUI.Tests;

namespace Can_Bootloader_GUI
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--test")
            {
                VerificationTests.Run();
                return;
            }

            if (args.Length > 1 && args[0] == "--screenshot")
            {
                ScreenshotCapture.Capture(args[1]);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
