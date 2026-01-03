# CPU Migration Plan: From Legacy to Stateless Functional Architecture

## Executive Summary

This document outlines the strategy for migrating from the current third-party legacy CPU implementation (`6502.NET/Emulator`) to a native, stateless, functional 6502 CPU implementation. The migration will use an incremental approach with automatic fallback, allowing continuous validation and zero downtime.

**Current Status:** Using `CPUAdapter` wrapper around legacy `Emulator.CPU`  
**Target:** Native stateless functional CPU with externalized state  
**Approach:** Hybrid manager with automatic legacy fallback during migration  
**Validation:** Klaus Dormann's 6502 functional test suite

---

## Table of Contents

1. [Architectural Vision](#architectural-vision)
2. [Current Limitations](#current-limitations)
3. [Target Architecture](#target-architecture)
4. [Migration Strategy](#migration-strategy)
5. [Implementation Plan](#implementation-plan)
6. [Testing Strategy](#testing-strategy)
7. [Progress Tracking](#progress-tracking)
8. [Success Criteria](#success-criteria)

---

## Architectural Vision

### Philosophical Approach

The new CPU will treat instruction execution as a **pure mathematical function**:

```
f(Bus, CpuState) → CpuState
```

This functional approach enables:
- **Time-travel debugging** - Save/restore state at any cycle
- **Speculative execution** - Test different code paths without side effects
- **Parallel analysis** - Run multiple scenarios simultaneously
- **Perfect serialization** - State is just data
- **Architecture swapping** - Same state, different CPU implementations
- **Zero side effects** - CPU can't corrupt anything

### Method Injection Philosophy

Both **bus** and **state** will be passed as parameters (not stored in CPU):

```csharp
// Pure function style - no internal state
public CpuState Clock(IMemoryBus bus, CpuState state)
{
    // CPU is stateless - just transforms inputs to outputs
    byte opcode = bus.Read(state.PC);
    
    return opcode switch
    {
        0xA9 => state with  // Immutable update
        {
            A = bus.Read((ushort)(state.PC + 1)),
            PC = (ushort)(state.PC + 2),
            Cycles = state.Cycles + 2
        },
        // ... other instructions
    };
}
```

**Benefits:**
- Maximum flexibility for debugging (swap buses per call)
- No tight coupling between CPU and bus
- CPU can work with any bus implementation
- State can be managed externally (snapshots, replays, etc.)

---

## Current Limitations

### Legacy CPU Architecture Problems

1. **Tight Coupling:**
   - Legacy CPU uses mutual registration (CPU ↔ Bus)
   - Requires `Connect(bus)` / `Connect(null)` ceremony
   - Connection overhead on every operation

2. **Embedded State:**
   - Registers (PC, A, X, Y, SP, Status) stored inside CPU
   - Difficult to snapshot/restore
   - Impossible to run multiple states with one CPU instance

3. **External Dependency:**
   - Depends on third-party `6502.NET` project
   - Limited control over implementation
   - Can't optimize for Pandowdy's specific needs

4. **Performance Overhead:**
   - Connection/disconnection on every `Clock()` call
   - Most frequently called method in emulator
   - Measurable but not critical overhead

### CPUAdapter Workaround

Current `CPUAdapter` class bridges these issues by:
- Wrapping legacy CPU with Pandowdy's `ICpu` interface
- Handling connection/disconnection pattern automatically
- Passing bus as parameter (hiding legacy coupling)

**But it's still limited by legacy CPU's internal architecture.**

---

## Target Architecture

### Core Components

#### 1. CpuState Struct (Externalized State)

```csharp
/// <summary>
/// Immutable CPU state - all registers and flags externalized.
/// </summary>
public struct CpuState
{
    // Registers
    public ushort PC;          // Program Counter
    public byte A;             // Accumulator
    public byte X;             // X Index
    public byte Y;             // Y Index
    public byte SP;            // Stack Pointer
    
    // Status flags
    public ProcessorStatus Status;
    
    // Cycle tracking
    public ulong Cycles;       // Total cycles executed
    
    // Execution state (for multi-cycle instructions)
    public byte CurrentOpcode;
    public byte CycleWithinInstruction;
    public ushort InternalAddress;  // Temp for addressing modes
}
```

**Key Properties:**
- **Immutable** - Use `with` syntax for updates
- **Serializable** - JSON, binary, etc.
- **Copyable** - Trivial snapshots
- **Value type** - Fast, stack-allocated

#### 2. IStatelessCpu Interface

```csharp
/// <summary>
/// Pure functional 6502 CPU - all state externalized.
/// </summary>
public interface IStatelessCpu
{
    /// <summary>
    /// Execute one clock cycle, returning new state.
    /// Throws NotImplementedException for unimplemented instructions.
    /// </summary>
    CpuState Clock(IMemoryBus bus, CpuState state);
    
    CpuState Reset(IMemoryBus bus, CpuState state);
    CpuState InterruptRequest(IMemoryBus bus, CpuState state);
    CpuState NonMaskableInterrupt(IMemoryBus bus, CpuState state);
}
```

#### 3. Cpu6502Functional Implementation

```csharp
/// <summary>
/// Native functional 6502 implementation.
/// Start with all instructions throwing NotImplementedException.
/// </summary>
public class Cpu6502Functional : IStatelessCpu
{
    public CpuState Clock(IMemoryBus bus, CpuState state)
    {
        byte opcode = bus.Read(state.PC);
        
        return opcode switch
        {
            // Implemented instructions
            0xA9 => ExecuteLDA_Immediate(bus, state),
            0xA5 => ExecuteLDA_ZeroPage(bus, state),
            
            // Not implemented - triggers fallback
            _ => throw new NotImplementedException(
                $"Opcode ${opcode:X2} not yet implemented")
        };
    }
    
    private CpuState ExecuteLDA_Immediate(IMemoryBus bus, CpuState state)
    {
        var operand = bus.Read((ushort)(state.PC + 1));
        
        return state with
        {
            A = operand,
            PC = (ushort)(state.PC + 2),
            Cycles = state.Cycles + 2,
            Status = UpdateFlagsNZ(state.Status, operand)
        };
    }
}
```

---

## Migration Strategy

### Hybrid CPU Manager Pattern

Use a **Facade + Fallback Pattern** that automatically handles migration:

```csharp
/// <summary>
/// Manages transition from legacy to new CPU.
/// Automatically falls back to legacy for unimplemented instructions.
/// </summary>
public class HybridCpuManager : ICpu
{
    private readonly ICpu _legacyCpu;              // Old stateful CPU
    private readonly IStatelessCpu _newCpu;        // New functional CPU
    private CpuState _externalState;               // Externalized state
    
    public void Clock(IAppleIIBus bus)
    {
        try
        {
            // Try new CPU first
            _externalState = _newCpu.Clock(bus, _externalState);
            _newInstructionsUsed++;
            
            // Keep legacy CPU in sync for potential fallback
            SyncLegacyCpuFromState(_externalState);
        }
        catch (NotImplementedException)
        {
            // Fallback to legacy CPU
            _legacyFallbacksUsed++;
            
            // Execute on legacy
            SyncLegacyCpuFromState(_externalState);
            _legacyCpu.Clock(bus);
            _externalState = SyncStateFromLegacyCpu();
        }
    }
}
```

### Key Benefits

1. **Automatic Fallback:**
   - Try new CPU → Success? Use result
   - Try new CPU → NotImplementedException? Use legacy

2. **Always Working:**
   - Emulator always functions (legacy fallback)
   - Can run Apple II software throughout migration

3. **Incremental Progress:**
   - Implement one instruction at a time
   - Run tests after each instruction
   - Catch bugs immediately

4. **Clear Metrics:**
   - Track % of instructions using new CPU
   - Identify most-used unimplemented instructions
   - Know when migration is complete (0% legacy)

5. **Safe Removal:**
   - When legacy fallback never used → remove it
   - Clear signal that migration is complete

---

## Implementation Plan

### Phase 1: Infrastructure (Weeks 1-2)

**Goal:** Set up hybrid manager and state management

#### Tasks:
1. Create `CpuState` struct
   - All 6502 registers
   - Cycle counter
   - Instruction execution state

2. Define `IStatelessCpu` interface
   - Pure functional signatures
   - NotImplementedException for unimplemented

3. Implement `HybridCpuManager`
   - Legacy + New CPU instances
   - Try-catch fallback pattern
   - State synchronization methods

4. Create `Cpu6502Functional` stub
   - All 256 opcodes throw NotImplementedException
   - Placeholder for instruction implementations

#### Deliverables:
- ✅ Infrastructure compiles
- ✅ Hybrid manager works with 100% legacy fallback
- ✅ State sync verified correct

---

### Phase 2: Load/Store Instructions (Weeks 3-4)

**Goal:** Implement foundational data movement instructions

#### Instructions to Implement:

**LDA (Load Accumulator):**
- `0xA9` - LDA #immediate
- `0xA5` - LDA zeropage
- `0xAD` - LDA absolute
- `0xB5` - LDA zeropage,X
- `0xBD` - LDA absolute,X
- `0xB9` - LDA absolute,Y
- `0xA1` - LDA (indirect,X)
- `0xB1` - LDA (indirect),Y

**Similar for LDX, LDY, STA, STX, STY**

#### Validation:
- Unit test each instruction against legacy CPU
- Run Klaus test after each implementation
- Verify no regressions

---

### Phase 3: Control Flow (Weeks 5-6)

**Goal:** Implement branches, jumps, and stack operations

#### Instructions:
- Branch: BEQ, BNE, BCS, BCC, BMI, BPL, BVS, BVC
- Jump: JMP absolute, JMP indirect, JSR, RTS, RTI
- Stack: PHA, PLA, PHP, PLP
- System: NOP, BRK

---

### Phase 4: ALU Operations (Weeks 7-9)

**Goal:** Implement arithmetic and logic instructions

#### Instructions:
- Arithmetic: ADC, SBC (all addressing modes)
- Logic: AND, ORA, EOR (all addressing modes)
- Shifts: ASL, LSR, ROL, ROR (accumulator + memory)
- Compare: CMP, CPX, CPY (all addressing modes)
- Increment/Decrement: INC, DEC, INX, DEX, INY, DEY
- Bit test: BIT

---

### Phase 5: Transfer & Flag Operations (Week 10)

**Goal:** Complete remaining simple instructions

#### Instructions:
- Transfers: TAX, TXA, TAY, TYA, TSX, TXS
- Flag control: CLC, SEC, CLD, SED, CLI, SEI, CLV

---

### Phase 6: Cleanup & Optimization (Weeks 11-12)

**Goal:** Remove legacy CPU, optimize performance

#### Tasks:
1. Verify 0% legacy fallback on Klaus test
2. Remove `_legacyCpu` field from HybridCpuManager
3. Optimize hot paths (LDA, STA, branches)
4. Profile performance vs legacy
5. Update documentation

---

## Testing Strategy

### 1. Unit Tests (Per Instruction)

Test each new instruction against legacy CPU:

```csharp
[Test]
public void LDA_Immediate_MatchesLegacy()
{
    var initialState = new CpuState { PC = 0x1000 };
    var bus = CreateTestBus(0x1000, new byte[] { 0xA9, 0x42 });
    
    // Execute with new CPU
    var newState = _newCpu.Clock(bus, initialState);
    
    // Execute with legacy CPU
    SyncLegacyCpu(initialState);
    _legacyCpu.Clock(bus);
    var legacyState = SyncFromLegacy();
    
    // States must match exactly
    Assert.AreEqual(legacyState, newState);
}
```

### 2. Klaus Dormann Functional Test

**Gold standard validation** - comprehensive 6502 test ROM:

```csharp
[Test]
public void Klaus_FunctionalTest_NewCpu_PassesAllTests()
{
    // Load test ROM
    var testRom = File.ReadAllBytes("TestRoms/6502_functional_test.bin");
    var bus = new TestRomBus(testRom, loadAddress: 0x0000);
    
    // Run hybrid manager
    var hybrid = new HybridCpuManager(legacyCpu, newCpu);
    RunTestRom(hybrid, bus, maxCycles: 100_000_000);
    
    // Verify test passed
    Assert.IsTrue(bus.DidTestPass(hybrid.PC, successAddress: 0x3469));
    
    // Print migration progress
    hybrid.ReportProgress();  // Shows % new vs legacy
}
```

**Klaus Test Properties:**
- Source: https://github.com/Klaus2m5/6502_65C02_functional_tests
- Tests: All 151 documented 6502 instructions
- Coverage: All addressing modes, edge cases, flags
- Success: Infinite loop at $3469
- Failure: Traps at failing test address

### 3. Continuous Validation

Run Klaus test after **every instruction implementation**:

```csharp
[Test]
[Order(1)]
public void After_LDA_Implementation_KlausTestStillPasses()
{
    RunKlausTestAndVerify();
}

[Test]
[Order(2)]
public void After_STA_Implementation_KlausTestStillPasses()
{
    RunKlausTestAndVerify();
}
```

**Benefits:**
- Catch regressions immediately
- Don't pile bugs on top of bugs
- Always have working emulator

### 4. Test ROM Harness

```csharp
/// <summary>
/// Minimal bus for running 6502 test ROMs.
/// </summary>
public class TestRomBus : IAppleIIBus
{
    private readonly byte[] _ram = new byte[0x10000];
    private ushort _lastPC = 0;
    private int _pcRepeatCount = 0;
    
    public TestRomBus(byte[] testRom, ushort loadAddress)
    {
        Array.Copy(testRom, 0, _ram, loadAddress, testRom.Length);
    }
    
    public byte Read(ushort address) => _ram[address];
    public void Write(ushort address, byte value) => _ram[address] = value;
    
    // Detect test completion (PC stuck)
    public bool IsTestComplete(ushort currentPC)
    {
        if (currentPC == _lastPC)
        {
            _pcRepeatCount++;
            return _pcRepeatCount > 10;  // Infinite loop detected
        }
        _lastPC = currentPC;
        _pcRepeatCount = 0;
        return false;
    }
}
```

---

## Progress Tracking

### Metrics to Track

1. **Instruction Coverage:**
   ```
   Implemented: 85/256 opcodes (33.2%)
   Remaining: 171 opcodes
   ```

2. **Usage-Based Coverage:**
   ```
   New CPU: 2,847,234 instructions (78.4%)
   Legacy fallback: 784,901 instructions (21.6%)
   ```

3. **Most-Used Unimplemented:**
   ```
   Top 5 instructions to implement next:
     $6D ADC absolute  : 125,432 times
     $4C JMP absolute  : 98,234 times
     $20 JSR absolute  : 87,123 times
     $ED SBC absolute  : 76,543 times
     $10 BPL relative  : 65,432 times
   ```

4. **Klaus Test Progress:**
   ```
   Phase 1 (Load/Store): 25% new CPU → Klaus PASS ✅
   Phase 2 (Control):    60% new CPU → Klaus PASS ✅
   Phase 3 (ALU):        85% new CPU → Klaus PASS ✅
   Phase 4 (Misc):       100% new CPU → Klaus PASS ✅
   ```

### Progress Reporting

```csharp
public class InstrumentedHybridCpuManager
{
    private Dictionary<byte, int> _newCpuOpcodeCount = new();
    private Dictionary<byte, int> _legacyFallbackOpcodeCount = new();
    
    public void PrintDetailedReport()
    {
        Console.WriteLine("\n=== Migration Progress ===");
        Console.WriteLine($"Implemented opcodes: {_newCpuOpcodeCount.Count}/256");
        
        var total = _newCpuOpcodeCount.Values.Sum() + 
                    _legacyFallbackOpcodeCount.Values.Sum();
        var newPercent = _newCpuOpcodeCount.Values.Sum() * 100.0 / total;
        
        Console.WriteLine($"New CPU: {newPercent:F1}%");
        Console.WriteLine($"Legacy fallback: {100-newPercent:F1}%");
        
        // Show top unimplemented instructions
        foreach (var (opcode, count) in _legacyFallbackOpcodeCount
            .OrderByDescending(kv => kv.Value)
            .Take(10))
        {
            Console.WriteLine($"  ${opcode:X2} {GetName(opcode),-20} : {count,8:N0}");
        }
    }
}
```

---

## Success Criteria

### Completion Checklist

#### Phase Completion:
- ✅ All 256 opcodes either implemented or confirmed illegal
- ✅ Klaus functional test passes with 100% new CPU (0% legacy)
- ✅ All unit tests pass
- ✅ Performance equal or better than legacy CPU
- ✅ No `NotImplementedException` thrown during normal operation

#### Quality Gates:
- ✅ Legacy CPU field removed from HybridCpuManager
- ✅ All tests pass without legacy CPU
- ✅ Documentation updated
- ✅ Example programs run correctly (Apple II software)

#### Final Validation:
- ✅ Run Klaus test 10 times → 10/10 passes
- ✅ Run Apple II boot sequence → Success
- ✅ Run test programs (games, demos) → Work correctly
- ✅ Performance benchmark vs legacy → Equal or better
- ✅ Code review → Approved

---

## Future Enhancements

### After Migration Complete

1. **Advanced Debugging Features:**
   - Time-travel debugging (rewind/replay)
   - State snapshots at any cycle
   - Conditional breakpoints on state changes
   - Execution trace recording

2. **Performance Optimizations:**
   - JIT compilation for hot paths
   - Instruction caching
   - Branch prediction hints

3. **Architecture Variations:**
   - 65C02 support (additional opcodes)
   - 65816 support (16-bit mode)
   - Rockwell/WDC extensions

4. **Analysis Tools:**
   - Instruction frequency profiling
   - Memory access heatmaps
   - Cycle-accurate timing analysis
   - Coverage reporting

---

## Risk Mitigation

### Potential Issues & Solutions

**Risk:** Migration takes too long  
**Mitigation:** Hybrid manager ensures emulator always works; can pause migration indefinitely

**Risk:** New CPU has bugs not caught by tests  
**Mitigation:** Klaus test + continuous validation catches bugs immediately

**Risk:** Performance worse than legacy  
**Mitigation:** Profile and optimize; pure functional code often faster than stateful

**Risk:** State synchronization bugs  
**Mitigation:** Verification tests compare legacy vs new state after every operation

**Risk:** Loss of momentum  
**Mitigation:** Small incremental steps; visible progress; automated tests provide confidence

---

## References

### External Resources

- **Klaus Dormann's 6502 Test:**  
  https://github.com/Klaus2m5/6502_65C02_functional_tests

- **6502 Reference:**  
  http://www.6502.org/tutorials/6502opcodes.html

- **Cycle-Accurate Timing:**  
  http://nesdev.com/6502_cpu.txt

- **Visual 6502:**  
  http://www.visual6502.org/

### Internal Documentation

- `CPUAdapter.cs` - Current legacy CPU wrapper
- `ICpu.cs` - CPU interface
- `IAppleIIBus.cs` - Bus interface
- Test suite in `Pandowdy.EmuCore.Tests/`

---

## Document History

- **Created:** 2024 (Copilot assisted planning session)
- **Status:** Planning phase
- **Next Review:** After Phase 1 infrastructure completion

---

## Appendix: Code Examples

### Example 1: Complete LDA Immediate Implementation

```csharp
private CpuState ExecuteLDA_Immediate(IMemoryBus bus, CpuState state)
{
    // Fetch operand (byte after opcode)
    var operand = bus.Read((ushort)(state.PC + 1));
    
    // Update flags
    var newStatus = state.Status with
    {
        Zero = (operand == 0),
        Negative = (operand & 0x80) != 0
    };
    
    // Return new state (immutable)
    return state with
    {
        A = operand,                    // Load into accumulator
        PC = (ushort)(state.PC + 2),    // Advance PC (opcode + operand)
        Cycles = state.Cycles + 2,      // LDA immediate = 2 cycles
        Status = newStatus
    };
}
```

### Example 2: Multi-Cycle Instruction (JSR)

```csharp
private CpuState ExecuteJSR_Absolute(IMemoryBus bus, CpuState state)
{
    // JSR takes 6 cycles - need to track progress
    return state.CycleWithinInstruction switch
    {
        0 => state with { CycleWithinInstruction = 1 },  // Fetch opcode
        1 => state with  // Fetch low byte of address
        {
            InternalAddress = bus.Read((ushort)(state.PC + 1)),
            CycleWithinInstruction = 2
        },
        2 => state with { CycleWithinInstruction = 3 },  // Internal operation
        3 => state with  // Push PCH to stack
        {
            // ... stack operations ...
            CycleWithinInstruction = 4
        },
        4 => state with  // Push PCL to stack
        {
            // ... stack operations ...
            CycleWithinInstruction = 5
        },
        5 =>  // Fetch high byte and jump
        {
            var high = bus.Read((ushort)(state.PC + 2));
            var targetAddress = (ushort)(state.InternalAddress | (high << 8));
            
            return state with
            {
                PC = targetAddress,
                Cycles = state.Cycles + 6,
                CycleWithinInstruction = 0  // Reset for next instruction
            };
        },
        _ => throw new InvalidOperationException("Invalid cycle state")
    };
}
```

### Example 3: State Snapshot Usage

```csharp
// Save state before executing suspect code
var checkpoint = currentState;

// Execute questionable code
for (int i = 0; i < 1000; i++)
{
    currentState = cpu.Clock(bus, currentState);
}

// Bug detected? Restore and debug
if (DetectedBug(currentState))
{
    // Restore to checkpoint
    currentState = checkpoint;
    
    // Re-execute with logging
    for (int i = 0; i < 1000; i++)
    {
        Console.WriteLine($"Cycle {i}: PC=${currentState.PC:X4} A=${currentState.A:X2}");
        currentState = cpu.Clock(bus, currentState);
        
        if (DetectedBug(currentState))
        {
            Console.WriteLine($"Bug occurs at cycle {i}!");
            break;
        }
    }
}
```

---

**End of Document**
