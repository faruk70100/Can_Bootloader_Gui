using System;

namespace Can_Bootloader_GUI.Models
{
    /// <summary>
    /// Standard CAN 2.0A / 2.0B frame representation
    /// </summary>
    public class CanMessage
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsTx { get; set; }
        public uint Id { get; set; }
        public byte Dlc { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string Description { get; set; } = string.Empty;

        public string DirectionStr => IsTx ? "TX" : "RX";
        public string IdHex => $"0x{Id:X3}";
        public string DataHex => BitConverter.ToString(Data).Replace("-", " ");
        public string TimestampStr => Timestamp.ToString("HH:mm:ss.fff");

        public CanMessage() { }

        public CanMessage(uint id, byte[] data, bool isTx = true, string description = "")
        {
            Timestamp = DateTime.Now;
            Id = id;
            Dlc = (byte)(data?.Length ?? 0);
            Data = data ?? Array.Empty<byte>();
            IsTx = isTx;
            Description = description;
        }
    }

    /// <summary>
    /// Parsed firmware container
    /// </summary>
    public class FirmwareInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty; // ELF, HEX, BIN
        public uint BaseAddress { get; set; }
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public int TotalBytes => RawData?.Length ?? 0;
        public int TotalPackets => (TotalBytes + 7) / 8;
        public uint CalculatedCrc32 { get; set; }
    }
}
