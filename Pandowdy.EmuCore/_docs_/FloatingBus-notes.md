#FloatingBusReader – Planning Notes#

##Goal:##
Simulate realistic Apple II floating bus behavior without implementing a full pixel-accurate video pipeline, by modeling bus mastering, video scan position, and display mode–dependent memory fetch patterns.

##Core Responsibilities:##

- Determine the correct value to return for any read from a "floating bus" address.

- Model which device (CPU or video hardware) is effectively "driving" the data bus at a given cycle.

- Reconstruct what byte would be present on the bus at that cycle, based on:

  - Current video mode (TEXT/GR/HGR)

  - Current scanline

  - Current horizontal position within the scanline

  - HBLANK/VBLANK vs active display

##Key Concepts:##

1. Cycle Counter

  - Maintain a master cycle counter tied to the 6502 clock.

  - From this counter, derive:

    - Current video frame position

    - Current scanline index

    - Current horizontal position (character/byte or pixel group)

    - Whether the current position is:

      - Active display region

      - Horizontal blanking (HBLANK)

      - Vertical blanking (VBLANK)

2. LastBusValue Register

  - Track the last value placed on the data bus by:

    - Any CPU read

    - Any CPU write

  - Optionally, also update this when simulating video DMA fetches (only needed if I want LastBusValue to be influenced by video outside of floating-bus reads).

  - During HBLANK and VBLANK, this is the value returned for floating-bus reads, because the video hardware is not actively fetching display bytes and the CPU is effectively the last bus master.

3. Floating Bus Read Decision

  - When the CPU reads from a floating-bus address:

    - Use the cycle counter to determine if the beam is:

      - In active display

      - In HBLANK

      - In VBLANK

  - If in HBLANK or VBLANK:

    - Return LastBusValue (last CPU-driven bus value).

  - If in active display:

    - Compute which video memory byte would be fetched at this position, based on:

      - Current mode (TEXT, GR, HGR)

      - Current scanline

      - Horizontal offset within the line

      - Apple II memory layout rules for that mode

    - Return that computed byte as the floating-bus value.

4. Mode-Specific Behavior

  - TEXT Mode:

    - Video fetches bytes from the text page (screen codes).

    - For a given scanline and character position:

      - Compute the text memory address of the current character cell.

      - Floating bus returns the raw text page byte (screen code), not glyph ROM output.

  - GR (Low-Res Graphics) Mode:

    - Each byte contains two 4-bit color nibbles (two blocks per byte).

    - For a given scanline and block pair:

      - Compute the GR memory address for the corresponding byte.

      - Floating bus returns the raw GR byte for that position.

  - HGR (Hi-Res Graphics) Mode:

    - Each byte represents 7 pixels plus 1 color/phase bit.

    - Memory layout is interleaved and depends on scanline and column.

    - For a given scanline and pixel group:

      - Compute the HGR memory address using the standard Apple II HGR interleave formula.

      - Floating bus returns the raw HGR byte corresponding to that 7-pixel group.

5. No Need to Store Per-Cycle Bus History

  - Do not store bus values per cycle.

  - Instead, reconstruct the "expected" video fetch byte on demand using:

    - The cycle counter

    - Mode

    - Derived scanline and horizontal position

    - Known memory layout formulas

  - This keeps the implementation efficient but still cycle-accurate for floating-bus reads.

6. Interface Design (Conceptual)

  - Inputs:

    - Current cycle count

    - Current video mode flags (TEXT, GR, HGR, MIXED, etc.)

    - LastBusValue

    - Memory read callback (to fetch the appropriate byte from video RAM when needed)

  - Output:

    - Byte value representing what the floating bus should return at this cycle.

  - Example call:

    - value = FloatingBusReader.readFloatingBus(cycleCount, videoState, lastBusValue, memory)

7. Update Responsibilities Outside FloatingBusReader

  - CPU core:

    - Must update LastBusValue on every read/write.

    - Must advance the master cycle counter in lockstep with instruction execution.

  - Video system:

    - Must maintain video mode state (TEXT/GR/HGR/etc.).

    - Must define or share the mapping from: (scanline, horizontal position, mode) -> video memory address.

  - FloatingBusReader:

    - Purely responsible for using these inputs to decide what the floating bus "would" show at a given moment.

