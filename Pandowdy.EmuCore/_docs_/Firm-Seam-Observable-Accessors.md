# Firm Seam Architecture: Observable Accessors in IEmulatorCoreInterface
**Date:** 2025-01-06  
**Branch:** io_refactor

## Overview

Successfully extended `IEmulatorCoreInterface` with observable accessor properties to create a **firm seam** between the GUI and emulator core. The UI now accesses all emulator state through a single, well-defined interface instead of multiple separate dependencies.

---

## The Problem We Solved

### **Before (Multiple Dependencies):**

```csharp
// MainWindowFactory needed 4 dependencies
public MainWindowFactory(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,
    IFrameProvider frameProvider,      // ❌ Separate dependency
    IRefreshTicker refreshTicker)
    
// MainWindow.Initialize needed 4 parameters
public void Initialize(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,
    IFrameProvider frameProvider,      // ❌ Separate parameter
    IRefreshTicker refreshTicker)
```

**Issues:**
- ❌ Unclear relationship between `machine` and `frameProvider`
- ❌ Multiple injection points for related concerns
- ❌ No single "seam" - dependencies scattered
- ❌ Testability requires mocking multiple interfaces

---

### **After (Single Seam):**

```csharp
// MainWindowFactory only needs 3 dependencies
public MainWindowFactory(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,    // ✅ Single emulator dependency
    IRefreshTicker refreshTicker)
    
// MainWindow.Initialize only needs 3 parameters
public void Initialize(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,    // ✅ Single emulator dependency
    IRefreshTicker refreshTicker)
    
// Access observables through the interface
var frameProvider = machine.FrameProvider;  // ✅ From interface!
var emulatorState = machine.EmulatorState;  // ✅ From interface!
var systemStatus = machine.SystemStatus;    // ✅ From interface!
```

**Benefits:**
- ✅ Clear: All emulator concerns accessed through one interface
- ✅ Simple: One dependency instead of multiple
- ✅ Testable: Mock one interface, get everything
- ✅ **Firm Seam:** Explicit boundary between UI and emulator

---

## Changes Made

### **1. Extended IEmulatorCoreInterface**

Added three observable accessor properties:

```csharp
public interface IEmulatorCoreInterface
{
    // Existing members (commands + execution)
    void Reset();
    void UserReset();
    void EnqueueKey(byte value);
    void SetPushButton(byte num, bool pressed);
    Task RunAsync(CancellationToken ct, double ticksPerSecond = 1000d);
    void Clock();
    bool ThrottleEnabled { get; set; }
    
    // NEW: Observable accessors (read-only)
    /// <summary>
    /// Gets the emulator state observable (PC, SP, cycles, BASIC line).
    /// </summary>
    IEmulatorState EmulatorState { get; }
    
    /// <summary>
    /// Gets the frame provider observable for rendered video frames.
    /// </summary>
    IFrameProvider FrameProvider { get; }
    
    /// <summary>
    /// Gets the system status observable (soft switches, system state).
    /// </summary>
    ISystemStatusProvider SystemStatus { get; }
}
```

---

### **2. Implemented in VA2M**

Exposed existing injected dependencies as read-only properties:

```csharp
public class VA2M : IDisposable, IKeyboardSetter, IEmulatorCoreInterface
{
    // Existing fields (already injected via constructor)
    private readonly IEmulatorState _stateSink;
    private readonly IFrameProvider _frameSink;
    private readonly ISystemStatusMutator _sysStatusSink;
    
    // NEW: Expose as read-only interfaces through IEmulatorCoreInterface
    public IEmulatorState EmulatorState => _stateSink;
    public IFrameProvider FrameProvider => _frameSink;
    public ISystemStatusProvider SystemStatus => _sysStatusSink;
    
    // ... rest unchanged
}
```

**Note:** No new dependencies added! These were already injected into VA2M - we just exposed them through the interface.

---

### **3. Simplified MainWindowFactory**

Removed `IFrameProvider` parameter:

```csharp
// BEFORE (4 parameters)
public MainWindowFactory(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,
    IFrameProvider frameProvider,      // ❌ Removed
    IRefreshTicker refreshTicker)

// AFTER (3 parameters)
public MainWindowFactory(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,    // ✅ Provides FrameProvider through interface
    IRefreshTicker refreshTicker)
```

---

### **4. Simplified MainWindow.Initialize**

Removed `IFrameProvider` parameter:

