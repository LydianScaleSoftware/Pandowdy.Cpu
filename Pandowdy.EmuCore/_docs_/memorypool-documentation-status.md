# MemoryPool.cs Documentation Status

## Completed Documentation

### ✅ File Header
- Performance-optimized design rationale
- Trade-offs between speed and clarity
- Future refactoring plans
- Design benefits and limitations

### ✅ Class-Level XML Documentation
- Apple IIe memory architecture overview
- 128KB layout (Main + Aux + ROM/I/O)
- Detailed memory map with all regions
- Soft switch mapping explanation
- Slice-based performance notes
- Thread safety (ReaderWriterLockSlim)
- Future refactoring goals

### ✅ MemoryAccessEventArgs
- Event arguments for memory notifications
- Address and Value properties documented

### ✅ IMemory Interface Members
- `Size` property - 64KB address space
- `Read()` method - mapped memory reads
- `Write()` method - mapped memory writes  
- Indexer `this[address]` - array-like syntax

### ✅ IMemoryAccessNotifier Interface
- `MemoryWritten` event - write notifications
- `MemoryRead` event - reserved for future use

### ✅ IDirectMemoryPoolReader Interface
- `ReadRawMain()` - bypass mapping, read main bank
- `ReadRawAux()` - bypass mapping, read aux bank
- Use cases for debuggers and video renderers

### ✅ Core Fields
- `_pool` - backing store array (163,072 bytes)
- `Pool` property - exposed for advanced use
- `Ranges` enum - memory address regions with boundaries

### ✅ Soft Switch Fields (Complete)
All 12 soft switch bool fields documented:
- `_ramRd` / `_ramWrt` - Auxiliary memory selection
- `_altZp` - Auxiliary zero page
- `_80Store` - Page 2 selection for text/hi-res
- `_hires` - Hi-res graphics mode
- `_page2` - Page 2 selection
- `_intCxRom` - Internal ROM vs slot ROMs
- `_slotC3Rom` - Slot 3 ROM override
- `_highWrite` / `_highRead` - Language Card RAM access
- `_bank1` - Language Card bank selection
- `_preWrite` - Language Card write enable sequence

---

## Remaining Sections (TODO)

### Memory Slice Fields
40+ `Memory<byte>` readonly fields need documentation:
- **Main memory slices** (`_m1` through `_m9`)
- **Auxiliary memory slices** (`_a1` through `_a9`)
- **ROM/I/O slices** (`_io`, `_int1-7`, `_intext`, `_rom1`, `_rom2`)
- **Slot ROM slices** (`_s1-7`, `_s1ext-7`)

**Documentation Pattern:**
```csharp
/// <summary>
/// Main memory slice for $0000-$01FF (Zero Page + Stack).
/// </summary>
/// <remarks>
/// 512 bytes. Default mapping unless ALTZP is set, in which case _a1 is used.
/// Pool offset: 0x0000
/// </remarks>
private readonly Memory<byte> _m1;
```

### Dictionaries
- `_readRanges` - Map of address ranges to read memory slices
- `_writeRanges` - Map of address ranges to write memory slices

**Documentation:**
```csharp
/// <summary>
/// Maps address ranges to memory slices for read operations.
/// </summary>
/// <remarks>
/// Updated by <see cref="UpdateMemoryMappings"/> when soft switches change.
/// Nullable values indicate unmapped/ROM regions that return 0 on read.
/// Uses ReaderWriterLockSlim for thread-safe access.
/// </remarks>
private readonly Dictionary<Ranges, Memory<byte>?> _readRanges = [];
```

### Constructor
`MemoryPool(int poolSize, bool randomInit)`
- Pool allocation
- Random vs zero initialization
- Slice creation (40+ slices carved from pool)
- Default mapping setup

### Core Methods

#### `ResetRanges()`
Resets memory mappings to power-on state.

#### `SetDefaultReadRanges()` / `SetDefaultWriteRanges()`
Initialize default mapping tables.

#### `ReadPool(int address)` / `WritePool(int address, byte value)`
Direct pool access (bypasses all mapping).

#### `ReadFromRegion(Ranges region, int address)`
Reads from a specific mapped region with lock protection.

#### `WriteToRegion(Ranges region, int address, byte value)`
Writes to a specific mapped region with lock protection, returns false if write-protected.

#### `ReadMapped(ushort address)`
**Critical method** - switch expression that maps addresses to regions.
```csharp
/// <summary>
/// Reads from the mapped memory space using soft switch-configured regions.
/// </summary>
/// <param name="address">16-bit address ($0000-$FFFF).</param>
/// <returns>Byte value from the currently mapped physical location.</returns>
/// <remarks>
/// <para>
/// <strong>Algorithm:</strong> Uses switch expression to determine which region contains
/// the address, then delegates to <see cref="ReadFromRegion"/> for bounds-checked access.
/// </para>
/// <para>
/// <strong>Performance:</strong> Switch expression compiles to efficient jump table or
/// binary search. Region lookup is O(1) via dictionary. Lock-protected for thread safety.
/// </para>
/// <para>
/// <strong>Example:</strong>
/// <code>
/// // Read from $0400 (text page)
/// byte value = ReadMapped(0x0400);
/// // If 80STORE && PAGE2: reads from _a3 (aux text)
/// // Otherwise: reads from _m3 (main text)
/// </code>
/// </para>
/// </remarks>
public byte ReadMapped(ushort address) => address switch { ... };
```

#### `WriteMapped(ushort address, byte value)`
Similar to ReadMapped but handles write-protection and events.