##Behavior Summary:##

  - During active display:

    - Floating bus returns the byte that the video hardware would be reading from RAM for the current character/block/pixel group, determined by mode and beam position.

  - During HBLANK or VBLANK:

    - Floating bus returns LastBusValue, representing the last CPU-driven value on the bus.

  - No actual video DMA or pixel rendering is required; only the memory fetch pattern is simulated, driven by a cycle-accurate timing model.

This design provides:

  - Realistic, cycle-accurate Apple II floating-bus behavior.

  - Correct interaction with software that syncs to the video beam or relies on floating-bus quirks.

  - A clean separation between CPU timing, video mode/state, and floating-bus emulation.

---

##Implementation Recommendations and Refinements##

###Overall Assessment###

The design is **production-ready** and strikes an excellent balance between accuracy and practicality. The on-demand reconstruction approach is optimal—avoiding per-cycle storage while maintaining cycle-accurate behavior.

**Strengths:**
- Clean separation of concerns (CPU, video, floating bus logic)
- Memory-efficient (zero per-cycle storage overhead)
- Cycle-accurate via deterministic reconstruction
- Testable components with clear interfaces

###Enhancement Suggestions###

####1. Extract Video Address Calculation into Pluggable Interface####

Consider separating video address calculation from FloatingBusReader:

```csharp
public interface IVideoAddressMapper
{
    ushort GetVideoAddress(int scanline, int horizontalPosition, VideoMode mode);
}
```

**Benefits:**
- Video address logic can be tested independently
- Easier to support different Apple II models (II+, IIe, IIgs)
- FloatingBusReader becomes simpler (just timing + delegation)
- Can swap implementations for testing or accuracy tuning

**Implementation:**
```csharp
public class Apple2VideoAddressMapper : IVideoAddressMapper
{
    public ushort GetVideoAddress(int scanline, int hpos, VideoMode mode)
    {
        return mode switch
        {
            VideoMode.TEXT => GetTextAddress(scanline, hpos),
            VideoMode.GR => GetLowResAddress(scanline, hpos),
            VideoMode.HGR => GetHiResAddress(scanline, hpos),
            _ => 0x0400 // Safe fallback
        };
    }
    
    private ushort GetTextAddress(int scanline, int column) { /* ... */ }
    private ushort GetLowResAddress(int scanline, int column) { /* ... */ }
    private ushort GetHiResAddress(int scanline, int byteColumn) { /* ... */ }
}
```

####2. Define VideoState Structure Explicitly####

The conceptual interface mentions `videoState` but doesn't define it. Recommend:

```csharp
public struct VideoState
{
    public VideoMode Mode;        // TEXT, GR, HGR, DHGR
    public bool MixedMode;         // Bottom 4 lines are text
    public bool Page2;             // Which text/hi-res page
    public bool Show80Col;         // 80-column text mode
    public bool DoubleHiRes;       // DHGR mode
    
    // Computed property for convenience
    public bool IsInMixedTextRegion(int scanline) => MixedMode && scanline >= 160;
}
```

This makes the interface contract explicit and documents what soft switches affect floating bus behavior.

####3. Handle Mixed Mode Explicitly####

Mixed mode changes video fetch pattern mid-frame:

```
If MIXED mode is enabled:
  - Scanlines 0-159: Use GR or HGR fetch pattern
  - Scanlines 160-191: Use TEXT fetch pattern (bottom 4 lines of text)
```

**Implementation Note:** FloatingBusReader needs to check scanline position and switch fetch logic at the boundary.

####4. Plan for 80-Column Mode####

80-column mode has special floating bus behavior (as documented in IFloatingBusProvider):

```
In 80-column mode without aux memory:
  - Even columns: Read from main memory
  - Odd columns: Floating bus returns previous (main memory) value
  - Result: Characters appear doubled ("ghost characters")
```

Your architecture naturally supports this, but explicitly call it out:

