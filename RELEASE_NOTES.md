# Pandowdy.Cpu Release Notes

---

## v3.0.0

**Release Date:** TBD

### Overview

v3.0 is a major refactoring that replaces the static `Cpu` class with an instance-based API using `IPandowdyCpu` and `CpuFactory`. This enables cleaner code, better testability, and variant-specific behavior without passing parameters to every method.

### Breaking Changes

#### API Changes

| Change | Details |
|--------|---------|
| **Static `Cpu` class removed** | Use `CpuFactory.Create(variant, buffer)` to create CPU instances |
| **`CpuVariant` enum renamed** | `NMOS6502` → `Nmos6502`, `NMOS6502_NO_ILLEGAL` → `Nmos6502Simple`, `WDC65C02` → `Wdc65C02`, `ROCKWELL65C02` → `Rockwell65C02` |
| **Interrupt methods moved** | `SignalIrq()`, `SignalNmi()`, `SignalReset()`, `ClearIrq()`, `HandlePendingInterrupt()` moved from `CpuState` to `IPandowdyCpu` |
| **`ClearDecimalOnInterrupt` removed** | D-flag behavior is now automatic based on CPU variant |

#### Migration Example

**Before (v2.x):**
```csharp
var buffer = new CpuStateBuffer();
Cpu.Reset(buffer, bus);
Cpu.Step(CpuVariant.WDC65C02, buffer, bus);
buffer.Current.SignalIrq();
buffer.Current.HandlePendingInterrupt(bus);
```

**After (v3.0):**
```csharp
var buffer = new CpuStateBuffer();
var cpu = CpuFactory.Create(CpuVariant.Wdc65C02, buffer);
cpu.Reset(bus);
cpu.Step(bus);
cpu.SignalIrq();
cpu.HandlePendingInterrupt(bus);
```

### New Features

#### Instance-Based CPU API

- **`IPandowdyCpu` interface** — Defines the contract for all CPU implementations
- **`CpuFactory.Create()`** — Factory method to create CPU instances by variant
- **Concrete CPU classes** — `Cpu6502`, `Cpu6502Simple`, `Cpu65C02`, `Cpu65C02Rockwell`

#### Variant-Specific Interrupt Behavior

The D-flag clearing on interrupts is now automatic based on CPU variant:
- **NMOS 6502** (`Cpu6502`, `Cpu6502Simple`): D flag is **NOT** cleared on IRQ/NMI/BRK
- **65C02** (`Cpu65C02`, `Cpu65C02Rockwell`): D flag **IS** cleared on IRQ/NMI/BRK

No configuration property needed — the correct behavior is built into each CPU class.

#### New CpuState Properties

| Property | Description |
|----------|-------------|
| `CurrentOpcode` | The opcode byte currently being executed |
| `OpcodeAddress` | The address from which the opcode was read |

These properties are set during instruction fetch, eliminating guesswork about the PC's relationship to the current instruction.

#### CpuState.Clone()

```csharp
CpuState Clone()
```

Creates a deep copy of CPU state. Useful for save states or when you need an independent copy. For hot-path updates, use `CopyFrom()` instead to avoid allocation.

#### Swappable Buffers

The `cpu.Buffer` property is now settable, allowing you to swap state buffers at runtime:

```csharp
var cpu = CpuFactory.Create(CpuVariant.Nmos6502, buffer1);
cpu.Step(bus1);

// Swap to different state (no need to create new CPU)
cpu.Buffer = buffer2;
cpu.Step(bus2);
```

### Improvements

- **Test coverage**: 2,185 unit tests, 95.8% code coverage
- **All Dormann tests pass**: Functional, Decimal, Interrupt (with WAI), and Extended Opcodes tests
- **All Harte tests pass**: Cycle-accurate validation for all variants
- **Cleaner code**: Interrupt handling logic is now in CPU classes where it belongs

### Internal Changes

- `CpuBase` abstract class contains shared execution logic
- Each CPU class sets `_clearDecimalOnInterrupt` appropriately
- Pipelines are cached per-variant in the CPU instance
- Removed obsolete `ClearDecimalOnInterrupt` workaround property

---

## v2.1.0

**Release Date:** January 30, 2026

### Critical Bug Fix

- **CpuStateBuffer state comparison logic fixed**: The `Prev` and `Current` states in `CpuStateBuffer` were not being managed correctly for before/after instruction comparison.