### Soft Switch Setter Methods
All 12 `Set*` methods follow this pattern:
```csharp
/// <summary>
/// Sets the RAMRD soft switch state.
/// </summary>
/// <param name="ramRd">True to enable auxiliary memory for reads.</param>
/// <remarks>
/// When enabled, read operations from most address ranges access auxiliary memory
/// instead of main memory. Calls <see cref="UpdateMemoryMappings"/> to remap memory.
/// </remarks>
public void SetRamRd(bool ramRd)
{
    _ramRd = ramRd;
    UpdateMemoryMappings();
}
```

**Display-only setters** (no mapping effect):
- `SetMixed()`, `SetText()`, `SetAn0-3()`, `Set80Vid()`, `SetAltChar()`

These are stubs marked `/* NA - display only */` because they only affect video
display, not memory mapping.

### UpdateMemoryMappings()
**Critical method** - implements all Apple IIe bank switching logic.

```csharp
/// <summary>
/// Updates memory region mappings based on current soft switch states.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Implements the complex Apple IIe bank switching logic.
/// Called whenever any soft switch changes. Updates both read and write mapping
/// dictionaries to reflect new memory configuration.
/// </para>
/// <para>
/// <strong>Algorithm:</strong>
/// <list type="number">
/// <item>Acquire write lock (blocks all reads and writes during update)</item>
/// <item>Map $0000-$01FF based on ALTZP</item>
/// <item>Map $0200-$BFFF based on RAMRD/RAMWRT and 80STORE rules</item>
/// <item>Map $C100-$CFFF based on INTCXROM/SLOTC3ROM</item>
/// <item>Map $D000-$FFFF based on Language Card switches</item>
/// <item>Release write lock</item>
/// </list>
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> Uses ReaderWriterLockSlim to serialize mapping
/// updates while allowing concurrent reads. This is critical since the CPU thread
/// accesses memory while UI/I/O threads may toggle soft switches.
/// </para>
/// <para>
/// <strong>Complexity:</strong> This method contains the full Apple IIe bank switching
/// logic. It's complex because it must handle all combinations of soft switches:
/// <code>
/// // Example: Text page 1 mapping depends on 80STORE and PAGE2
/// if (!_80Store)
///     _readRanges[Ranges.Region_0400_07FF] = (_ramRd ? _a3 : _m3);
/// else
///     _readRanges[Ranges.Region_0400_07FF] = (_page2 ? _a3 : _m3);
/// </code>
/// </para>
/// <para>
/// <strong>⚠️ FUTURE INTEGRATION:</strong> Currently uses hardcoded slot ROM logic.
/// Will be refactored to integrate with SlotHandler and IExpansionCard system:
/// <code>
/// // Future: Query SlotHandler for ROM presence
/// bool hasCard = _slotHandler.HasCard(slotNumber);
/// Memory&lt;byte&gt;? slotRom = _slotHandler.GetSlotRom(slotNumber);
/// </code>
/// This will allow dynamic expansion card installation/removal without MemoryPool
/// having direct knowledge of card types.
/// </para>
/// </remarks>
private void UpdateMemoryMappings() { ... }
```

### Thread Safety
- `_mappingLock` - ReaderWriterLockSlim for protecting mapping updates

```csharp
/// <summary>
/// Reader/writer lock for thread-safe memory mapping updates.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Pattern:</strong> Allows multiple concurrent readers OR single writer.
/// Read operations (CPU memory access) acquire read lock. Write operations (soft
/// switch updates via <see cref="UpdateMemoryMappings"/>) acquire write lock.
/// </para>
/// <para>
/// <strong>Performance:</strong> Read locks are very fast when no write is pending.
/// Write locks must wait for all readers to finish, but soft switch changes are
/// infrequent compared to memory accesses.
/// </para>
/// </remarks>
private readonly ReaderWriterLockSlim _mappingLock = new(LockRecursionPolicy.NoRecursion);
```

### Dispose()
Releases the ReaderWriterLockSlim.

### InstallApple2ROM(byte[] rom)
Loads 16KB ROM image into the appropriate regions.

```csharp
/// <summary>
/// Loads the Apple IIe ROM image into memory.
/// </summary>
/// <param name="rom">16KB ROM image (must be exactly 16,384 bytes).</param>
/// <exception cref="Exception">Thrown if ROM size is not exactly 16KB.</exception>
/// <remarks>
/// <para>
/// <strong>ROM Layout:</strong> The 16KB ROM is divided into regions:
/// <list type="bullet">
/// <item>$C000-$C0FF (256B): I/O region (_io)</item>
/// <item>$C100-$C7FF (1792B): Internal peripheral ROMs (_int1-7)</item>
/// <item>$C800-$CFFF (2KB): Extended internal ROM (_intext)</item>
/// <item>$D000-$DFFF (4KB): Monitor ROM bank 1 (_rom1)</item>
/// <item>$E000-$FFFF (8KB): Monitor ROM bank 2 + reset vector (_rom2)</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong> Called once during emulator initialization to load the
/// Apple IIe system ROM (typically from "AppleIIe.rom" file).
/// </para>
/// </remarks>
public void InstallApple2ROM(byte[] rom) { ... }
```

---

## Documentation Priority

Given the large size of MemoryPool.cs (~600 lines), prioritize documenting:

1. ✅ **File header** - Design rationale (DONE)
2. ✅ **Class-level docs** - Memory architecture (DONE)
3. ✅ **Soft switches** - Critical for understanding (DONE)
4. **ReadMapped/WriteMapped** - Most frequently called (HIGH PRIORITY)
5. **UpdateMemoryMappings** - Most complex logic (HIGH PRIORITY)
6. **Constructor** - Initialization logic (MEDIUM PRIORITY)
7. **Memory slice fields** - 40+ fields (LOWER PRIORITY - tedious but straightforward)

---

## Future Integration: Slot Handler & Expansion Card System

### Current Limitation (Hardcoded Slot Logic)

Currently, `UpdateMemoryMappings()` uses a hardcoded array to determine slot ROM presence:

```csharp
// Current: Hardcoded slot configuration
bool[] hasCard = [false, true, true, true, true, true, true, true]; // Slots 0-7
```

This makes it impossible to:
- Dynamically install/remove expansion cards
- Support different card types with different memory requirements
- Allow cards to manage their own ROM/RAM spaces
- Hot-swap cards during emulation

### Planned Architecture: SlotHandler Integration

#### SlotHandler Class (To Be Implemented)

```csharp
/// <summary>
/// Manages Apple IIe expansion slots (slots 1-7) and card installation.
/// </summary>
public class SlotHandler
{
    private readonly IExpansionCard?[] _slots = new IExpansionCard?[8]; // Slot 0 unused
    
    /// <summary>
    /// Checks if a card is installed in the specified slot.
    /// </summary>
    public bool HasCard(int slotNumber) => _slots[slotNumber] != null;
    
    /// <summary>
    /// Gets the slot ROM for the specified slot.
    /// </summary>
    /// <returns>256-byte ROM for $Cn00-$CnFF, or null if no card/no ROM.</returns>
    public Memory<byte>? GetSlotRom(int slotNumber)
    {
        return _slots[slotNumber]?.GetSlotRom();
    }
    
    /// <summary>
    /// Gets the extended ROM for the specified slot.
    /// </summary>
    /// <returns>2KB ROM for $C800-$CFFF when slot is selected, or null.</returns>
    public Memory<byte>? GetExtendedRom(int slotNumber)
    {
        return _slots[slotNumber]?.GetExtendedRom();
    }
    
    /// <summary>
    /// Installs an expansion card into the specified slot.
    /// </summary>
    public void InstallCard(int slotNumber, IExpansionCard card)
    {
        if (slotNumber < 1 || slotNumber > 7)
            throw new ArgumentOutOfRangeException(nameof(slotNumber));
        
        _slots[slotNumber] = card;
        card.Initialize(slotNumber);
    }
    
    /// <summary>
    /// Removes the expansion card from the specified slot.
    /// </summary>
    public void RemoveCard(int slotNumber)
    {
        _slots[slotNumber]?.Dispose();
        _slots[slotNumber] = null;
    }
}
```

#### IExpansionCard Interface (To Be Implemented)

```csharp
/// <summary>
/// Interface for Apple IIe expansion cards (peripherals installed in slots 1-7).
/// </summary>
public interface IExpansionCard : IDisposable
{
    /// <summary>
    /// Gets the card name (e.g., "Disk II Controller", "Super Serial Card").
    /// </summary>
    string CardName { get; }
    
    /// <summary>
    /// Gets the slot number this card is installed in (1-7).
    /// </summary>
    int SlotNumber { get; }
    
    /// <summary>
    /// Initializes the card for the specified slot.
    /// </summary>
    void Initialize(int slotNumber);
    
    /// <summary>
    /// Gets the 256-byte slot ROM ($Cn00-$CnFF) for this card.
    /// </summary>
    /// <returns>ROM memory, or null if card has no slot ROM.</returns>
    Memory<byte>? GetSlotRom();
    
    /// <summary>
    /// Gets the 2KB extended ROM ($C800-$CFFF) for this card.
    /// </summary>
    /// <returns>Extended ROM memory, or null if card has no extended ROM.</returns>
    Memory<byte>? GetExtendedRom();
    
    /// <summary>
    /// Handles I/O access to the card's soft switches ($C0n0-$C0nF).
    /// </summary>
    /// <param name="offset">Offset within card's I/O space (0-15).</param>
    /// <param name="isWrite">True if write operation, false if read.</param>
    /// <param name="value">Value to write (ignored for reads).</param>
    /// <returns>Value read (0 for writes).</returns>
    byte HandleIO(byte offset, bool isWrite, byte value);
}
```

### Integration Changes Required in MemoryPool

#### 1. Add SlotHandler Dependency

```csharp
public sealed class MemoryPool : IMemory, ...
{
    private readonly SlotHandler _slotHandler;
    
    public MemoryPool(SlotHandler slotHandler, int poolSize = 0x27F00, bool randomInit = false)
    {
        ArgumentNullException.ThrowIfNull(slotHandler);
        _slotHandler = slotHandler;
        
        // ... rest of constructor
    }
}
```

#### 2. Add IFloatingBusData Dependency

```csharp
public sealed class MemoryPool : IMemory, ...
{
    private readonly SlotHandler _slotHandler;
    private readonly IFloatingBusData _floatingBus;
    
    public MemoryPool(
        SlotHandler slotHandler, 
        IFloatingBusData floatingBus,
        int poolSize = 0x27F00, 
        bool randomInit = false)
    {
        ArgumentNullException.ThrowIfNull(slotHandler);
        ArgumentNullException.ThrowIfNull(floatingBus);
        
        _slotHandler = slotHandler;
        _floatingBus = floatingBus;
        
        // ... rest of constructor
    }
}
```