```csharp
private ushort Get80ColumnAddress(int scanline, int column, bool hasAuxMemory)
{
    bool isOddColumn = (column % 2) == 1;
    
    if (isOddColumn && !hasAuxMemory)
    {
        // Floating bus will return previous value (handled by caller)
        return FLOATING_BUS_MARKER; // Special sentinel value
    }
    
    // Even columns or aux memory present
    ushort baseAddr = GetTextAddress(scanline, column / 2);
    return isOddColumn ? (ushort)(baseAddr | 0x10000) : baseAddr; // Flag aux memory
}
```

####5. Add Double Hi-Res (DHGR) Support####

For full Apple IIe compatibility:

```
DHGR Mode:
  - Alternates fetches between main and aux memory
  - Even byte columns: main memory
  - Odd byte columns: aux memory
  - Floating bus reflects whichever bank is being scanned
```

Don't implement now, but ensure architecture accommodates it.

###Potential Challenges and Solutions###

####Challenge 1: HGR Interleave Formula Complexity####

The Apple II hi-res memory layout is complex:

```csharp
private ushort GetHiResAddress(int scanline, int byteColumn, bool page2)
{
    int baseAddress = page2 ? 0x4000 : 0x2000;
    int block = scanline / 64;           // 0, 1, or 2
    int offset = scanline % 64;
    int lineGroup = offset / 8;          // 0-7
    int lineInGroup = offset % 8;        // 0-7
    
    return (ushort)(baseAddress + 
                    (block * 0x28 * 8) +     // Block offset (320 bytes * 8 lines)
                    (lineInGroup * 0x400) +   // Scanline interleave (1024 bytes)
                    (lineGroup * 0x28) +      // Line group (40 bytes)
                    byteColumn);              // Horizontal position (0-39)
}
```

**Recommendation:** Test extensively with known HGR patterns. This formula is easy to get wrong.

**Test Cases:**
```csharp
[Theory]
[InlineData(0, 0, false, 0x2000)]     // First byte, first line, page 1
[InlineData(0, 39, false, 0x2027)]    // Last byte, first line, page 1
[InlineData(7, 0, false, 0x2400)]     // First byte, line 7 (interleave test)
[InlineData(63, 0, false, 0x3FD8)]    // First byte, line 63 (complex case)
[InlineData(191, 39, false, 0x3FFF)]  // Last byte, last line, page 1
public void HiResAddress_CalculatesCorrectly(int scanline, int column, bool page2, ushort expected)
{
    var mapper = new Apple2VideoAddressMapper();
    ushort actual = mapper.GetHiResAddress(scanline, column, page2);
    Assert.Equal(expected, actual);
}
```

####Challenge 2: Cycle-to-Beam-Position Mapping####

Precise timing constants:

```
Apple II NTSC Timing:
  CYCLES_PER_FRAME = 17,030 cycles
  CYCLES_PER_SCANLINE = 65 cycles
  VISIBLE_SCANLINES = 192
  VBLANK_SCANLINES = 70
  TOTAL_SCANLINES = 262

Blanking Periods:
  HBLANK: Cycles 40-64 within each scanline (~25 cycles)
  VBLANK: Scanlines 192-261 (70 scanlines)
```

**Implementation:**
```csharp
private (int scanline, int hpos) GetBeamPosition(ulong cycle)
{
    int cycleInFrame = (int)(cycle % CYCLES_PER_FRAME);
    int scanline = cycleInFrame / CYCLES_PER_SCANLINE;
    int hpos = cycleInFrame % CYCLES_PER_SCANLINE;
    return (scanline, hpos);
}

private bool IsInBlanking(ulong cycle)
{
    var (scanline, hpos) = GetBeamPosition(cycle);
    
    // VBlank check
    if (scanline >= 192) return true;
    
    // HBlank check
    if (hpos >= 40) return true;
    
    return false;
}
```

####Challenge 3: Text Mode Flashing####

In TEXT mode, some characters flash at 16 Hz. For floating bus purposes, return the **raw character code** (no flash logic needed). The video renderer handles flashing separately.

**Document this clearly:** FloatingBusReader returns raw memory values, not rendered output.

###Recommended Class Structure###

####Core Implementation####