**Previous (incorrect) behavior in v2.0.0:**
- At instruction completion, buffers were swapped and `Current` was overwritten
- After `Step()` returned, `Prev` and `Current` contained the same values
- Comparing `Prev` vs `Current` showed no differences

**New (correct) behavior in v2.1.0:**
- At the start of a new instruction cycle, `Current` is copied to `Prev` (saving the "before" state)
- Micro-ops modify `Current` during execution
- After `Step()` returns, `Prev` = before, `Current` = after
- The pipeline is not reset until the next instruction cycle begins
- Comparing `Prev` vs `Current` correctly shows what changed during the instruction

### Namespace Reorganization

- **Clean public API**: User-facing types remain in `Pandowdy.Cpu` namespace
- **Internal implementation hidden**: `MicroOp`, `MicroOps`, and `Pipelines` moved to `Pandowdy.Cpu.Internals` namespace and marked `internal`
- **CpuState cleanup**: `Pipeline` and `PipelineIndex` properties are now `internal`

**Public API (`Pandowdy.Cpu`):**
- `Cpu` - Static execution engine
- `CpuState` - CPU registers and flags
- `CpuStateBuffer` - Double-buffered state for debugging
- `CpuVariant`, `CpuStatus` - Enums
- `IPandowdyCpuBus` - Bus interface

**Internals (`Pandowdy.Cpu.Internals`):**
- `MicroOp` - Micro-operation delegate
- `MicroOps` - Micro-operation implementations
- `Pipelines` - Opcode pipeline tables

### New API

- Added `SaveStateBeforeInstruction()` method to `CpuStateBuffer` (called internally by `Cpu.Clock()`)

---

## v2.0.0

**Release Date:** January 30, 2026

### What's New

- **Pure C# Implementation**: The entire CPU emulator is now implemented in C#, simplifying the build process and reducing dependencies.
- **Single Package**: All functionality is in the `Pandowdy.Cpu` package:
  - `Cpu` static class with `Clock`, `Step`, `Run`, `Reset`, `CurrentOpcode`, and `CyclesRemaining` methods
  - `CpuState`, `CpuStateBuffer`, `CpuVariant`, `CpuStatus` types
  - `IPandowdyCpuBus` interface

### Bug

- **Critical**: `CpuStateBuffer` did not correctly preserve before/after state for instruction comparison. Fixed in v2.1.0.

### Features

- **Cycle-Accurate Emulation** — Micro-op pipeline architecture provides true cycle-level timing, validated against real hardware traces
- **Four CPU Variants:**
  - `NMOS6502` — Original NMOS 6502 with illegal/undocumented opcodes
  - `NMOS6502_NO_ILLEGAL` — NMOS 6502 with undefined opcodes as NOPs
  - `WDC65C02` — Later WDC 65C02 (W65C02S) with all CMOS instructions including RMB/SMB/BBR/BBS
  - `ROCKWELL65C02` — Rockwell 65C02 (same as WDC but WAI/STP are NOPs)
- **Double-Buffered State** — Clean instruction boundaries for debugging, single-stepping, and state comparison
- **Full Interrupt Support** — IRQ, NMI, and Reset with proper priority handling
- **Stateless Core** — CPU execution engine is stateless; all state lives in `CpuStateBuffer`

### Validation

All CPU variants pass industry-standard test suites:

| Test Suite | Coverage |
|------------|----------|
| Klaus Dormann 6502 Functional Test | ✅ All variants |
| Klaus Dormann Decimal Test | ✅ All variants |
| Klaus Dormann 65C02 Extended Opcodes | ✅ WDC65C02, ROCKWELL65C02 |
| Tom Harte SingleStepTests | ✅ 100% pass rate, all variants |

The Tom Harte tests validate not only final register state but also **cycle-by-cycle bus activity** for every opcode.

- All 2,149 unit tests pass

### Requirements

- .NET 8.0 or later

### Quick Start

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

### Documentation

- [README](https://github.com/markdavidlong/Pandowdy.Cpu/blob/main/README.md)
- [CPU Usage Guide](https://github.com/markdavidlong/Pandowdy.Cpu/blob/main/docs/CpuUsageGuide.md)
- [API Reference](https://github.com/markdavidlong/Pandowdy.Cpu/blob/main/docs/ApiReference.md)

### License

Apache License 2.0

### Author

Copyright 2026 Mark D. Long

---

## v1.0.0

**Release Date:** January 2025

Initial internal release (not published).