#### 3. Update UpdateMemoryMappings() to Query SlotHandler

```csharp
public void UpdateMemoryMappings()
{
    _mappingLock.EnterWriteLock();
    try
    {
        // ... existing mapping logic ...
        
        // Replace hardcoded slot logic with dynamic queries
        for (int slot = 1; slot <= 7; slot++)
        {
            var region = slot switch
            {
                1 => Ranges.Region_C100_C1FF,
                2 => Ranges.Region_C200_C2FF,
                3 => Ranges.Region_C300_C3FF,
                4 => Ranges.Region_C400_C4FF,
                5 => Ranges.Region_C500_C5FF,
                6 => Ranges.Region_C600_C6FF,
                7 => Ranges.Region_C700_C7FF,
                _ => throw new InvalidOperationException()
            };
            
            // Query SlotHandler for card presence and ROM
            if (_intCxRom)
            {
                // Internal ROM overrides all slots (except C3 if SLOTC3ROM)
                _readRanges[region] = GetInternalRomForSlot(slot);
            }
            else if (_slotHandler.HasCard(slot))
            {
                // Card ROM is active
                var slotRom = _slotHandler.GetSlotRom(slot);
                _readRanges[region] = slotRom ?? GetInternalRomForSlot(slot);
            }
            else
            {
                // No card, use internal ROM
                _readRanges[region] = GetInternalRomForSlot(slot);
            }
        }
        
        // Handle C3 special case
        if (_slotC3Rom && _slotHandler.HasCard(3))
        {
            _readRanges[Ranges.Region_C300_C3FF] = _slotHandler.GetSlotRom(3) ?? _int3;
        }
        
        // Extended ROM ($C800-$CFFF) will require additional logic
        // to track which slot is currently selected (last slot accessed)
    }
    finally
    {
        _mappingLock.ExitWriteLock();
    }
}
```

#### 4. Add I/O Handling Delegation

```csharp
/// <summary>
/// Handles read from $C0n0-$C0nF (card I/O soft switches).
/// </summary>
/// <remarks>
/// ⚠️ FUTURE: Will delegate to SlotHandler.HandleIO() to allow cards
/// to respond to their soft switch addresses.
/// </remarks>
private byte ReadCardIO(ushort address)
{
    // Extract slot number from address
    int slot = (address >> 4) & 0x07;
    byte offset = (byte)(address & 0x0F);
    
    // Delegate to card if present
    if (_slotHandler.HasCard(slot))
    {
        var card = _slotHandler.GetCard(slot);
        return card.HandleIO(offset, isWrite: false, value: 0);
    }
    
    // No card, return open bus (typically 0 or last value on bus)
    return 0;
}
```

### Example Card Implementations

#### Disk II Controller Card

```csharp
public class DiskIIController : IExpansionCard
{
    private readonly byte[] _slotRom = new byte[256];  // P5A ROM
    private readonly DiskDrive[] _drives = new DiskDrive[2];
    
    public string CardName => "Disk II Controller";
    public int SlotNumber { get; private set; }
    
    public void Initialize(int slotNumber)
    {
        SlotNumber = slotNumber;
        LoadP5ARom(_slotRom);  // Load Disk II bootstrap ROM
    }
    
    public Memory<byte>? GetSlotRom() => _slotRom;
    public Memory<byte>? GetExtendedRom() => null;  // No extended ROM
    
    public byte HandleIO(byte offset, bool isWrite, byte value)
    {
        return offset switch
        {
            0x0 => SelectDrive(0),        // $C0n0: Select drive 1
            0x1 => SelectDrive(1),        // $C0n1: Select drive 2
            0x2 => PhaseOff(0),          // $C0n2: Stepper phase 0 off
            0x3 => PhaseOn(0),           // $C0n3: Stepper phase 0 on
            // ... etc for all 16 soft switches
            _ => 0
        };
    }
    
    // ... disk controller logic ...
}
```

### Benefits of SlotHandler Architecture

1. **Dynamic Card Management:**
   - Install/remove cards at runtime
   - Hot-swap during emulation
   - Easy debugging (swap in test cards)

2. **Encapsulation:**
   - Cards manage their own ROM/RAM spaces
   - Cards handle their own I/O logic
   - MemoryPool doesn't need card-specific knowledge

3. **Extensibility:**
   - Add new card types without modifying MemoryPool
   - Cards can implement complex behaviors (networking, etc.)
   - Easy to create mock cards for testing

4. **Maintainability:**
   - Clear separation of concerns
   - Each card is self-contained
   - Easier to understand and debug

### Migration Path

1. **Phase 1:** Implement SlotHandler and IExpansionCard interfaces
2. **Phase 2:** Create stub cards for testing (NoOpCard, TestCard)
3. **Phase 3:** Update MemoryPool constructor to accept SlotHandler
4. **Phase 4:** Update UpdateMemoryMappings() to use SlotHandler
5. **Phase 5:** Implement real cards (Disk II, Super Serial, etc.)
6. **Phase 6:** Remove hardcoded slot logic completely

This refactoring will happen alongside the general MemoryPool clarity improvements,
resulting in a much cleaner and more maintainable architecture.

---

## Floating Bus Architecture

### What is the Floating Bus?

The Apple II "floating bus" is a hardware characteristic where reads from unmapped memory
addresses return the last value that was on the data bus, rather than a fixed value like 0.
This behavior was used by some software (particularly copy protection and timing-sensitive
code) to:

1. **Detect emulators** - Emulators that return 0 for unmapped reads are detectable
2. **Read video data** - The video scanner puts display memory on the bus during refresh
3. **Timing loops** - Software can detect frame timing by watching video scanner values
4. **Hardware identification** - Different hardware produces different floating bus patterns

### Current Limitation

Currently, `ReadFromRegion()` returns 0 for unmapped regions:

```csharp
private byte ReadFromRegion(Ranges region, int address)
{
    _mappingLock.EnterReadLock();
    try
    {
        _readRanges.TryGetValue(region, out var mem);
        if (!mem.HasValue)
        { 
            return 0;  // ❌ Incorrect - should return floating bus value
        }
        // ...
    }
}
```

This breaks compatibility with software that relies on floating bus behavior.

### Planned Architecture: IFloatingBusData

#### IFloatingBusData Interface (To Be Implemented)

```csharp
/// <summary>
/// Provides floating bus data for unmapped memory reads.
/// </summary>
/// <remarks>
/// <para>
/// The Apple II floating bus is a hardware characteristic where reads from unmapped
/// addresses return the last value that was on the data bus. This is typically the
/// value from the most recent memory access (by CPU, video scanner, or DMA).
/// </para>
/// <para>
/// Multiple implementations are possible:
/// <list type="bullet">
/// <item><strong>LastValueBus:</strong> Returns last CPU read/write (simple, fast)</item>
/// <item><strong>VideoScannerBus:</strong> Returns video memory being scanned (accurate)</item>
/// <item><strong>ZeroBus:</strong> Returns 0 (for testing/debugging, inaccurate)</item>
/// <item><strong>RandomBus:</strong> Returns random values (for testing)</item>
/// </list>
/// </para>
/// </remarks>
public interface IFloatingBusData
{
    /// <summary>
    /// Gets the current floating bus value.
    /// </summary>
    /// <returns>
    /// The byte value that would appear on the data bus for an unmapped read.
    /// Typically the last value that was on the bus from a CPU or video access.
    /// </returns>
    byte GetFloatingBusValue();
    
    /// <summary>
    /// Notifies the floating bus tracker that a value was placed on the bus.
    /// </summary>
    /// <param name="value">The byte value that was on the bus.</param>
    /// <remarks>
    /// Called by memory read/write operations to keep track of the last bus value.
    /// Not all implementations need to use this (e.g., VideoScannerBus reads directly
    /// from video memory).
    /// </remarks>
    void NotifyBusValue(byte value);
}
```

#### LastValueBus Implementation (Simple, Fast)

```csharp
/// <summary>
/// Simple floating bus implementation that returns the last value on the bus.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Accuracy:</strong> Moderately accurate. Real hardware includes video scanner
/// activity, but this only tracks CPU reads/writes. Good enough for most software.
/// </para>
/// <para>
/// <strong>Performance:</strong> Very fast (single field read/write). No overhead.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> Not thread-safe. Assumes single-threaded CPU execution.
/// If multiple threads access memory, use volatile or locks.
/// </para>
/// </remarks>
public class LastValueBus : IFloatingBusData
{
    private byte _lastValue = 0;
    
    public byte GetFloatingBusValue() => _lastValue;
    
    public void NotifyBusValue(byte value) => _lastValue = value;
}
```

#### VideoScannerBus Implementation (Accurate)

```csharp
/// <summary>
/// Accurate floating bus implementation that reads from video memory being scanned.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Accuracy:</strong> Very accurate. Mimics real hardware where the video scanner
/// places display memory on the bus during refresh cycles. Software can read the current
/// scanline being displayed.
/// </para>
/// <para>
/// <strong>Performance:</strong> Moderate overhead. Requires cycle-accurate timing to
/// determine which video memory address is being scanned.
/// </para>
/// <para>
/// <strong>Usage:</strong> Required for software that uses the floating bus to read
/// video memory or detect frame timing (some games, copy protection).
/// </para>
/// </remarks>
public class VideoScannerBus : IFloatingBusData
{
    private readonly IDirectMemoryPoolReader _memory;
    private readonly ISystemClock _clock;  // Cycle-accurate timing
    
    public VideoScannerBus(IDirectMemoryPoolReader memory, ISystemClock clock)
    {
        _memory = memory;
        _clock = clock;
    }
    
    public byte GetFloatingBusValue()
    {
        // Calculate which video address is currently being scanned
        ulong currentCycle = _clock.TotalCycles;
        
        // Apple II video scanner timing (simplified):
        // 65 cycles per scanline, 262 scanlines per frame
        // 17,030 cycles per frame (65 × 262)
        
        int cycleInFrame = (int)(currentCycle % 17030);
        int scanline = cycleInFrame / 65;
        int cycleInLine = cycleInFrame % 65;
        
        // Determine if this is a visible scanline and visible cycle
        if (scanline < 192 && cycleInLine < 40)
        {
            // Calculate Apple II's interleaved video address
            ushort videoAddress = CalculateVideoAddress(scanline, cycleInLine);
            
            // Read from main or aux memory depending on video mode
            // (This is simplified - real implementation needs to check 80STORE, PAGE2, etc.)
            return _memory.ReadRawMain(videoAddress);
        }
        
        // During VBL or HBL, return last CPU value
        return 0;  // Or track last CPU value
    }
    
    public void NotifyBusValue(byte value)
    {
        // Optional: Track last CPU value for use during blanking periods
    }
    
    private ushort CalculateVideoAddress(int scanline, int byteInLine)
    {
        // Apple II video memory interleaving formula
        // This is the actual hardware addressing used by the video scanner
        int block = scanline / 64;           // 0, 1, or 2 (three 64-line blocks)
        int offset = scanline % 64;          // Line within block
        int lineGroup = offset / 8;          // 0-7 (eight groups of 8 lines)
        int lineInGroup = offset % 8;        // 0-7 (line within group)
        
        ushort baseAddress = 0x2000;  // Hi-res page 1 (TODO: check PAGE2 soft switch)
        ushort address = (ushort)(baseAddress + 
                                  (block * 0x28 * 8) +     // Block offset
                                  (lineInGroup * 0x400) +   // Line interleave
                                  (lineGroup * 0x28) +      // Group offset
                                  byteInLine);              // Byte in line
        
        return address;
    }
}
```

