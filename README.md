<div align="center">

# 🚀 Can Bootloader GUI

**Industrial-grade CAN firmware updater for STM32 — pure C#, zero third-party dependencies.**

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?logo=windows&logoColor=white)
![UI](https://img.shields.io/badge/UI-Windows%20Forms-00BCD4)
![MCU](https://img.shields.io/badge/target-STM32-03234B)
![Adapter](https://img.shields.io/badge/adapter-Waveshare%20USB--CAN--A-green)
![License](https://img.shields.io/badge/license-MIT-lightgrey)

</div>

---

## 📖 Overview

**Can Bootloader GUI** is a Windows desktop application for flashing firmware to **STM32** microcontrollers over a **CAN bus**, using the **Waveshare USB-CAN-A** adapter. Load an `.elf`, `.hex` or `.bin` image, and the app erases, transfers, and verifies it (IEEE 802.3 CRC32) through a lightweight CAN bootloader protocol — all with a live CAN traffic monitor and target telemetry.


## ✨ Features

- 🔌 **Native Waveshare USB-CAN-A driver** — no vendor DLLs required
- 📦 **Built-in ELF / HEX / BIN parsers** — no external libraries
- 🔐 **CRC32 verification** with hardware-computed result from the target
- 📡 **Real-time CAN monitor** with autoscroll, 5,000-entry buffer and CSV export
- 🧪 **Target test module** — LED blink command and live telemetry
- 🛑 **Cancellable transfers** via `CancellationToken`
- 🌑 **Modern industrial dark theme** (Cyan & Neon Green accents)

## 🧩 Architecture

| Component | File | Responsibility |
|---|---|---|
| USB-CAN driver | `WaveshareUsbCanDriver.cs` | Serial link, CAN config, TX/RX framing |
| ELF parser | `ElfParser.cs` | 32-bit LE ELF, `PT_LOAD` scan, Flash filtering, `0xFF` gap fill |
| HEX / BIN parser | `HexBinParser.cs` | Intel HEX decoding + checksum, raw BIN loading |
| CRC32 engine | `Crc32.cs` | IEEE 802.3 (`0xEDB88320`) |
| Protocol state machine | `BootloaderProtocol.cs` | Erase → Write → Complete → Verify flow |

### 🔌 Hardware & Driver

| Parameter | Value |
|---|---|
| USB serial baudrate | `2,000,000 bps`, 8N1 |
| CAN baudrate | 1 Mbps (default), 800k, 500k, 250k, 125k, … |
| Config frame (20 bytes) | `0xAA 0x55 0x12 [BaudCode] 0x01 … 0x55` |
| TX frame (CAN 2.0A, 11-bit) | `0xAA [0xC0 \| DLC] [ID_Low] [ID_High] [Data] 0x55` |
| RX parser | Sliding ring buffer with frame re-synchronization |

### 📁 Firmware Parsers

- **ELF** — 32-bit little-endian (`0x7F 'E' 'L' 'F'`), scans `PT_LOAD` segments, keeps only STM32 Flash (`0x08000000`) and pads gaps with `0xFF`.
- **Intel HEX** — record types `00` Data, `01` EOF, `02` Extended Segment, `04` Extended Linear Address; checksum validated, `0xFF` padding.
- **BIN** — raw image with configurable base address (default `0x08008000`).

## 📡 CAN Bootloader Protocol

| CAN ID | DLC | Direction | Description |
|:---:|:---:|:---:|---|
| `0x700` | 5 | PC → Target | **Erase** command: `Data[0]=0x01`, `Data[1..4]` = total size (LE) |
| `0x703` | 1 | Target → PC | **Erase ACK**: `0x01` success, `0x00` fail (4 s timeout) |
| `0x701` | ≤ 8 | PC → Target | **Firmware data** chunk (configurable inter-packet delay) |
| `0x704` | 4 | Target → PC | **Write ACK** (periodic) — drives the progress bar |
| `0x702` | 1 | PC → Target | **Transfer completed** |
| `0x705` | 4 | Target → PC | **CRC32 result** (hardware IEEE 802.3) |
| `0x706` | 2 | Target → PC | **Error report** |

**Error codes (`0x706`):** `1` Erase · `2` Program · `3` Overflow · `4` Sequence

### Update flow

```
PC                                  Target (STM32)
 │── 0x700 Erase (size) ───────────────▶│
 │◀────────────── 0x703 Erase ACK ──────│
 │── 0x701 Data chunks ────────────────▶│
 │◀────────────── 0x704 Write ACK ──────│  (periodic)
 │── 0x702 Transfer completed ─────────▶│
 │◀────────────── 0x705 CRC32 ──────────│  → PC compares CRC
```

## 🧪 Target Test & Live Telemetry

| CAN ID | DLC | Direction | Payload |
|:---:|:---:|:---:|---|
| `0x710` | 2 | PC → Target | LED blink period in ms (`uint16` LE) |
| `0x720` | 8 | Target → PC | Counter (`uint32` LE), Blink period (`uint16` LE), Status `0xAA`, Tail `0x55` |

## ⚙️ Getting Started

### Prerequisites

- Windows 10 / 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows Desktop) — .NET 8 SDK also works
- Waveshare USB-CAN-A adapter + CAN transceiver wired to your STM32

### Build

```bash
git clone https://github.com/<your-username>/<your-repo>.git
cd <your-repo>
dotnet build -c Release
```

### Run

```bash
dotnet run -c Release
```

### Self-verification tests

```bash
dotnet run -c Release -- --test
```

## 🕹️ Usage

1. Connect the Waveshare USB-CAN-A adapter and select its **COM port**.
2. Choose the **CAN baudrate** (must match the bootloader — default 1 Mbps).
3. Load a firmware file (`.elf`, `.hex` or `.bin`; set the base address for BIN).
4. Click **Flash** and watch the progress bar and CAN monitor.
5. The app compares the target's CRC32 with the computed one and reports the result.

> ⚠️ The target must be running a compatible CAN bootloader that implements the protocol above.

## 🗺️ Roadmap

- [ ] Extended (29-bit) CAN ID support
- [ ] Multi-node addressing
- [ ] Additional CAN adapters
- [ ] Automated CI build (GitHub Actions)

## 🤝 Contributing

Contributions are welcome! Fork the repo, create a feature branch, and open a pull request. For major changes, please open an issue first.

## 📄 License

Distributed under the **MIT License**. See `LICENSE` for details. *(Change this if you use a different license.)*
