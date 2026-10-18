using System;
using System.IO;
using System.Text;
using Can_Bootloader_GUI.Parsers;
using Can_Bootloader_GUI.Models;

namespace Can_Bootloader_GUI.Tests
{
    public static class VerificationTests
    {
        public static void Run()
        {
            Console.WriteLine("=== Starting Self-Verification Tests ===");

            // 1. Test CRC32 standard test vector: "123456789" => 0xCBF43926
            byte[] testVector = Encoding.ASCII.GetBytes("123456789");
            uint crc = Crc32.Compute(testVector);
            Console.WriteLine($"[1] CRC32 Test: Expected=0xCBF43926, Actual=0x{crc:X8}");
            if (crc != 0xCBF43926) throw new Exception("CRC32 mismatch!");

            // 2. Test Intel HEX Parser
            // :020000040800F2 (Sum: 02+00+00+04+08+00+F2 = 0x100 -> 0x00)
            // :100000000010002001010008050100080701000898 (Sum: 0x68 + 0x98 = 0x100 -> 0x00)
            // :00000001FF (Sum: 00+00+00+01+FF = 0x100 -> 0x00)
            string sampleHex = 
":020000040800F2\r\n" +
":100000000010002001010008050100080701000898\r\n" +
":00000001FF\r\n";
            string tempHexPath = Path.Combine(Path.GetTempPath(), "test_firmware.hex");
            File.WriteAllText(tempHexPath, sampleHex);
            var hexInfo = HexBinParser.ParseHex(tempHexPath);
            Console.WriteLine($"[2] HEX Parser Test: Type={hexInfo.FileType}, Base=0x{hexInfo.BaseAddress:X8}, Bytes={hexInfo.TotalBytes}, CRC=0x{hexInfo.CalculatedCrc32:X8}");
            if (hexInfo.BaseAddress != 0x08000000 || hexInfo.TotalBytes != 16) throw new Exception("HEX parser failed!");
            File.Delete(tempHexPath);

            // 3. Test Raw BIN Parser
            string tempBinPath = Path.Combine(Path.GetTempPath(), "test_firmware.bin");
            File.WriteAllBytes(tempBinPath, new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 });
            var binInfo = HexBinParser.ParseBin(tempBinPath, 0x08008000);
            Console.WriteLine($"[3] BIN Parser Test: Type={binInfo.FileType}, Base=0x{binInfo.BaseAddress:X8}, Bytes={binInfo.TotalBytes}");
            if (binInfo.BaseAddress != 0x08008000 || binInfo.TotalBytes != 5) throw new Exception("BIN parser failed!");
            File.Delete(tempBinPath);

            // 4. Test 32-bit ELF synthetic generator & parser
            string tempElfPath = Path.Combine(Path.GetTempPath(), "test_firmware.elf");
            byte[] dummyElf = new byte[128];
            dummyElf[0] = 0x7F; dummyElf[1] = (byte)'E'; dummyElf[2] = (byte)'L'; dummyElf[3] = (byte)'F';
            dummyElf[4] = 1; // 32-bit
            dummyElf[5] = 1; // Little Endian
            BitConverter.GetBytes((uint)52).CopyTo(dummyElf, 28);
            BitConverter.GetBytes((ushort)32).CopyTo(dummyElf, 42);
            BitConverter.GetBytes((ushort)1).CopyTo(dummyElf, 44);

            int phOffset = 52;
            BitConverter.GetBytes((uint)1).CopyTo(dummyElf, phOffset + 0); // PT_LOAD
            BitConverter.GetBytes((uint)84).CopyTo(dummyElf, phOffset + 4);
            BitConverter.GetBytes((uint)0x08000000).CopyTo(dummyElf, phOffset + 8);
            BitConverter.GetBytes((uint)0x08000000).CopyTo(dummyElf, phOffset + 12);
            BitConverter.GetBytes((uint)16).CopyTo(dummyElf, phOffset + 16);
            BitConverter.GetBytes((uint)16).CopyTo(dummyElf, phOffset + 20);

            for (int i = 0; i < 16; i++) dummyElf[84 + i] = (byte)(i + 1);

            File.WriteAllBytes(tempElfPath, dummyElf);
            var elfInfo = ElfParser.Parse(tempElfPath);
            Console.WriteLine($"[4] ELF Parser Test: Type={elfInfo.FileType}, Base=0x{elfInfo.BaseAddress:X8}, Bytes={elfInfo.TotalBytes}, CRC=0x{elfInfo.CalculatedCrc32:X8}");
            if (elfInfo.BaseAddress != 0x08000000 || elfInfo.TotalBytes != 16) throw new Exception("ELF parser failed!");
            File.Delete(tempElfPath);

            Console.WriteLine("=== All Verification Tests PASSED! ===");
        }
    }
}