#### ZeroBus Implementation (Testing/Debugging)

```csharp
/// <summary>
/// Floating bus implementation that always returns 0.
/// </summary>
/// <remarks>
/// <strong>Warning:</strong> Inaccurate and breaks compatibility with software that
/// relies on floating bus behavior. Use only for testing or debugging.
/// </remarks>
public class ZeroBus : IFloatingBusData
{
    public byte GetFloatingBusValue() => 0;
    public void NotifyBusValue(byte value) { /* Ignore */ }
}
```

### Integration with MemoryPool

#### Update ReadFromRegion to Use Floating Bus

```csharp
private byte ReadFromRegion(Ranges region, int address)
{
    _mappingLock.EnterReadLock();
    try
    {
        _readRanges.TryGetValue(region, out var mem);
        if (!mem.HasValue)
        { 
            // Return floating bus value instead of 0
            byte floatingValue = _floatingBus.GetFloatingBusValue();
            return floatingValue;
        }
        
        var m = mem.Value;
        int baseAddr = (int)region;
        int offset = address - baseAddr;
        if ((uint)offset >= m.Length)
        { 
            return _floatingBus.GetFloatingBusValue();
        }
        
        byte value = m.Span[offset];
        
        // Notify floating bus tracker of the value on the bus
        _floatingBus.NotifyBusValue(value);
        
        return value;
    }
    finally
    {
        _mappingLock.ExitReadLock();
    }
}
```

#### Update WriteToRegion to Track Bus Values

```csharp
private bool WriteToRegion(Ranges region, int address, byte value)
{
    _mappingLock.EnterReadLock();
    try
    {
        _writeRanges.TryGetValue(region, out var mem);
        if (!mem.HasValue)
        {
            // Write to unmapped region is ignored, but value was on the bus
            _floatingBus.NotifyBusValue(value);
            return false;
        }

        var m = mem.Value;
        int baseAddr = (int)region;
        int offset = address - baseAddr;
        if ((uint)offset >= m.Length)
        {
            _floatingBus.NotifyBusValue(value);
            return false;
        }

        m.Span[offset] = value;
        
        // Notify floating bus tracker
        _floatingBus.NotifyBusValue(value);
        
        return true;
    }
    finally
    {
        _mappingLock.ExitReadLock();
    }
}
```

### Alternative Approach: Read/Write Decorator

Instead of modifying `ReadFromRegion`/`WriteToRegion` directly, a decorator pattern
could wrap the MemoryPool to track bus values:

```csharp
/// <summary>
/// Decorator that tracks bus values for floating bus implementation.
/// </summary>
public class FloatingBusMemoryDecorator : IMemory
{
    private readonly IMemory _inner;
    private readonly IFloatingBusData _floatingBus;
    
    public FloatingBusMemoryDecorator(IMemory inner, IFloatingBusData floatingBus)
    {
        _inner = inner;
        _floatingBus = floatingBus;
    }
    
    public byte Read(ushort address)
    {
        byte value = _inner.Read(address);
        _floatingBus.NotifyBusValue(value);
        return value;
    }
    
    public void Write(ushort address, byte value)
    {
        _floatingBus.NotifyBusValue(value);
        _inner.Write(address, value);
    }
    
    // ... other IMemory members delegate to _inner
}

// Usage:
var memoryPool = new MemoryPool(slotHandler, poolSize);
var floatingBus = new LastValueBus();
var memory = new FloatingBusMemoryDecorator(memoryPool, floatingBus);

// Now 'memory' automatically tracks bus values
```