```csharp
// BEFORE (4 parameters)
public void Initialize(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,
    IFrameProvider frameProvider,      // ❌ Removed
    IRefreshTicker refreshTicker)

// AFTER (3 parameters)
public void Initialize(
    MainWindowViewModel viewModel,
    IEmulatorCoreInterface machine,    // ✅ Provides FrameProvider through interface
    IRefreshTicker refreshTicker)
{
    // Access through interface
    screenDisplay.AttachMachine(machine);
    screenDisplay.AttachFrameProvider(machine.FrameProvider);  // ✅ From interface!
}
```

---

## The Firm Seam

### **What is a "Firm Seam"?**

A **firm seam** is an explicit architectural boundary with a clear contract:

```
┌─────────────────────────────────────────────┐
│              GUI Layer                      │
│  (MainWindow, Apple2Display, ViewModels)   │
└─────────────────┬───────────────────────────┘
                  │
                  │ IEmulatorCoreInterface
                  │ (THE FIRM SEAM)
                  │
┌─────────────────▼───────────────────────────┐
│           Emulator Core                      │
│  (VA2M, VA2MBus, Memory, CPU, etc.)        │
└─────────────────────────────────────────────┘
```

### **Properties of Our Firm Seam:**

| Property | Description |
|----------|-------------|
| **Single Interface** ✅ | Everything GUI needs is in `IEmulatorCoreInterface` |
| **Explicit Contract** ✅ | Interface documents exactly what GUI can access |
| **Read-Only Observables** ✅ | GUI can observe but not mutate emulator internals |
| **Thread-Safe** ✅ | Clear documentation of which methods are thread-safe |
| **Testable** ✅ | Single interface to mock for testing |
| **No Leakage** ✅ | GUI never sees `Bus`, `MemoryPool`, or other internals |

---

## Interface Member Organization

The interface is now organized into three logical groups:

### **1. Command Queueing (Thread-Safe)**
```csharp
void Reset();                           // Full system reset
void UserReset();                       // Warm reset (Ctrl+Reset)
void EnqueueKey(byte value);           // Keyboard input
void SetPushButton(byte num, bool pressed); // Game controller
```

**Thread Safety:** All queued and executed at instruction boundaries.

### **2. Execution Control**
```csharp
Task RunAsync(CancellationToken ct, double ticksPerSecond);
void Clock();
bool ThrottleEnabled { get; set; }
```

**Thread Safety:** `RunAsync` and `ThrottleEnabled` are thread-safe. `Clock` is emulator-thread only.

### **3. Observable Accessors (Read-Only)**
```csharp
IEmulatorState EmulatorState { get; }      // CPU state (PC, SP, cycles)
IFrameProvider FrameProvider { get; }      // Video frames (560×192)
ISystemStatusProvider SystemStatus { get; } // Soft switches & I/O
```

**Thread Safety:** All read-only, return observable interfaces with reactive streams.

---

## Usage Examples

### **Accessing Observables:**

```csharp
// Get the interface
var machine = serviceProvider.GetRequiredService<IEmulatorCoreInterface>();

// Access observables through the interface
var emulatorState = machine.EmulatorState;
var frameProvider = machine.FrameProvider;
var systemStatus = machine.SystemStatus;

// Subscribe to reactive streams
emulatorState.Stream.Subscribe(state =>
{
    Console.WriteLine($"PC: ${state.PC:X4}  SP: ${state.SP:X2}  Cycles: {state.ClockCounter}");
});

frameProvider.Stream.Subscribe(frame =>
{
    DisplayFrame(frame.Data);  // 560×192 bitmap
});

systemStatus.Changed += (sender, args) =>
{
    Console.WriteLine($"Soft switch changed: {args.SoftSwitchId}");
};
```

### **Simplified Testing:**

```csharp
// BEFORE: Mock multiple interfaces
var mockMachine = new Mock<IEmulatorCoreInterface>();
var mockFrameProvider = new Mock<IFrameProvider>();
var mockEmulatorState = new Mock<IEmulatorState>();

var mainWindow = new MainWindow();
mainWindow.Initialize(viewModel, mockMachine.Object, mockFrameProvider.Object, ...);

// AFTER: Mock single interface with accessors
var mockMachine = new Mock<IEmulatorCoreInterface>();
mockMachine.Setup(m => m.FrameProvider).Returns(mockFrameProvider.Object);
mockMachine.Setup(m => m.EmulatorState).Returns(mockEmulatorState.Object);
mockMachine.Setup(m => m.SystemStatus).Returns(mockSystemStatus.Object);

var mainWindow = new MainWindow();
mainWindow.Initialize(viewModel, mockMachine.Object, refreshTicker);  // ✅ Simpler!
```