```csharp
public interface IFloatingBusProvider
{
    byte Read();
    void NotifyBusValue(byte value); // Called by CPU on reads/writes
}

public class CycleAccurateFloatingBus : IFloatingBusProvider
{
    private readonly ICycleCounter _cycleCounter;
    private readonly IVideoStateProvider _videoState;
    private readonly IVideoAddressMapper _videoMapper;
    private readonly IMemory _memory;
    private byte _lastBusValue;
    
    public byte Read()
    {
        ulong cycle = _cycleCounter.CurrentCycle;
        
        if (IsInBlanking(cycle))
            return _lastBusValue;
        
        var state = _videoState.GetCurrentState();
        var (scanline, hpos) = GetBeamPosition(cycle);
        
        ushort videoAddr = _videoMapper.GetVideoAddress(scanline, hpos, state);
        return _memory.Read(videoAddr);
    }
    
    public void NotifyBusValue(byte value)
    {
        _lastBusValue = value;
    }
}
```

####Helper Interfaces####

```csharp
public interface ICycleCounter
{
    ulong CurrentCycle { get; }
}

public interface IVideoStateProvider
{
    VideoState GetCurrentState();
}

public interface IVideoAddressMapper
{
    ushort GetVideoAddress(int scanline, int horizontalPosition, VideoState state);
}
```

####Simple Fallback Implementation####

For initial development/testing:

```csharp
public class LastValueFloatingBus : IFloatingBusProvider
{
    private byte _lastValue;
    
    public byte Read() => _lastValue;
    public void NotifyBusValue(byte value) => _lastValue = value;
}
```

Start with this simple implementation, then upgrade to cycle-accurate once system is stable.

###Testing Strategy###

####Unit Tests: Video Address Calculation####

```csharp
[Theory]
[InlineData(0, 0, 0x0400)]   // Text line 0, column 0
[InlineData(0, 39, 0x0427)]  // Text line 0, column 39
[InlineData(23, 0, 0x07D0)]  // Text line 23, column 0
[InlineData(23, 39, 0x07F7)] // Text line 23, column 39 (last position)
public void TextMode_CalculatesCorrectAddress(int line, int column, ushort expected)
{
    var mapper = new Apple2VideoAddressMapper();
    ushort actual = mapper.GetTextAddress(line, column);
    Assert.Equal(expected, actual);
}

[Theory]
[InlineData(0, 0, 0x0400)]   // GR line 0, column 0
[InlineData(20, 19, 0x06D3)] // Middle of screen
public void LowResMode_CalculatesCorrectAddress(int line, int column, ushort expected)
{
    var mapper = new Apple2VideoAddressMapper();
    ushort actual = mapper.GetLowResAddress(line, column);
    Assert.Equal(expected, actual);
}
```

####Integration Tests: Floating Bus Behavior####

```csharp
[Fact]
public void FloatingBus_DuringVBlank_ReturnsLastCpuValue()
{
    var cycleCounter = new MockCycleCounter();
    var fb = new CycleAccurateFloatingBus(cycleCounter, ...);
    
    fb.NotifyBusValue(0x42);
    cycleCounter.SetCycle(192 * 65); // First VBlank scanline
    
    Assert.Equal(0x42, fb.Read());
}

[Fact]
public void FloatingBus_DuringActiveDisplay_ReturnsVideoMemory()
{
    var memory = new MemoryBlock64k();
    memory[0x0400] = 0xAA; // Text page character
    
    var cycleCounter = new MockCycleCounter();
    cycleCounter.SetCycle(20); // Scanline 0, horizontal position 20
    
    var videoState = new MockVideoStateProvider();
    videoState.Mode = VideoMode.TEXT;
    
    var fb = new CycleAccurateFloatingBus(cycleCounter, videoState, mapper, memory);
    
    byte value = fb.Read();
    Assert.Equal(0xAA, value); // Should read from text page
}

[Fact]
public void FloatingBus_MixedMode_SwitchesAtScanline160()
{
    var memory = new MemoryBlock64k();
    memory[0x2000] = 0xBB; // HGR data
    memory[0x0750] = 0xCC; // Text data (line 20)
    
    var cycleCounter = new MockCycleCounter();
    var videoState = new MockVideoStateProvider();
    videoState.Mode = VideoMode.HGR;
    videoState.MixedMode = true;
    
    // Before scanline 160: HGR
    cycleCounter.SetCycle(159 * 65 + 20);
    var fb = new CycleAccurateFloatingBus(cycleCounter, videoState, mapper, memory);
    Assert.Equal(0xBB, fb.Read());
    
    // After scanline 160: TEXT
    cycleCounter.SetCycle(160 * 65 + 20);
    Assert.Equal(0xCC, fb.Read());
}
```

