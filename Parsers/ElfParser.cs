using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Can_Bootloader_GUI.Models;

namespace Can_Bootloader_GUI.Parsers
{
    /// <summary>
    /// Pure C# 32-bit Little-Endian ELF Parser for ARM GCC / STM32CubeIDE builds.
    /// Extracts PT_LOAD segments targeting STM32 Flash (>= 0x08000000), pads internal gaps with 0xFF.
    /// </summary>
    public static class ElfParser
    {
        private const uint PT_LOAD = 1;
        private const uint STM32_FLASH_BASE = 0x08000000;

        private class Segment
        {
            public uint PhysicalAddress { get; set; }
            public uint VirtualAddress { get; set; }
            public uint FileOffset { get; set; }
            public uint FileSize { get; set; }
            public uint MemorySize { get; set; }
            public byte[] Data { get; set; } = Array.Empty<byte>();
        }

        public static FirmwareInfo Parse(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"ELF file not found: {filePath}");

            byte[] elfBytes = File.ReadAllBytes(filePath);

            if (elfBytes.Length < 52)
                throw new InvalidDataException("File is too small to be a valid 32-bit ELF binary.");

            // 1. Verify Magic: 0x7F 'E' 'L' 'F'
            if (elfBytes[0] != 0x7F || elfBytes[1] != (byte)'E' || elfBytes[2] != (byte)'L' || elfBytes[3] != (byte)'F')
                throw new InvalidDataException("Invalid ELF magic header.");

            // 2. EI_CLASS: 1 = 32-bit
            if (elfBytes[4] != 1)
                throw new NotSupportedException("Only 32-bit ELF files are supported for STM32 targets.");

            // 3. EI_DATA: 1 = Little Endian
            if (elfBytes[5] != 1)
                throw new NotSupportedException("Only Little-Endian ELF files are supported for STM32 targets.");

            // 4. ELF Header fields
            // e_phoff: Program header table offset (bytes 28..31)
            uint phOff = BitConverter.ToUInt32(elfBytes, 28);
            // e_phentsize: Program header entry size (bytes 42..43)
            ushort phEntSize = BitConverter.ToUInt16(elfBytes, 42);
            // e_phnum: Number of program header entries (bytes 44..45)
            ushort phNum = BitConverter.ToUInt16(elfBytes, 44);

            if (phOff == 0 || phNum == 0)
                throw new InvalidDataException("No program headers found in ELF file.");

            var segments = new List<Segment>();

            for (int i = 0; i < phNum; i++)
            {
                int entryStart = (int)(phOff + (i * phEntSize));
                if (entryStart + 32 > elfBytes.Length)
                    break;

                uint pType = BitConverter.ToUInt32(elfBytes, entryStart + 0);
                uint pOffset = BitConverter.ToUInt32(elfBytes, entryStart + 4);
                uint pVaddr = BitConverter.ToUInt32(elfBytes, entryStart + 8);
                uint pPaddr = BitConverter.ToUInt32(elfBytes, entryStart + 12);
                uint pFilesz = BitConverter.ToUInt32(elfBytes, entryStart + 16);
                uint pMemsz = BitConverter.ToUInt32(elfBytes, entryStart + 20);

                // Only consider PT_LOAD segments with non-zero file size
                if (pType == PT_LOAD && pFilesz > 0)
                {
                    uint targetAddr = (pPaddr >= STM32_FLASH_BASE) ? pPaddr : pVaddr;

                    // Filter segments placed in STM32 Flash (address space 0x08000000 - 0x08FFFFFF)
                    if (targetAddr >= STM32_FLASH_BASE && targetAddr < 0x09000000)
                    {
                        if (pOffset + pFilesz <= elfBytes.Length)
                        {
                            byte[] segData = new byte[pFilesz];
                            Array.Copy(elfBytes, (int)pOffset, segData, 0, (int)pFilesz);

                            segments.Add(new Segment
                            {
                                PhysicalAddress = targetAddr,
                                VirtualAddress = pVaddr,
                                FileOffset = pOffset,
                                FileSize = pFilesz,
                                MemorySize = pMemsz,
                                Data = segData
                            });
                        }
                    }
                }
            }

            if (segments.Count == 0)
                throw new InvalidDataException("No valid flash-loadable segments (0x08000000 range) found in ELF.");

            // Sort segments by address
            segments = segments.OrderBy(s => s.PhysicalAddress).ToList();

            uint baseAddress = segments.First().PhysicalAddress;
            uint maxAddress = segments.Max(s => s.PhysicalAddress + s.FileSize);
            int totalFlashSpan = (int)(maxAddress - baseAddress);

            // Pre-fill memory image with 0xFF (erased flash state)
            byte[] firmwareBuffer = new byte[totalFlashSpan];
            for (int i = 0; i < firmwareBuffer.Length; i++)
                firmwareBuffer[i] = 0xFF;

            // Copy segment contents into contiguous image
            foreach (var seg in segments)
            {
                int bufferOffset = (int)(seg.PhysicalAddress - baseAddress);
                Array.Copy(seg.Data, 0, firmwareBuffer, bufferOffset, seg.Data.Length);
            }

            uint crc = Crc32.Compute(firmwareBuffer);

            return new FirmwareInfo
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath,
                FileType = "ELF",
                BaseAddress = baseAddress,
                RawData = firmwareBuffer,
                CalculatedCrc32 = crc
            };
        }
    }
}
