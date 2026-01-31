# Pandowdy.CPU Emulator

A cycle-accurate 6502/65C02 CPU emulator written in C# for .NET 8.

## Features

- **Cycle-Accurate Emulation** — Micro-op pipeline architecture provides true cycle-level timing
- **Instance-Based API** — Create CPU instances via `CpuFactory.Create()` with injected state buffers
- **Multiple CPU Variants** — NMOS 6502, 65C02, and Rockwell 65C02 with bit manipulation instructions
- **Double-Buffered State** — Clean instruction boundaries for debugging, single-stepping, and state comparison
- **Interrupt Support** — IRQ, NMI, and Reset with proper priority handling and variant-specific D-flag behavior
- **Extensible Bus Interface** — Simple `IPandowdyCpuBus` interface for custom memory maps and I/O
- **Pure C#** — No external dependencies beyond .NET 8

## Validation

All CPU variants pass [Klaus Dormann's 6502/65C02 Functional Tests](https://github.com/Klaus2m5/6502_65C02_functional_tests), a standard test suite for 6502 emulator validation:

| Test | Nmos6502 | Nmos6502Simple | Wdc65C02 | Rockwell65C02 |
|------|----------|----------------|----------|---------------|
| 6502 Functional Test | ✓ | ✓ | ✓ | ✓ |
| 6502 Decimal Test | ✓ | ✓ | ✓ | ✓ |
| 6502 Interrupt Test | ✓ | ✓ | ✓ | ✓ |
| 65C02 Extended Opcodes Test | — | — | ✓ | ✓ |

### Cycle-Accurate Validation

All CPU variants are **cycle-accurate** and pass the [Tom Harte SingleStepTests](https://github.com/SingleStepTests/65x02), which validate not only final register state but also cycle-by-cycle bus activity for every opcode:

| Variant | Pass Rate | Coverage |
|---------|-----------|-------|
| Nmos6502 | 100% (256 opcodes × 10,000 tests each) | All opcodes tested |
| Nmos6502Simple | 100% (151 opcodes × 10,000 tests each) | Only documented opcodes tested | 
| Wdc65C02 | 100% (254 opcodes × 10,000 tests each) | All opcodes except WAI & STP tested |
| Rockwell65C02 | 100% (256 opcodes × 10,000 tests each) | All opcodes tested |

See [Pandowdy.Cpu.Harte-SST-Tests/README.md](Pandowdy.Cpu.Harte-SST-Tests/README.md) for instructions on running these tests.

## Supported CPU Variants

| Variant | Class | Description |
|---------|-------|-------------|
| `Nmos6502` | `Cpu6502` | Original NMOS 6502 with illegal opcodes |
| `Nmos6502Simple` | `Cpu6502Simple` | NMOS 6502 with undefined opcodes as NOPs |
| `Wdc65C02` | `Cpu65C02` | Later WDC 65C02 (W65C02S) with all CMOS instructions including RMB/SMB/BBR/BBS |
| `Rockwell65C02` | `Cpu65C02Rockwell` | Rockwell 65C02 (same as WDC but WAI/STP are NOPs) |

## Quick Start

```csharp
using Pandowdy.Cpu;

// Create memory bus and CPU state buffer
var bus = new RamBus();
var cpuBuffer = new CpuStateBuffer();

// Create CPU instance
var cpu = CpuFactory.Create(CpuVariant.Wdc65C02, cpuBuffer);

// Load program and set reset vector
byte[] program = { 0xA9, 0x42, 0x8D, 0x00, 0x02 }; // LDA #$42, STA $0200
bus.LoadProgram(0x0400, program);
bus.SetResetVector(0x0400);

// Reset and execute
cpu.Reset(bus);
int cycles = cpu.Step(bus);

Console.WriteLine($"A = ${cpuBuffer.Current.A:X2}"); // A = $42
```

## Project Structure

```
Pandowdy.Cpu/              # Core library (CPU classes, MicroOps, Pipelines, CpuState, CpuStateBuffer)
Pandowdy.Cpu.Tests/        # xUnit test suite (2,185 tests)
Pandowdy.Cpu.Dormann-Tests/ # Klaus Dormann functional test runner
Pandowdy.Cpu.Harte-SST-Tests/ # Tom Harte SingleStepTests runner
```

## Documentation

- [CPU Usage Guide](docs/CpuUsageGuide.md) — Detailed usage instructions and examples
- [API Reference](docs/ApiReference.md) — Complete API documentation for IPandowdyCpu, CpuFactory, CpuState, and CpuStateBuffer
- [Building](BUILDING.md) — Build instructions, running tests, and project structure

## Quick Build

```bash
dotnet build
dotnet test
```

For detailed build instructions, external test suites, and troubleshooting, see [BUILDING.md](BUILDING.md).

## Migration from v2.x

If you're upgrading from v2.x, see the [Migration Guide](#migration-from-v2x-to-v30) below.

## License

Licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE) for details.

---

## Migration from v2.x to v3.0

v3.0 introduces a new instance-based API, replacing the static `Cpu` class.

### Before (v2.x)

```csharp
var buffer = new CpuStateBuffer();
Cpu.Reset(buffer, bus);
Cpu.Step(CpuVariant.WDC65C02, buffer, bus);
buffer.Current.SignalIrq();
buffer.Current.HandlePendingInterrupt(bus);
```

### After (v3.0)

```csharp
var buffer = new CpuStateBuffer();
var cpu = CpuFactory.Create(CpuVariant.Wdc65C02, buffer);
cpu.Reset(bus);
cpu.Step(bus);
cpu.SignalIrq();
cpu.HandlePendingInterrupt(bus);
```

### Breaking Changes

| Change | Migration |
|--------|-----------|
| Static `Cpu` class removed | Use `CpuFactory.Create()` to create CPU instances |
| `CpuVariant` enum renamed | `NMOS6502` → `Nmos6502`, `WDC65C02` → `Wdc65C02`, etc. |
| Interrupt methods moved from `CpuState` to `IPandowdyCpu` | Use `cpu.SignalIrq()` instead of `buffer.Current.SignalIrq()` |
| `ClearDecimalOnInterrupt` removed | D-flag behavior is automatic based on CPU variant |

### New Features in v3.0

- **`CpuState.CurrentOpcode`** — The opcode byte currently being executed
- **`CpuState.OpcodeAddress`** — The address from which the opcode was read
- **`CpuState.Clone()`** — Create a deep copy of CPU state for save states
- **Swappable buffers** — `cpu.Buffer` is settable, allowing buffer swapping without creating a new CPU

## Author

Copyright 2026 Mark D. Long