####Real Software Tests####

Test with actual Apple II programs:
- **Copy protection routines** (many games check floating bus)
- **DOS 3.3 timing loops** (rely on VBlank detection)
- **Graphics demos** that sync to beam position

###Phased Implementation Plan###

####Phase 1: Foundation (Minimal Viable Implementation)####
1. Implement `ICycleCounter` interface and basic counter
2. Implement `LastValueFloatingBus` (simple fallback)
3. Wire CPU to call `NotifyBusValue()` on reads/writes
4. Test basic functionality

####Phase 2: Timing Infrastructure####
1. Implement cycle-to-beam-position calculation
2. Implement HBLANK/VBLANK detection
3. Unit tests for timing calculations
4. Verify against Apple II specs

####Phase 3: Video Address Mapping####
1. Implement `IVideoAddressMapper` interface
2. Implement TEXT mode address calculation
3. Implement GR mode address calculation
4. Implement HGR mode address calculation (with interleave)
5. Comprehensive unit tests for all formulas
6. Add Mixed Mode support

####Phase 4: Integration####
1. Implement `CycleAccurateFloatingBus`
2. Implement `IVideoStateProvider`
3. Wire up to video soft switches
4. Integration tests with memory subsystem

####Phase 5: Validation and Polish####
1. Test with real Apple II software
2. Verify 80-column ghost character behavior
3. Verify copy protection compatibility
4. Performance profiling (ensure no bottlenecks)
5. Documentation and cleanup

###Memory Map Reference###

```
TEXT Page 1: $0400-$07FF (1024 bytes, 24 lines × 40 columns)
TEXT Page 2: $0800-$0BFF (1024 bytes)
GR Page 1:   $0400-$07FF (shares TEXT Page 1)
GR Page 2:   $0800-$0BFF (shares TEXT Page 2)
HGR Page 1:  $2000-$3FFF (8192 bytes, 192 lines × ~43 bytes/line with interleave)
HGR Page 2:  $4000-$5FFF (8192 bytes)
```

###Known Edge Cases to Handle###

1. **Mixed mode scanline 160 transition** - Fetch pattern changes mid-frame
2. **80-column mode without aux memory** - Odd columns return previous value
3. **Page 2 selection** - Different base addresses for TEXT/HGR
4. **DHGR mode** (future) - Alternating main/aux memory fetches
5. **HBlank timing** - Exact cycle when horizontal blanking starts
6. **VBlank timing** - Exact scanline when vertical blanking starts

###Performance Considerations###

The floating bus read may be called frequently (every unmapped I/O access). Optimize:

1. **Cache beam position calculations** - If multiple reads occur at same cycle
2. **Fast path for blanking periods** - Check blanking first (common case)
3. **Inline simple calculations** - GetBeamPosition should be very fast
4. **Pre-compute constants** - Store CYCLES_PER_FRAME as constants

**Estimated Performance:**
- Direct array access (LastValue): ~1-2ns
- Full video fetch calculation: ~20-50ns
- Still negligible compared to CPU emulation overhead (~100-500ns per instruction)

###Conclusion###

This design is **solid and ready for implementation**. The suggested enhancements (video address mapper interface, explicit VideoState, mixed mode handling) are refinements that can be added incrementally.

**Recommendation: Start with Phase 1 (simple LastValueFloatingBus), then progressively add cycle-accurate behavior as needed.**

The architecture is clean, testable, and maintainable. Proceed with confidence!

---

##Singleton Design Decision##

###Rationale###

The FloatingBusProvider is implemented as a **singleton** because:

1. **Single Source of Truth:** There is exactly ONE data bus in the Apple II hardware. Multiple instances would be nonsensical and could cause inconsistent state.

2. **Ubiquitous Access:** Many disparate subsystems need access:
   - CPU (updates LastBusValue on every read/write)
   - Memory subsystem (returns floating bus on unmapped reads)
   - I/O handlers (returns floating bus on unimplemented addresses)
   - Expansion cards (for unmapped soft switch offsets)

