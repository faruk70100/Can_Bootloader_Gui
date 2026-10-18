using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Can_Bootloader_GUI.Models;

namespace Can_Bootloader_GUI.Parsers
{
    /// <summary>
    /// Pure C# Intel HEX (.hex) and Raw Binary (.bin) Parsers.
    /// Handles Records 00 (Data), 01 (EOF), 02 (Segment Address), 04 (Extended Linear Address).
    /// </summary>
    public static class HexBinParser
    {
        public const uint DEFAULT_BIN_BASE_ADDRESS = 0x08008000;

        /// <summary>
        /// Parses an Intel HEX (.hex) file into contiguous flash bytes with 0xFF padding.
        /// </summary>
        public static FirmwareInfo ParseHex(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"HEX file not found: {filePath}");

            string[] lines = File.ReadAllLines(filePath);
            var byteMap = new SortedDictionary<uint, byte>();

            uint upperAddress = 0;

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || !line.StartsWith(":"))
                    continue;

                // Minimal line: :00000001FF (11 chars)
                if (line.Length < 11)
                    continue;

                // Validate checksum
                byte byteCount = byte.Parse(line.Substring(1, 2), NumberStyles.HexNumber);
                ushort addressOffset = ushort.Parse(line.Substring(3, 4), NumberStyles.HexNumber);
                byte recordType = byte.Parse(line.Substring(7, 2), NumberStyles.HexNumber);

                int expectedLength = 1 + 2 + 4 + 2 + (byteCount * 2) + 2;
                if (line.Length < expectedLength)
                    throw new InvalidDataException($"Malformed HEX line: {line}");

                // Verify checksum
                byte sum = 0;
                for (int i = 1; i < expectedLength; i += 2)
                {
                    sum += byte.Parse(line.Substring(i, 2), NumberStyles.HexNumber);
                }
                if (sum != 0)
                {
                    throw new InvalidDataException($"HEX checksum mismatch in line: {line}");
                }

                if (recordType == 0x00) // Data Record
                {
                    uint fullAddress = upperAddress + addressOffset;
                    for (int i = 0; i < byteCount; i++)
                    {
                        byte b = byte.Parse(line.Substring(9 + (i * 2), 2), NumberStyles.HexNumber);
                        byteMap[fullAddress + (uint)i] = b;
                    }
                }
                else if (recordType == 0x01) // End of File
                {
                    break;
                }
                else if (recordType == 0x02) // Extended Segment Address (shift 4 bits)
                {
                    ushort seg = ushort.Parse(line.Substring(9, 4), NumberStyles.HexNumber);
                    upperAddress = (uint)seg << 4;
                }
                else if (recordType == 0x04) // Extended Linear Address (shift 16 bits)
                {
                    ushort ext = ushort.Parse(line.Substring(9, 4), NumberStyles.HexNumber);
                    upperAddress = (uint)ext << 16;
                }
            }

            if (byteMap.Count == 0)
                throw new InvalidDataException("HEX file contains no valid data records.");

            uint baseAddress = byteMap.Keys.First();
            uint maxAddress = byteMap.Keys.Last();
            int totalBytes = (int)(maxAddress - baseAddress + 1);

            byte[] firmwareBuffer = new byte[totalBytes];
            for (int i = 0; i < totalBytes; i++)
                firmwareBuffer[i] = 0xFF; // Unwritten flash fill

            foreach (var kvp in byteMap)
            {
                firmwareBuffer[kvp.Key - baseAddress] = kvp.Value;
            }

            uint crc = Crc32.Compute(firmwareBuffer);

            return new FirmwareInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileType = "HEX",
                BaseAddress = baseAddress,
                RawData = firmwareBuffer,
                CalculatedCrc32 = crc
            };
        }

        /// <summary>
        /// Loads a raw binary (.bin) file with a specified or default base address.
        /// </summary>
        public static FirmwareInfo ParseBin(string filePath, uint baseAddress = DEFAULT_BIN_BASE_ADDRESS)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"BIN file not found: {filePath}");

            byte[] rawBytes = File.ReadAllBytes(filePath);
            if (rawBytes.Length == 0)
                throw new InvalidDataException("BIN file is empty.");

            uint crc = Crc32.Compute(rawBytes);

            return new FirmwareInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileType = "BIN",
                BaseAddress = baseAddress,
                RawData = rawBytes,
                CalculatedCrc32 = crc
            };
        }

        /// <summary>
        /// Auto-detects file extension (.elf, .hex, .bin) and calls appropriate parser.
        /// </summary>
        public static FirmwareInfo LoadFirmware(string filePath, uint binBaseAddress = DEFAULT_BIN_BASE_ADDRESS)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext switch
            {
                ".elf" => ElfParser.Parse(filePath),
                ".hex" => ParseHex(filePath),
                ".bin" => ParseBin(filePath, binBaseAddress),
                _ => throw new NotSupportedException($"Unsupported firmware file format: {ext}. Only .elf, .hex, and .bin are supported.")
            };
        }
    }
}
