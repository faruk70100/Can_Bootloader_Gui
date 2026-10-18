using System;

namespace Can_Bootloader_GUI.Parsers
{
    /// <summary>
    /// Pure C# IEEE 802.3 CRC32 Implementation (Standard 32-bit polynomial: 0xEDB88320)
    /// Used by Ethernet, ZIP, and STM32 Bootloader CRC verification.
    /// </summary>
    public static class Crc32
    {
        private const uint Polynomial = 0xEDB88320u;
        private static readonly uint[] Table = new uint[256];

        static Crc32()
        {
            for (uint i = 0; i < 256; i++)
            {
                uint entry = i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ Polynomial;
                    else
                        entry >>= 1;
                }
                Table[i] = entry;
            }
        }

        /// <summary>
        /// Computes the IEEE 802.3 CRC32 checksum over the provided byte array.
        /// </summary>
        public static uint Compute(byte[] buffer)
        {
            if (buffer == null) return 0;
            return Compute(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Computes the IEEE 802.3 CRC32 checksum over a slice of the provided byte array.
        /// </summary>
        public static uint Compute(byte[] buffer, int offset, int length)
        {
            if (buffer == null || length <= 0) return 0;

            uint crc = 0xFFFFFFFFu;
            for (int i = offset; i < offset + length; i++)
            {
                byte tableIndex = (byte)((crc & 0xFF) ^ buffer[i]);
                crc = (crc >> 8) ^ Table[tableIndex];
            }

            return ~crc;
        }
    }
}