3. **Performance Critical:** The floating bus read is in the hot path (millions of calls/second). Static access is faster than DI lookups.

4. **Natural Hardware Mapping:** Singleton mirrors the physical reality - one data bus per machine.

This is **not a code smell** but a deliberate architectural choice for this specific scenario.

###Implementation: Static Access with DI Initialization###

```csharp
namespace Pandowdy.EmuCore;

/// <summary>
/// Provides floating bus data for the entire emulator system.
/// Implemented as a singleton because there is exactly one data bus in the Apple II.
/// </summary>
public sealed class FloatingBusProvider : IFloatingBusProvider
{
    private static FloatingBusProvider? _instance;
    
    private readonly ICycleCounter _cycleCounter;
    private readonly IVideoStateProvider _videoState;
    private readonly IVideoAddressMapper _videoMapper;
    private readonly IMemory _memory;
    private byte _lastBusValue;
    
    /// <summary>
    /// Initializes the singleton FloatingBusProvider with required dependencies.
    /// </summary>
    /// <param name="cycleCounter">Cycle counter for timing calculations.</param>
    /// <param name="videoState">Video state provider for mode information.</param>
    /// <param name="videoMapper">Video address mapper for memory fetch calculations.</param>
    /// <param name="memory">Memory interface for reading video RAM.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if FloatingBusProvider is instantiated more than once.
    /// </exception>
    public FloatingBusProvider(
        ICycleCounter cycleCounter,
        IVideoStateProvider videoState,
        IVideoAddressMapper videoMapper,
        IMemory memory)
    {
        // Guard against multiple instantiation
        if (_instance != null)
        {
            throw new InvalidOperationException(
                "FloatingBusProvider can only be instantiated once. " +
                "Only one data bus exists in the system.");
        }
        
        ArgumentNullException.ThrowIfNull(cycleCounter);
        ArgumentNullException.ThrowIfNull(videoState);
        ArgumentNullException.ThrowIfNull(videoMapper);
        ArgumentNullException.ThrowIfNull(memory);
        
        _cycleCounter = cycleCounter;
        _videoState = videoState;
        _videoMapper = videoMapper;
        _memory = memory;
        
        _instance = this;
    }
    
    /// <summary>
    /// Reads the current floating bus value. 
    /// Static for performance-critical hot path access.
    /// </summary>
    /// <returns>
    /// Byte value on the floating bus at the current cycle.
    /// During blanking: returns last CPU-driven value.
    /// During active display: returns current video fetch byte.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if FloatingBusProvider has not been initialized via DI.
    /// </exception>
    public static byte Read()
    {
        if (_instance == null)
        {
            throw new InvalidOperationException(
                "FloatingBusProvider has not been initialized. " +
                "Ensure it is registered and constructed in the DI container.");
        }
        
        return _instance.ReadInternal();
    }
    
    /// <summary>
    /// Notifies the floating bus of a value on the data bus.
    /// Called by CPU on every read/write operation.
    /// Static for performance-critical hot path access.
    /// </summary>
    /// <param name="value">Byte value that was on the data bus.</param>
    public static void NotifyBusValue(byte value)
    {
        if (_instance != null)
        {
            _instance._lastBusValue = value;
        }
    }
    
    /// <summary>
    /// Internal read implementation with cycle-accurate logic.
    /// </summary>
    private byte ReadInternal()
    {
        ulong cycle = _cycleCounter.CurrentCycle;
        
        // Fast path: check if we're in blanking period
        if (IsInBlanking(cycle))
        {
            return _lastBusValue; // Return last CPU value
        }
        
        // Active display: compute video fetch address
        var state = _videoState.GetCurrentState();
        var (scanline, hpos) = GetBeamPosition(cycle);
        
        // Handle mixed mode (bottom 4 lines are always text)
        if (state.IsInMixedTextRegion(scanline))
        {
            // Override mode to TEXT for bottom 4 lines
            state = state with { Mode = VideoMode.TEXT };
        }
        
        ushort videoAddr = _videoMapper.GetVideoAddress(scanline, hpos, state);
        return _memory.Read(videoAddr);
    }
    
    // Timing helper methods
    private const int CYCLES_PER_FRAME = 17030;
    private const int CYCLES_PER_SCANLINE = 65;
    private const int VISIBLE_SCANLINES = 192;
    private const int HBLANK_START = 40;
    
    private (int scanline, int hpos) GetBeamPosition(ulong cycle)
    {
        int cycleInFrame = (int)(cycle % CYCLES_PER_FRAME);
        int scanline = cycleInFrame / CYCLES_PER_SCANLINE;
        int hpos = cycleInFrame % CYCLES_PER_SCANLINE;
        return (scanline, hpos);
    }
    
    private bool IsInBlanking(ulong cycle)
    {
        var (scanline, hpos) = GetBeamPosition(cycle);
        
        // VBlank check (scanlines 192-261)
        if (scanline >= VISIBLE_SCANLINES) return true;
        
        // HBlank check (cycles 40-64 within scanline)
        if (hpos >= HBLANK_START) return true;
        
        return false;
    }
    
    // IFloatingBusProvider implementation (for interface compatibility)
    byte IFloatingBusProvider.Read() => ReadInternal();
    void IFloatingBusProvider.NotifyBusValue(byte value) => _lastBusValue = value;
}
```