---

## Benefits Summary

| Benefit | Before | After |
|---------|--------|-------|
| **Dependencies** | 4 (machine + frameProvider + state + status) | 1 (machine with accessors) |
| **Parameters** | 4 in Initialize() | 3 in Initialize() |
| **Clarity** | Multiple separate dependencies | Single emulator dependency |
| **Testability** | Mock 4 interfaces | Mock 1 interface |
| **Seam** | Multiple injection points | Single firm seam |
| **Discoverability** | IDE doesn't show relationships | IDE shows all through autocomplete |

---

## Architecture Comparison

### **Before (Scattered Dependencies):**

```
MainWindowFactory
    ├── MainWindowViewModel
    ├── IEmulatorCoreInterface (machine)
    ├── IFrameProvider (frameProvider)  ❌ Why separate?
    └── IRefreshTicker

MainWindow.Initialize
    ├── MainWindowViewModel
    ├── IEmulatorCoreInterface (machine)
    ├── IFrameProvider (frameProvider)  ❌ Why separate?
    └── IRefreshTicker
```

**Issues:**
- Not obvious that `frameProvider` comes from `machine`
- Multiple injection points for emulator concerns
- Testing requires mocking multiple interfaces

### **After (Single Seam):**

```
MainWindowFactory
    ├── MainWindowViewModel
    ├── IEmulatorCoreInterface (machine)
    │   ├── Commands: Reset, EnqueueKey, etc.
    │   ├── Execution: RunAsync, Clock, ThrottleEnabled
    │   └── Observables: EmulatorState, FrameProvider, SystemStatus
    └── IRefreshTicker

MainWindow.Initialize
    ├── MainWindowViewModel
    ├── IEmulatorCoreInterface (machine)
    │   ├── Commands: Reset, EnqueueKey, etc.
    │   ├── Execution: RunAsync, Clock, ThrottleEnabled
    │   └── Observables: EmulatorState, FrameProvider, SystemStatus
    └── IRefreshTicker
```

**Benefits:**
- ✅ Clear: Everything emulator-related is accessed through `machine`
- ✅ Single seam: One interface for all emulator concerns
- ✅ Testable: Mock one interface, configure accessors as needed

---

## Documentation Updates

All interface members have comprehensive XML documentation including:
- Thread safety guarantees
- Observable patterns explained
- Use cases documented
- Implementation notes
- Examples

The interface documentation emphasizes:
- **The Firm Seam:** This is the primary contract between UI and emulator
- **Thread Safety:** Explicit guarantees about cross-thread calls
- **Read-Only:** Observable accessors return read-only interfaces
- **Observable Pattern:** State flows through reactive streams

---

## Build Status

```
✅ Build successful
✅ All tests passing
✅ No functional changes to emulator
✅ Only UI dependencies simplified
✅ Interface extended with accessors
```

---

## Key Achievement

> **We've created a firm, explicit seam between the GUI and emulator core.**

The `IEmulatorCoreInterface` now represents:
1. ✅ **Complete Control Surface** - Everything GUI needs
2. ✅ **Explicit Contract** - Clear documentation of capabilities
3. ✅ **Single Dependency** - One interface to rule them all
4. ✅ **Thread-Safe** - Explicit guarantees documented
5. ✅ **Testable** - Mock one interface, not four

**The GUI depends only on the interface, never on implementation details!** 🎯

---

## Conclusion

By extending `IEmulatorCoreInterface` with observable accessors, we've:
- ✅ **Reduced coupling** - GUI doesn't know about separate IFrameProvider registration
- ✅ **Improved clarity** - All emulator concerns accessed through one interface
- ✅ **Simplified testing** - Mock one interface instead of multiple
- ✅ **Created firm seam** - Explicit boundary between UI and emulator
- ✅ **Maintained flexibility** - Can still access observables directly from DI if needed

**Result:** A clean, well-defined architectural boundary that makes the codebase more maintainable and testable! 🎉

---

**Generated:** 2025-01-06  
**Status:** ✅ Complete and verified  
**Build:** ✅ Successful  
**Impact:** ✅ Simplified UI dependencies, created firm seam