**Benefits of Decorator Approach:**
- Separation of concerns (MemoryPool doesn't need to know about floating bus)
- Can be added/removed without modifying MemoryPool
- Easy to test in isolation
- Can chain multiple decorators (logging, profiling, etc.)

**Drawbacks:**
- Extra method call overhead (though likely negligible)
- Slightly more complex setup

### Expansion Card Floating Bus Access

Expansion cards may also need access to floating bus values:

```csharp
public interface IExpansionCard : IDisposable
{
    // ... existing members ...
    
    /// <summary>
    /// Sets the floating bus data provider for this card.
    /// </summary>
    /// <remarks>
    /// Some cards need to return floating bus values for unmapped I/O reads.
    /// For example, reading from an unused soft switch offset should return
    /// the floating bus value, not 0.
    /// </remarks>
    void SetFloatingBus(IFloatingBusData floatingBus);
}
```

Example card usage:

```csharp
public class DiskIIController : IExpansionCard
{
    private IFloatingBusData? _floatingBus;
    
    public void SetFloatingBus(IFloatingBusData floatingBus)
    {
        _floatingBus = floatingBus;
    }
    
    public byte HandleIO(byte offset, bool isWrite, byte value)
    {
        return offset switch
        {
            0x0 => SelectDrive(0),
            0x1 => SelectDrive(1),
            // ... implemented offsets ...
            
            // Unmapped offsets return floating bus
            _ => _floatingBus?.GetFloatingBusValue() ?? 0
        };
    }
}
```

### Implementation Priority

1. **Phase 1: Interface & LastValueBus** (Easy, fast, good enough for most software)
   - Define IFloatingBusData interface
   - Implement LastValueBus (simple last-value tracking)
   - Update MemoryPool to use it (or use decorator)
   - Test with basic software

2. **Phase 2: Decorator Pattern** (Optional, cleaner separation)
   - Implement FloatingBusMemoryDecorator
   - Test that it doesn't break existing functionality
   - Measure performance impact (should be negligible)

3. **Phase 3: VideoScannerBus** (Harder, needed for accuracy)
   - Implement cycle-accurate video scanner timing
   - Calculate correct video memory addresses
   - Handle different video modes (text, lo-res, hi-res, double hi-res)
   - Test with software that uses floating bus for video reads

4. **Phase 4: Expansion Card Integration**
   - Add SetFloatingBus to IExpansionCard
   - Update card implementations to use it
   - Test with cards that have unmapped I/O offsets

### Software That Relies on Floating Bus

Several Apple II programs use the floating bus:

1. **Copy Protection:**
   - Many games detect emulators by checking for 0 on unmapped reads
   - Proper floating bus behavior defeats these checks

2. **Video Reading:**
   - Some programs read video memory via floating bus during display
   - Used for effects or compression

3. **Timing Loops:**
   - Frame-accurate timing by watching video scanner progress
   - Used in music players and animation

4. **Hardware Detection:**
   - Detecting specific hardware configurations
   - Identifying clone hardware vs Apple hardware

Implementing floating bus correctly significantly improves compatibility!

---

## Real-World Floating Bus Example: 80-Column Ghost Characters

### The Phenomenon

On a 64KB Apple IIe without an 80-column card, enabling 80-column mode produces
**doubled characters** - each 40-column character appears twice horizontally. This is
a classic floating bus behavior:

```
Normal 40-column:     HELLO WORLD
80-column (no card):  HHEELLLLOO  WWOORRLLDD
                      └┬┘└┬┘└┬┘└┬┘  └┬┘└┬┘└┬┘└┬┘
                       └──┴──┴──┴────┴──┴──┴──┴── Ghost characters from floating bus
```

### Why This Happens

**80-Column Display Mode:**
1. Video scanner reads alternating bytes from main and auxiliary memory
2. **Even columns:** Read from main text page ($0400-$07FF)
3. **Odd columns:** Read from auxiliary text page ($10400-$107FF)

**Without 80-Column Card:**
1. Auxiliary memory doesn't exist (no aux RAM installed)
2. **But 80-column firmware is built into the Apple IIe ROM**
3. Video scanner tries to read from auxiliary text page
4. **Floating bus returns the last value on the data bus**
5. The last value was the main memory character (from even column read)
6. Result: Same character displayed twice (doubled/ghosted)

### Detailed Timing

```
Video scanner timing for 80-column mode (simplified):

Cycle 1: Read main[$0400]     → 'H' (0x48)  [Main memory]
         Bus now has 0x48
         Display at position 0

Cycle 2: Read aux[$10400]      → 0x48 (floating bus!) [Non-existent aux memory]
         Bus still has 0x48 from previous read
         Display at position 1
         
Cycle 3: Read main[$0401]     → 'E' (0x45)  [Main memory]
         Bus now has 0x45
         Display at position 2

Cycle 4: Read aux[$10401]      → 0x45 (floating bus!) [Non-existent aux memory]
         Bus still has 0x45 from previous read
         Display at position 3

...and so on for entire line
```

### Hardware vs Emulation

**Real Apple IIe (no 80-column card):**
- Floating bus returns last value (main memory character)
- Characters are doubled: `HHEELLLLOO`

**Emulator returning 0 for unmapped reads:**
- Auxiliary reads return 0x00 (not a valid character)
- Odd columns display spaces or garbage
- Display looks wrong: `H E L L O` (spaced out)

**Emulator with proper floating bus:**
- Auxiliary reads return last bus value (main memory character)
- Characters are doubled exactly like real hardware
- Perfect hardware compatibility! ✅

### Implementation Requirements

To correctly emulate this behavior, the emulator needs:

#### 1. **LastValueBus (Minimum)**

```csharp
// Simple implementation - good enough for this case
public class LastValueBus : IFloatingBusData
{
    private byte _lastValue = 0;
    
    public byte GetFloatingBusValue() => _lastValue;
    public void NotifyBusValue(byte value) => _lastValue = value;
}

// Usage in video renderer:
for (int x = 0; x < 80; x++)
{
    byte charByte;
    
    if (x % 2 == 0)
    {
        // Even column - read from main memory
        charByte = memory.ReadRawMain(0x0400 + (x / 2));
        floatingBus.NotifyBusValue(charByte);  // Track for odd column
    }
    else
    {
        // Odd column - read from aux memory (or floating bus)
        if (hasAuxMemory)
            charByte = memory.ReadRawAux(0x0400 + (x / 2));
        else
            charByte = floatingBus.GetFloatingBusValue();  // Ghost character!
    }
    
    DisplayCharacter(x, charByte);
}
```

#### 2. **VideoScannerBus (More Accurate)**

For even more accuracy, the video scanner bus implementation would automatically
return the correct value based on what the video scanner just read, without needing
manual tracking:

```csharp
public class VideoScannerBus : IFloatingBusData
{
    private readonly IDirectMemoryPoolReader _memory;
    private readonly ISystemClock _clock;
    
    public byte GetFloatingBusValue()
    {
        // Calculate which address the video scanner just accessed
        // This is more complex but more accurate
        ushort lastVideoAddress = CalculateCurrentVideoScanAddress();
        
        // Return the value that's on the bus from the video scanner
        return _memory.ReadRawMain(lastVideoAddress);
    }
    
    // ... implementation ...
}
```

#### 3. **Expansion Card Integration**
   - Add SetFloatingBus to IExpansionCard
   - Update card implementations to use it
   - Test with cards that have unmapped I/O offsets

### Software That Relies on Floating Bus

Several Apple II programs use the floating bus:

1. **Copy Protection:**
   - Many games detect emulators by checking for 0 on unmapped reads
   - Proper floating bus behavior defeats these checks

2. **Video Reading:**
   - Some programs read video memory via floating bus during display
   - Used for effects or compression

3. **Timing Loops:**
   - Frame-accurate timing by watching video scanner progress
   - Used in music players and animation

4. **Hardware Detection:**
   - Detecting specific hardware configurations
   - Identifying clone hardware vs Apple hardware

Implementing floating bus correctly significantly improves compatibility!

---

## Real-World Floating Bus Example: 80-Column Ghost Characters

### The Phenomenon

On a 64KB Apple IIe without an 80-column card, enabling 80-column mode produces
**doubled characters** - each 40-column character appears twice horizontally. This is
a classic floating bus behavior:

```
Normal 40-column:     HELLO WORLD
80-column (no card):  HHEELLLLOO  WWOORRLLDD
                      └┬┘└┬┘└┬┘└┬┘  └┬┘└┬┘└┬┘└┬┘
                       └──┴──┴──┴────┴──┴──┴──┴── Ghost characters from floating bus
```

### Why This Happens

**80-Column Display Mode:**
1. Video scanner reads alternating bytes from main and auxiliary memory
2. **Even columns:** Read from main text page ($0400-$07FF)
3. **Odd columns:** Read from auxiliary text page ($10400-$107FF)

**Without 80-Column Card:**
1. Auxiliary memory doesn't exist (no aux RAM installed)
2. **But 80-column firmware is built into the Apple IIe ROM**
3. Video scanner tries to read from auxiliary text page
4. **Floating bus returns the last value on the data bus**
5. The last value was the main memory character (from even column read)
6. Result: Same character displayed twice (doubled/ghosted)

### Detailed Timing

```
Video scanner timing for 80-column mode (simplified):

Cycle 1: Read main[$0400]     → 'H' (0x48)  [Main memory]
         Bus now has 0x48
         Display at position 0

Cycle 2: Read aux[$10400]      → 0x48 (floating bus!) [Non-existent aux memory]
         Bus still has 0x48 from previous read
         Display at position 1
         
Cycle 3: Read main[$0401]     → 'E' (0x45)  [Main memory]
         Bus now has 0x45
         Display at position 2

Cycle 4: Read aux[$10401]      → 0x45 (floating bus!) [Non-existent aux memory]
         Bus still has 0x45 from previous read
         Display at position 3

...and so on for entire line
```

### Hardware vs Emulation

**Real Apple IIe (no 80-column card):**
- Floating bus returns last value (main memory character)
- Characters are doubled: `HHEELLLLOO`

**Emulator returning 0 for unmapped reads:**
- Auxiliary reads return 0x00 (not a valid character)
- Odd columns display spaces or garbage
- Display looks wrong: `H E L L O` (spaced out)

**Emulator with proper floating bus:**
- Auxiliary reads return last bus value (main memory character)
- Characters are doubled exactly like real hardware
- Perfect hardware compatibility! ✅

### Implementation Requirements

To correctly emulate this behavior, the emulator needs:

#### 1. **LastValueBus (Minimum)**

```csharp
// Simple implementation - good enough for this case
public class LastValueBus : IFloatingBusData
{
    private byte _lastValue = 0;
    
    public byte GetFloatingBusValue() => _lastValue;
    public void NotifyBusValue(byte value) => _lastValue = value;
}

// Usage in video renderer:
for (int x = 0; x < 80; x++)
{
    byte charByte;
    
    if (x % 2 == 0)
    {
        // Even column - read from main memory
        charByte = memory.ReadRawMain(0x0400 + (x / 2));
        floatingBus.NotifyBusValue(charByte);  // Track for odd column
    }
    else
    {
        // Odd column - read from aux memory (or floating bus)
        if (hasAuxMemory)
            charByte = memory.ReadRawAux(0x0400 + (x / 2));
        else
            charByte = floatingBus.GetFloatingBusValue();  // Ghost character!
    }
    
    DisplayCharacter(x, charByte);
}
```

#### 2. **VideoScannerBus (More Accurate)**

For even more accuracy, the video scanner bus implementation would automatically
return the correct value based on what the video scanner just read, without needing
manual tracking:

```csharp
public class VideoScannerBus : IFloatingBusData
{
    private readonly IDirectMemoryPoolReader _memory;
    private readonly ISystemClock _clock;
    
    public byte GetFloatingBusValue()
    {
        // Calculate which address the video scanner just accessed
        // This is more complex but more accurate
        ushort lastVideoAddress = CalculateCurrentVideoScanAddress();
        
        // Return the value that's on the bus from the video scanner
        return _memory.ReadRawMain(lastVideoAddress);
    }
    
    // ... implementation ...
}
```