###DI Registration###

```csharp
// Register dependencies first
services.AddSingleton<ICycleCounter, SystemCycleCounter>();
services.AddSingleton<IVideoStateProvider, VideoStateProvider>();
services.AddSingleton<IVideoAddressMapper, Apple2VideoAddressMapper>();
services.AddSingleton<IMemory, MemoryBlock64k>();

// Register FloatingBusProvider as singleton
// Constructor will validate single instantiation
services.AddSingleton<FloatingBusProvider>();

// Also register as IFloatingBusProvider interface for components that need it
services.AddSingleton<IFloatingBusProvider>(sp => 
    sp.GetRequiredService<FloatingBusProvider>());
```

###Usage Patterns###

**Hot Path (CPU - use static for performance):**
```csharp
public class CPUAdapter
{
    public byte Read(ushort address)
    {
        byte value = _memory[address];
        FloatingBusProvider.NotifyBusValue(value); // Fast static call
        return value;
    }
    
    public void Write(ushort address, byte value)
    {
        FloatingBusProvider.NotifyBusValue(value); // Fast static call
        _memory[address] = value;
    }
}
```

**Memory Subsystem (unmapped reads):**
```csharp
public class MemoryMapper
{
    public byte ReadUnmapped(ushort address)
    {
        // Direct static access - no DI needed
        return FloatingBusProvider.Read();
    }
}
```

**I/O Handlers (unimplemented addresses):**
```csharp
public byte ReadIO(ushort address)
{
    if (!_handlers.TryGetValue(address, out var handler))
    {
        return FloatingBusProvider.Read(); // Fast static access
    }
    return handler.Read();
}
```

**Interface-Based (for components that need DI):**
```csharp
public class ExpansionCard
{
    private readonly IFloatingBusProvider _floatingBus;
    
    public ExpansionCard(IFloatingBusProvider floatingBus)
    {
        _floatingBus = floatingBus; // Injected if needed
    }
    
    public byte ReadUnmappedSoftSwitch(byte offset)
    {
        return _floatingBus.Read(); // Interface access
    }
}
```

###Key Design Decisions###

1. **Public Constructor with DI:** Allows proper dependency injection while maintaining singleton semantics via guard check.

2. **No Instance Property:** The static `_instance` is private. Consumers only call `FloatingBusProvider.Read()`, not `FloatingBusProvider.Instance.Read()`.

3. **Static Read() Method:** Direct static access without exposing the instance provides the cleanest API for performance-critical paths.

4. **Constructor Guard:** Validates single instantiation at construction time, providing clear error message if violated.

5. **Business Logic Enforcement:** Since emulator architecture guarantees only one FloatingBusProvider, the guard is a defensive safety check, not a DI anti-pattern.

###Benefits of This Approach###

