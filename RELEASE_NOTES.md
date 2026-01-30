# Pandowdy.Cpu Release Notes

---

## v2.0.0

**Release Date:** January 30, 2026

### What's New

- **Pure C# Implementation**: The entire CPU emulator is now implemented in C#, simplifying the build process and reducing dependencies.
- **Single Package**: All functionality is in the `Pandowdy.Cpu` package:
  - `Cpu` static class with `Clock`, `Step`, `Run`, `Reset`, `CurrentOpcode`, and `CyclesRemaining` methods
  - `MicroOps` static class with all micro-operation implementations
  - `Pipelines` static class with all opcode pipeline definitions
  - `CpuState`, `CpuStateBuffer`, `CpuVariant`, `CpuStatus` types
  - `IPandowdyCpuBus` interface

### Validation

- All 2,149 unit tests pass
- Klaus Dormann functional tests pass for all variants
- Tom Harte SingleStepTests pass for all variants (cycle-accurate validation)

---

## v1.0.0

**Release Date:** January 2025

Initial release of the cycle-accurate 6502/65C02 CPU emulator for .NET.

## Features

- **Cycle-Accurate Emulation** — Micro-op pipeline architecture provides true cycle-level timing, validated against real hardware traces
- **Four CPU Variants:**
  - `NMOS6502` — Original NMOS 6502 with illegal/undocumented opcodes
  - `NMOS6502_NO_ILLEGAL` — NMOS 6502 with undefined opcodes as NOPs
  - `WDC65C02` — Later WDC 65C02 (W65C02S) with all CMOS instructions including RMB/SMB/BBR/BBS
  - `ROCKWELL65C02` — Rockwell 65C02 (same as WDC but WAI/STP are NOPs)
- **Double-Buffered State** — Clean instruction boundaries for debugging, single-stepping, and state comparison
- **Full Interrupt Support** — IRQ, NMI, and Reset with proper priority handling
- **Stateless Core** — CPU execution engine is stateless; all state lives in `CpuStateBuffer`

## Validation

All CPU variants pass industry-standard test suites:

| Test Suite | Coverage |
|------------|----------|
| Klaus Dormann 6502 Functional Test | ✅ All variants |
| Klaus Dormann Decimal Test | ✅ All variants |
| Klaus Dormann 65C02 Extended Opcodes | ✅ WDC65C02, ROCKWELL65C02 |
| Tom Harte SingleStepTests | ✅ 100% pass rate, all variants |

The Tom Harte tests validate not only final register state but also **cycle-by-cycle bus activity** for every opcode.

## Requirements

- .NET 8.0 or later

## Installation

### NuGet Package Manager
```
Install-Package Pandowdy.Cpu -Version 1.0.0
```

### .NET CLI
```
dotnet add package Pandowdy.Cpu --version 1.0.0
```

### PackageReference
```xml
<PackageReference Include="Pandowdy.Cpu" Version="1.0.0" />
```

## Quick Start

```csharp
using Pandowdy.Cpu;

var bus = new RamBus();
var cpuBuffer = new CpuStateBuffer();

// Load program and reset vector
byte[] program = { 0xA9, 0x42, 0x8D, 0x00, 0x02 }; // LDA #$42, STA $0200
bus.LoadProgram(0x0400, program);
bus.SetResetVector(0x0400);

// Reset and execute
Cpu.Reset(cpuBuffer, bus);
int cycles = Cpu.Step(CpuVariant.WDC65C02, cpuBuffer, bus);

Console.WriteLine($"A = ${cpuBuffer.Current.A:X2}"); // A = $42
```

## Documentation

- [README](https://github.com/markdavidlong/Pandowdy.Cpu/blob/main/README.md)
- [CPU Usage Guide](https://github.com/markdavidlong/Pandowdy.Cpu/blob/main/docs/CpuUsageGuide.md)
- [API Reference](https://github.com/markdavidlong/Pandowdy.Cpu/blob/main/docs/ApiReference.md)

## License

Apache License 2.0

## Author

Copyright 2026 Mark D. Long