1. **Performance:** Static method calls are inlined by JIT compiler (no virtual dispatch overhead)
2. **Simplicity:** Clean API - `FloatingBusProvider.Read()` is intuitive and concise
3. **Safety:** Constructor guard prevents accidental multiple instantiation
4. **DI Compatibility:** Dependencies injected properly via constructor
5. **Interface Support:** Still implements IFloatingBusProvider for components that need it
6. **Testability:** Can mock IFloatingBusProvider in unit tests for components that inject it
7. **Clear Errors:** Helpful exceptions if used before initialization or if instantiated twice

###Testing Strategy###

**Unit Tests (with interface injection):**
```csharp
[Fact]
public void Memory_UnmappedRead_ReturnsFloatingBus()
{
    var mockFloatingBus = new Mock<IFloatingBusProvider>();
    mockFloatingBus.Setup(fb => fb.Read()).Returns(0x42);
    
    var memory = new MemoryMapper(mockFloatingBus.Object);
    Assert.Equal(0x42, memory.ReadUnmapped(0xC0FF));
}
```

**Integration Tests (with real singleton):**
```csharp
[Fact]
public void CPU_Read_UpdatesFloatingBus()
{
    // FloatingBusProvider constructed by DI container in test setup
    var cpu = _serviceProvider.GetRequiredService<CPUAdapter>();
    var memory = _serviceProvider.GetRequiredService<IMemory>();
    
    memory[0x1000] = 0x42;
    cpu.Read(0x1000); // Should update floating bus via NotifyBusValue
    
    // Direct static access for verification
    byte floatingBusValue = FloatingBusProvider.Read();
    Assert.Equal(0x42, floatingBusValue);
}
```

**Testing Multiple Instantiation Guard:**
```csharp
[Fact]
public void FloatingBusProvider_Constructor_ThrowsOnSecondInstantiation()
{
    var cycleCounter = new Mock<ICycleCounter>().Object;
    var videoState = new Mock<IVideoStateProvider>().Object;
    var videoMapper = new Mock<IVideoAddressMapper>().Object;
    var memory = new MemoryBlock64k();
    
    // First instantiation succeeds
    var instance1 = new FloatingBusProvider(
        cycleCounter, videoState, videoMapper, memory);
    
    // Second instantiation throws
    var ex = Assert.Throws<InvalidOperationException>(() =>
        new FloatingBusProvider(
            cycleCounter, videoState, videoMapper, memory));
    
    Assert.Contains("can only be instantiated once", ex.Message);
}
```

###Comparison to Alternatives###

| Aspect | This Design | Pure Static | DI with Instance Property |
|--------|-------------|-------------|---------------------------|
| **API Simplicity** | ⭐⭐⭐⭐⭐ `FloatingBusProvider.Read()` | ⭐⭐⭐⭐⭐ `FloatingBus.Read()` | ⭐⭐⭐☆☆ `FloatingBusProvider.Instance.Read()` |
| **Performance** | ⭐⭐⭐⭐⭐ Static call | ⭐⭐⭐⭐⭐ Static call | ⭐⭐⭐⭐☆ Property + method call |
| **DI Integration** | ⭐⭐⭐⭐⭐ Full DI | ⭐☆☆☆☆ None | ⭐⭐⭐⭐⭐ Full DI |
| **Testability** | ⭐⭐⭐⭐⭐ Interface available | ⭐⭐⭐☆☆ Needs Reset() | ⭐⭐⭐⭐⭐ Interface available |
| **Safety** | ⭐⭐⭐⭐⭐ Constructor guard | ⭐⭐⭐☆☆ No protection | ⭐⭐⭐⭐⭐ Constructor guard |
| **Clarity** | ⭐⭐⭐⭐⭐ No instance exposed | ⭐⭐⭐⭐⭐ Simple | ⭐⭐⭐☆☆ Instance + Read() |

###Conclusion###

This refined singleton design provides:
- ✅ **Fastest possible access** (static method, no instance navigation)
- ✅ **Proper DI integration** (dependencies injected via constructor)
- ✅ **Safety guarantees** (constructor validates single instantiation)
- ✅ **Clean API** (direct `FloatingBusProvider.Read()` call)
- ✅ **Interface compatibility** (implements IFloatingBusProvider for injection)
- ✅ **Business logic enforcement** (mirrors hardware reality: one data bus)

The design accurately models the physical Apple II hardware (one data bus) while providing ergonomic, high-performance access for the performance-critical code paths in the emulator.
