# Disk II Integration Plan

## Overview

This document provides a comprehensive plan for integrating the Disk II emulation code from `Pandowdy.DiskImportCode` into `Pandowdy.EmuCore`. The code implements full Disk II controller card and drive emulation, supporting multiple disk image formats (NIB, WOZ, DSK/DO/PO).

**Source Project:** `Pandowdy.DiskImportCode` (temporary staging project)  
**Target Project:** `Pandowdy.EmuCore`  
**Branch:** `piecemealing`

---

## Table of Contents

1. [Source Code Inventory](#source-code-inventory)
2. [Architecture Overview](#architecture-overview)
3. [Integration Issues & Resolutions](#integration-issues--resolutions)
4. [Target File Structure](#target-file-structure)
5. [Phase 1: Foundation](#phase-1-foundation)
6. [Phase 2: Interfaces](#phase-2-interfaces)
7. [Phase 3: Disk Image Providers](#phase-3-disk-image-providers)
8. [Phase 4: Drive Implementation](#phase-4-drive-implementation)
9. [Phase 5: Controller Card](#phase-5-controller-card)
10. [Phase 6: Factory Registration](#phase-6-factory-registration)
11. [Phase 7: Tests](#phase-7-tests)
12. [Telemetry Integration](#telemetry-integration)
13. [Code Style Corrections](#code-style-corrections)
14. [Verification Checklist](#verification-checklist)

---

## Source Code Inventory

### Files in `Pandowdy.DiskImportCode`

| File | Lines | Purpose | Integration Notes |
|------|-------|---------|-------------------|
| `DiskIIControllerCards.cs` | ~1150 | Controller card base + 13/16-sector variants | Split into 3 files |
| `DiskIIDrive.cs` | ~310 | Physical drive mechanics | Minor refactoring |
| `DiskIIDebugDecorator.cs` | ~125 | Debug logging decorator | Refactor for telemetry |
| `DiskIIStatusDecorator.cs` | ~200 | Status provider integration | **Replace with telemetry** |
| `DiskIIFactory.cs` | ~125 | Drive factory with decorator chain | Update for telemetry |
| `DiskImageFactory.cs` | ~72 | Disk format detection & provider creation | Minor cleanup |
| `GcrEncoder.cs` | ~205 | GCR track synthesis from sectors | Clean import |
| `NibDiskImageProvider.cs` | ~320 | .nib format provider | Clean import |
| `SectorDiskImageProvider.cs` | ~294 | .dsk/.do/.po format provider | Clean import |
| `InternalWozDiskImageProvider.cs` | ~610 | Native .woz parser | Clean import |
| `WozDiskImageProvider.cs` | ~200 | CiderPress2-based .woz provider | Clean import |
| `NullDiskIIDrive.cs` | ~90 | Null object pattern for testing | Clean import |
| `IDiskIIDrive.cs` | ~32 | Drive interface | Clean import |
| `IDiskImageProvider.cs` | ~60 | Disk image abstraction | Clean import |
| `IDiskImageFactory.cs` | ~26 | Factory interface | Clean import |

### Interfaces Defined Inline (to extract)

| Interface | Current Location | Notes |
|-----------|-----------------|-------|
| `IDiskIIFactory` | `DiskIIControllerCards.cs:7-10` | Move to `Interfaces/` |

---

## Architecture Overview

### Component Relationships

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          DiskIIControllerCard                                    │
│  - Manages I/O addresses ($C0x0-$C0xF)                                          │
│  - Stepper motor phase control (quarter-track positioning)                       │
│  - Q6/Q7 mode switching (read/write/protect-sense)                              │
│  - Shift register for bit accumulation                                          │
│  - VBlank subscription for motor-off timeout                                     │
└────────────────────────────────────────┬────────────────────────────────────────┘
                                         │ contains
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    IDiskIIDrive (Drive 1, Drive 2)                               │
│  - Physical mechanics: motor on/off, quarter-track stepping                      │
│  - Bit read/write operations delegated to IDiskImageProvider                     │
│  - Disk insert/eject operations                                                  │
└────────────────────────────────────────┬────────────────────────────────────────┘
                                         │ uses
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          IDiskImageProvider                                      │
│  - Format-specific disk data access                                             │
│  - Track/sector encoding/decoding                                               │
│  - Cycle-based bit timing for accurate rotation                                  │
│                                                                                  │
│  Implementations:                                                                │
│  - NibDiskImageProvider (.nib - raw GCR nibbles)                                │
│  - InternalWozDiskImageProvider (.woz - flux timing)                            │
│  - WozDiskImageProvider (.woz via CiderPress2)                                  │
│  - SectorDiskImageProvider (.dsk/.do/.po - synthesized GCR)                     │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Decorator Chain (Current - TO BE CHANGED)

```
Current:    DiskIIDebugDecorator → DiskIIStatusDecorator → DiskIIDrive
                   │                        │
                   ▼                        ▼
            Debug.WriteLine()      IDiskStatusMutator (doesn't exist!)
```

### Decorator Chain (New - Using Telemetry)

```
New:        DiskIIDebugDecorator → DiskIIDrive
                                       │
                                       ▼
                              ITelemetryAggregator
                                       │
                              Publishes: TelemetryMessage
                              Category: "DiskII"
```

---

## Integration Issues & Resolutions

### Issue 1: Missing VBlankOccurred Event

**Problem:** `DiskIIControllerCard` subscribes to `_clocking.VBlankOccurred` (line 141) but this event doesn't exist in `CpuClockingCounters`.

**Resolution:** Add `VBlankOccurred` event to `CpuClockingCounters` that fires when `CheckAndAdvanceVBlank()` returns true.

**File:** `Pandowdy.EmuCore\DataTypes\CpuClockingCounters.cs`

```csharp
// Add event declaration
public event Action? VBlankOccurred;

// Modify CheckAndAdvanceVBlank() to invoke event
public bool CheckAndAdvanceVBlank()
{
    if (_totalCycleCount < _nextVblankCycle)
    {
        return false;
    }

    do
    {
        _nextVblankCycle += CyclesPerVBlank;
        ResetVBlankCounter();
    }
    while (_totalCycleCount >= _nextVblankCycle);

    // Fire event for subscribers (e.g., disk controller motor-off timing)
    VBlankOccurred?.Invoke();
    
    return true;
}
```

---

### Issue 2: IDiskStatusMutator Doesn't Exist

**Problem:** The original code uses `IDiskStatusMutator` for status updates, but this interface doesn't exist.

**Resolution:** Replace with `ITelemetryAggregator` pattern. The drive will publish telemetry messages directly.

**Old Pattern (in DiskIIStatusDecorator):**
```csharp
_statusMutator.MutateDrive(_slotNumber, _driveNumber, builder =>
{
    builder.MotorOn = value;
});
```

**New Pattern (direct in DiskIIDrive):**
```csharp
_telemetry.Publish(new TelemetryMessage(_telemetryId, "motor", _motorOn));
```

**Telemetry Message Types for DiskII:**

| MessageType | Payload | Description |
|-------------|---------|-------------|
| `"motor"` | `bool` | Motor on/off state |
| `"track"` | `double` | Current track position (e.g., 17.25) |
| `"sector"` | `int` | Current sector being accessed |
| `"disk-inserted"` | `string` | Disk image filename |
| `"disk-ejected"` | `null` | Disk was ejected |
| `"write-protect"` | `bool` | Write protection state |
| `"phase"` | `byte` | Phase state bitfield (4 bits) |
| `"motor-off-scheduled"` | `bool` | Motor-off pending (1-sec delay) |
| `"state"` | `DiskIIState` | Full state snapshot (for resend) |

---

### Issue 3: Duplicate IDiskIIFactory Interface

**Problem:** `IDiskIIFactory` defined twice (in `DiskIIControllerCards.cs:7-10` and as separate concept).

**Resolution:** Keep only one definition in `Interfaces/IDiskIIFactory.cs`. Remove inline definition.

---

### Issue 4: SlotNumber Enum Reference

**Problem:** Code uses `SlotNumber` enum that may not be accessible.

**Resolution:** `SlotNumber` is defined in `Pandowdy.EmuCore.Interfaces` (verified in `ICard.cs`). Ensure proper using directive.

---

### Issue 5: Shared Constants Duplication

**Problem:** `CYCLES_PER_BIT = 45.0 / 11.0` is duplicated in multiple files.

**Resolution:** Create `DiskIIConstants.cs` with shared timing constants.

```csharp
namespace Pandowdy.EmuCore.DiskII;

/// <summary>
/// Shared constants for Disk II emulation timing.
/// </summary>
public static class DiskIIConstants
{
    /// <summary>
    /// Cycles per bit for accurate Apple II Disk II timing.
    /// The disk reads at 250 kHz (4μs per bit) while the CPU runs at 1.023 MHz.
    /// This gives exactly 45/11 cycles per bit ≈ 4.090909 cycles/bit.
    /// </summary>
    public const double CyclesPerBit = 45.0 / 11.0; // 4.090909...
    
    /// <summary>
    /// Number of tracks on a standard 5.25" disk.
    /// </summary>
    public const int TrackCount = 35;
    
    /// <summary>
    /// Bytes per track in NIB format.
    /// </summary>
    public const int BytesPerNibTrack = 6656;
    
    /// <summary>
    /// Bits per track (6656 * 8 = 53,248).
    /// </summary>
    public const int BitsPerTrack = BytesPerNibTrack * 8;
    
    /// <summary>
    /// Maximum quarter-track position (35 tracks * 4 quarters + 1).
    /// </summary>
    public const int MaxQuarterTracks = 35 * 4 + 1; // 141
    
    /// <summary>
    /// Sectors per track for 16-sector format.
    /// </summary>
    public const int SectorsPerTrack = 16;
    
    /// <summary>
    /// Bytes per sector.
    /// </summary>
    public const int BytesPerSector = 256;
    
    /// <summary>
    /// Motor-off delay in CPU cycles (~1 second at 1.023 MHz).
    /// </summary>
    public const ulong MotorOffDelayCycles = 1_000_000;
    
    /// <summary>
    /// Telemetry category identifier for Disk II devices.
    /// </summary>
    public const string TelemetryCategory = "DiskII";
}
```

---

## Target File Structure

```
Pandowdy.EmuCore/
├── Interfaces/
│   ├── IDiskIIDrive.cs               (NEW - from DiskImportCode)
│   ├── IDiskIIFactory.cs             (NEW - extracted from DiskIIControllerCards)
│   ├── IDiskImageFactory.cs          (NEW - from DiskImportCode)
│   └── IDiskImageProvider.cs         (NEW - from DiskImportCode)
│
├── DataTypes/
│   ├── CpuClockingCounters.cs        (MODIFY - add VBlankOccurred event)
│   └── DiskIITelemetryPayloads.cs    (NEW - telemetry payload records)
│
├── Cards/
│   ├── DiskIIControllerCard.cs       (NEW - base class extracted)
│   ├── DiskIIControllerCard16Sector.cs (NEW - 16-sector ROM variant)
│   └── DiskIIControllerCard13Sector.cs (NEW - 13-sector ROM variant)
│
├── DiskII/
│   ├── DiskIIConstants.cs            (NEW - shared constants)
│   ├── DiskIIDrive.cs                (NEW - modified for telemetry)
│   ├── DiskIIFactory.cs              (NEW - simplified, no status decorator)
│   ├── DiskIIDebugDecorator.cs       (NEW - from DiskImportCode)
│   ├── NullDiskIIDrive.cs            (NEW - from DiskImportCode)
│   ├── GcrEncoder.cs                 (NEW - from DiskImportCode)
│   └── Providers/
│       ├── DiskImageFactory.cs           (NEW - from DiskImportCode)
│       ├── NibDiskImageProvider.cs       (NEW - from DiskImportCode)
│       ├── SectorDiskImageProvider.cs    (NEW - from DiskImportCode)
│       ├── InternalWozDiskImageProvider.cs (NEW - from DiskImportCode)
│       └── WozDiskImageProvider.cs       (NEW - optional CiderPress2 version)
│
├── Services/
│   └── CardFactory.cs                (MODIFY - register Disk II cards)
│
└── ... (existing files unchanged)

Pandowdy.EmuCore.Tests/
├── DiskII/
│   ├── DiskIIDriveTests.cs           (NEW)
│   ├── DiskIIControllerCardTests.cs  (NEW)
│   ├── GcrEncoderTests.cs            (NEW)
│   └── Providers/
│       ├── NibDiskImageProviderTests.cs   (NEW)
│       ├── SectorDiskImageProviderTests.cs (NEW)
│       └── InternalWozDiskImageProviderTests.cs (NEW)
└── ...
```

---

## Phase 1: Foundation

**Goal:** Add prerequisite infrastructure to EmuCore.

### Step 1.1: Add VBlankOccurred Event

**File:** `Pandowdy.EmuCore\DataTypes\CpuClockingCounters.cs`

**Changes:**
1. Add event declaration
2. Invoke event in `CheckAndAdvanceVBlank()`

```csharp
// Add after line 11 (_nextVblankCycle field)
/// <summary>
/// Event fired when a VBlank transition occurs.
/// Subscribers can use this for timing-dependent operations (e.g., motor-off delays).
/// </summary>
public event Action? VBlankOccurred;

// Modify CheckAndAdvanceVBlank() - add after line 159
VBlankOccurred?.Invoke();
```

### Step 1.2: Create DiskIIConstants

**File:** `Pandowdy.EmuCore\DiskII\DiskIIConstants.cs`

Create new file with shared constants (see Issue 5 resolution above).

### Step 1.3: Create Telemetry Payload Types

**File:** `Pandowdy.EmuCore\DataTypes\DiskIITelemetryPayloads.cs`

```csharp
namespace Pandowdy.EmuCore.DataTypes;

/// <summary>
/// Full state snapshot for a Disk II drive, used for telemetry resend requests.
/// </summary>
/// <param name="SlotNumber">Slot where the controller is installed (1-7).</param>
/// <param name="DriveNumber">Drive number on the controller (1-2).</param>
/// <param name="MotorOn">Current motor state.</param>
/// <param name="Track">Current track position (e.g., 17.25).</param>
/// <param name="Sector">Last accessed sector (0-15), or -1 if unknown.</param>
/// <param name="DiskImageFilename">Filename of inserted disk, or empty if none.</param>
/// <param name="IsWriteProtected">Whether disk is write-protected.</param>
/// <param name="PhaseState">Stepper motor phase bitfield (4 bits).</param>
/// <param name="MotorOffScheduled">Whether motor-off is pending (1-sec delay).</param>
public readonly record struct DiskIIState(
    int SlotNumber,
    int DriveNumber,
    bool MotorOn,
    double Track,
    int Sector,
    string DiskImageFilename,
    bool IsWriteProtected,
    byte PhaseState,
    bool MotorOffScheduled);

/// <summary>
/// Track change payload for Disk II telemetry.
/// </summary>
/// <param name="Track">New track position.</param>
/// <param name="Sector">Current sector, or -1 if unknown.</param>
public readonly record struct DiskIITrackInfo(double Track, int Sector);
```

### Step 1.4: Verify Build

After Phase 1 changes, run build to ensure no regressions.

---

## Phase 2: Interfaces

**Goal:** Add interface files to `Interfaces/` folder.

### Step 2.1: Copy IDiskImageProvider.cs

**Source:** `Pandowdy.DiskImportCode\IDiskImageProvider.cs`  
**Target:** `Pandowdy.EmuCore\Interfaces\IDiskImageProvider.cs`

**Changes:** None required - already uses correct namespace.

### Step 2.2: Copy IDiskIIDrive.cs

**Source:** `Pandowdy.DiskImportCode\IDiskIIDrive.cs`  
**Target:** `Pandowdy.EmuCore\Interfaces\IDiskIIDrive.cs`

**Changes:** Clean up comments, ensure proper XML documentation.

### Step 2.3: Copy IDiskImageFactory.cs

**Source:** `Pandowdy.DiskImportCode\IDiskImageFactory.cs`  
**Target:** `Pandowdy.EmuCore\Interfaces\IDiskImageFactory.cs`

**Changes:** None required.

### Step 2.4: Create IDiskIIFactory.cs

**Target:** `Pandowdy.EmuCore\Interfaces\IDiskIIFactory.cs`

```csharp
namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Factory for creating Disk II drive instances.
/// </summary>
public interface IDiskIIFactory
{
    /// <summary>
    /// Creates a Disk II drive with no disk inserted.
    /// </summary>
    /// <param name="driveName">Name for the drive (e.g., "Slot6-D1").</param>
    /// <returns>A new drive instance ready for disk insertion.</returns>
    IDiskIIDrive CreateDrive(string driveName);
}
```

### Step 2.5: Verify Build

Ensure all interfaces compile correctly.

---

## Phase 3: Disk Image Providers

**Goal:** Add disk format providers and supporting classes.

### Step 3.1: Create DiskII folder and GcrEncoder

**Target:** `Pandowdy.EmuCore\DiskII\GcrEncoder.cs`

**Source:** `Pandowdy.DiskImportCode\GcrEncoder.cs`

**Changes:**
- Update namespace to `Pandowdy.EmuCore.DiskII`

### Step 3.2: Create Providers subfolder

Create directory: `Pandowdy.EmuCore\DiskII\Providers\`

### Step 3.3: Copy DiskImageFactory

**Source:** `Pandowdy.DiskImportCode\DiskImageFactory.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\Providers\DiskImageFactory.cs`

**Changes:**
- Update namespace to `Pandowdy.EmuCore.DiskII.Providers`
- Add `using Pandowdy.EmuCore.Interfaces;`

### Step 3.4: Copy NibDiskImageProvider

**Source:** `Pandowdy.DiskImportCode\NibDiskImageProvider.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\Providers\NibDiskImageProvider.cs`

**Changes:**
- Update namespace to `Pandowdy.EmuCore.DiskII.Providers`
- Replace `CYCLES_PER_BIT` with `DiskIIConstants.CyclesPerBit`
- Replace other magic numbers with constants

### Step 3.5: Copy SectorDiskImageProvider

**Source:** `Pandowdy.DiskImportCode\SectorDiskImageProvider.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\Providers\SectorDiskImageProvider.cs`

**Changes:**
- Update namespace to `Pandowdy.EmuCore.DiskII.Providers`
- Use `DiskIIConstants`

### Step 3.6: Copy InternalWozDiskImageProvider

**Source:** `Pandowdy.DiskImportCode\InternalWozDiskImageProvider.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\Providers\InternalWozDiskImageProvider.cs`

**Changes:**
- Update namespace to `Pandowdy.EmuCore.DiskII.Providers`
- Use `DiskIIConstants`

### Step 3.7: Copy WozDiskImageProvider (Optional)

**Source:** `Pandowdy.DiskImportCode\WozDiskImageProvider.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\Providers\WozDiskImageProvider.cs`

**Changes:**
- Update namespace
- Use `DiskIIConstants`

### Step 3.8: Verify Build

Ensure all providers compile correctly.

---

## Phase 4: Drive Implementation

**Goal:** Add drive implementation with telemetry integration.

### Step 4.1: Copy NullDiskIIDrive

**Source:** `Pandowdy.DiskImportCode\NullDiskIIDrive.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\NullDiskIIDrive.cs`

**Changes:**
- Update namespace to `Pandowdy.EmuCore.DiskII`
- Fix brace style (see Code Style Corrections)

### Step 4.2: Create DiskIIDrive with Telemetry

**Source:** `Pandowdy.DiskImportCode\DiskIIDrive.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\DiskIIDrive.cs`

**Major Changes:**
1. Update namespace to `Pandowdy.EmuCore.DiskII`
2. Add `ITelemetryAggregator` dependency
3. Create telemetry ID in constructor
4. Publish telemetry on state changes
5. Subscribe to resend requests
6. Remove dependency on `IDiskImageFactory` from constructor (simplify)

**Modified Constructor:**
```csharp
public DiskIIDrive(
    string name, 
    ITelemetryAggregator telemetry,
    int slotNumber,
    int driveNumber,
    IDiskImageProvider? imageProvider = null)
{
    Name = name ?? "Unnamed";
    _telemetry = telemetry;
    _slotNumber = slotNumber;
    _driveNumber = driveNumber;
    _imageProvider = imageProvider;
    
    // Register with telemetry system
    _telemetryId = telemetry.CreateId(DiskIIConstants.TelemetryCategory);
    
    // Subscribe to resend requests
    telemetry.ResendRequests
        .Where(r => r.MatchesProvider(_telemetryId))
        .Subscribe(_ => PublishFullState());
    
    Reset();
    _imageProvider?.SetQuarterTrack(_quarterSteps);
}
```

**Add Telemetry Publishing:**
```csharp
private void PublishFullState()
{
    var state = new DiskIIState(
        _slotNumber,
        _driveNumber,
        _motorOn,
        Track,
        -1, // Sector unknown at drive level
        _imageProvider?.FilePath ?? string.Empty,
        IsWriteProtected(),
        0, // Phase state managed by controller
        false); // Motor-off scheduled managed by controller
    
    _telemetry.Publish(new TelemetryMessage(_telemetryId, "state", state));
}

// Modify MotorOn setter:
public bool MotorOn
{
    get => _motorOn;
    set
    {
        if (_motorOn != value)
        {
            _motorOn = value;
            _telemetry.Publish(new TelemetryMessage(_telemetryId, "motor", value));
            Debug.WriteLine($"Drive motor turned {(value ? "ON" : "OFF")}");
        }
    }
}

// Add telemetry to StepToHigherTrack/StepToLowerTrack:
// After position changes:
_telemetry.Publish(new TelemetryMessage(_telemetryId, "track", Track));
```

### Step 4.3: Copy DiskIIDebugDecorator

**Source:** `Pandowdy.DiskImportCode\DiskIIDebugDecorator.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\DiskIIDebugDecorator.cs`

**Changes:**
- Update namespace to `Pandowdy.EmuCore.DiskII`
- Fix brace style

### Step 4.4: Remove DiskIIStatusDecorator

**Action:** Do NOT copy this file. Functionality replaced by telemetry in DiskIIDrive.

### Step 4.5: Create DiskIIFactory

**Source:** `Pandowdy.DiskImportCode\DiskIIFactory.cs`  
**Target:** `Pandowdy.EmuCore\DiskII\DiskIIFactory.cs`

**Major Changes:**
1. Update namespace to `Pandowdy.EmuCore.DiskII`
2. Remove `IDiskStatusMutator` parameter
3. Add `ITelemetryAggregator` parameter
4. Remove `DiskIIStatusDecorator` from chain
5. Pass telemetry, slot, and drive to DiskIIDrive

**Simplified Factory:**
```csharp
namespace Pandowdy.EmuCore.DiskII;

public class DiskIIFactory(
    IDiskImageFactory imageFactory, 
    ITelemetryAggregator telemetry) : IDiskIIFactory
{
    public IDiskIIDrive CreateDrive(string driveName)
    {
        var (slotNumber, driveNumber) = ParseDriveName(driveName);
        
        // Create core drive with telemetry integration
        var coreDrive = new DiskIIDrive(
            driveName, 
            telemetry, 
            slotNumber, 
            driveNumber);
        
        // Wrap in debug decorator (outermost layer)
        return new DiskIIDebugDecorator(coreDrive);
    }
    
    // ... ParseDriveName unchanged
}
```

### Step 4.6: Verify Build

Ensure all drive components compile correctly.

---

## Phase 5: Controller Card

**Goal:** Add controller card implementations.

### Step 5.1: Create Cards folder

Create directory: `Pandowdy.EmuCore\Cards\` (if not exists)

### Step 5.2: Extract DiskIIControllerCard Base Class

**Source:** `Pandowdy.DiskImportCode\DiskIIControllerCards.cs` (lines 32-1070)  
**Target:** `Pandowdy.EmuCore\Cards\DiskIIControllerCard.cs`

**Changes:**
1. Update namespace to `Pandowdy.EmuCore.Cards`
2. Remove `IDiskStatusMutator` dependency
3. Add `ITelemetryAggregator` dependency
4. Create telemetry ID for controller-level events
5. Replace `_diskStatusMutator.MutateDrive(...)` calls with telemetry publishes
6. Update constructor to use `IDiskIIFactory` properly
7. Use `DiskIIConstants` for timing values
8. Fix brace style throughout

**Key Telemetry Changes:**

```csharp
// In UpdatePhaseState():
private void UpdatePhaseState()
{
    // Publish phase state through telemetry
    // Note: Drives publish their own state; controller publishes phase
    _telemetry.Publish(new TelemetryMessage(
        _telemetryId, 
        "phase", 
        new { DriveIndex = _selectedDriveIndex + 1, Phase = _currentPhase }));
}

// In UpdateMotorOffScheduledStatus():
private void UpdateMotorOffScheduledStatus(bool scheduled)
{
    _telemetry.Publish(new TelemetryMessage(
        _telemetryId,
        "motor-off-scheduled",
        new { DriveIndex = _selectedDriveIndex + 1, Scheduled = scheduled }));
}

// In UpdateTrackAndSector():
private void UpdateTrackAndSector(double track, int sector)
{
    _telemetry.Publish(new TelemetryMessage(
        _telemetryId,
        "sector",
        new DiskIITrackInfo(track, sector)));
}
```

### Step 5.3: Extract DiskIIControllerCard16Sector

**Source:** `Pandowdy.DiskImportCode\DiskIIControllerCards.cs` (lines 1072-1113)  
**Target:** `Pandowdy.EmuCore\Cards\DiskIIControllerCard16Sector.cs`

**Changes:**
- Separate file with 16-sector boot ROM
- Update constructor signature
- Use `DiskIIConstants`

### Step 5.4: Extract DiskIIControllerCard13Sector

**Source:** `Pandowdy.DiskImportCode\DiskIIControllerCards.cs` (lines 1115-1152)  
**Target:** `Pandowdy.EmuCore\Cards\DiskIIControllerCard13Sector.cs`

**Changes:**
- Separate file with 13-sector boot ROM
- Update constructor signature
- Use `DiskIIConstants`

### Step 5.5: Add ICard.Slot Property

The current import code stores `_slotNumber` but ICard now has a `Slot` property. Need to implement this.

### Step 5.6: Verify Build

Ensure all card components compile correctly.

---

## Phase 6: Factory Registration

**Goal:** Register Disk II cards with CardFactory.

### Step 6.1: Modify CardFactory

**File:** `Pandowdy.EmuCore\Services\CardFactory.cs`

**Changes:**
1. Add dependency on `ITelemetryAggregator`
2. Add dependency on `IDiskImageFactory`
3. Create `IDiskIIFactory` instance
4. Register DiskIIControllerCard16Sector
5. Register DiskIIControllerCard13Sector

### Step 6.2: Verify Build

Ensure CardFactory compiles and all cards are registered.

---

## Phase 7: Tests

**Goal:** Create comprehensive test coverage.

### Step 7.1: Create Test Directory Structure

```
Pandowdy.EmuCore.Tests/
├── DiskII/
│   ├── DiskIIDriveTests.cs
│   ├── DiskIIControllerCardTests.cs
│   ├── DiskIIFactoryTests.cs
│   ├── GcrEncoderTests.cs
│   └── Providers/
│       ├── DiskImageFactoryTests.cs
│       ├── NibDiskImageProviderTests.cs
│       ├── SectorDiskImageProviderTests.cs
│       └── InternalWozDiskImageProviderTests.cs
```

### Step 7.2: Create Mock Telemetry Aggregator

```csharp
public class MockTelemetryAggregator : ITelemetryAggregator
{
    private int _nextId = 0;
    private readonly Subject<TelemetryMessage> _messageSubject = new();
    private readonly Subject<ResendRequest> _resendSubject = new();
    
    public List<TelemetryMessage> PublishedMessages { get; } = new();
    
    public TelemetryId CreateId(string category) => 
        new(Interlocked.Increment(ref _nextId), category);
    
    public void Publish(TelemetryMessage message)
    {
        PublishedMessages.Add(message);
        _messageSubject.OnNext(message);
    }
    
    public IObservable<TelemetryMessage> Stream => _messageSubject.AsObservable();
    public IObservable<ResendRequest> ResendRequests => _resendSubject.AsObservable();
    public void PublishResendRequest(ResendRequest request) => _resendSubject.OnNext(request);
}
```

### Step 7.3: Priority Test Areas

1. **DiskIIDrive:** Motor control, track stepping, bit read/write
2. **DiskIIControllerCard:** Phase control, Q6/Q7 modes, shift register
3. **GcrEncoder:** Address/data field encoding, 6&2 encoding
4. **NibDiskImageProvider:** File loading, bit reading, track positioning
5. **Telemetry Integration:** Verify messages published on state changes

---

## Telemetry Integration

### Category and Message Types

| Category | MessageType | Payload Type | Description |
|----------|-------------|--------------|-------------|
| `DiskII` | `motor` | `bool` | Motor on/off |
| `DiskII` | `track` | `double` | Track position (0-34.75) |
| `DiskII` | `sector` | `DiskIITrackInfo` | Track + sector |
| `DiskII` | `disk-inserted` | `string` | Disk filename |
| `DiskII` | `disk-ejected` | `null` | Disk removed |
| `DiskII` | `write-protect` | `bool` | Write protection |
| `DiskII` | `phase` | `{ DriveIndex, Phase }` | Stepper phases |
| `DiskII` | `motor-off-scheduled` | `{ DriveIndex, Scheduled }` | Motor-off pending |
| `DiskII` | `state` | `DiskIIState` | Full state snapshot |

### UI Subscription Pattern

```csharp
// In DiskStatusViewModel:
public DiskStatusViewModel(ITelemetryStream telemetry)
{
    telemetry.Stream
        .Where(m => m.SourceId.Category == "DiskII")
        .ObserveOn(RxApp.MainThreadScheduler)
        .Subscribe(HandleDiskTelemetry);
    
    // Request current state on startup
    // (Via IEmulatorCoreInterface.RequestTelemetryResendByCategory)
}

private void HandleDiskTelemetry(TelemetryMessage msg)
{
    switch (msg.MessageType)
    {
        case "motor":
            UpdateMotorState(msg.SourceId.Id, (bool)msg.Payload!);
            break;
        case "track":
            UpdateTrack(msg.SourceId.Id, (double)msg.Payload!);
            break;
        case "state":
            UpdateFullState((DiskIIState)msg.Payload!);
            break;
        // ... etc
    }
}
```

---

## Code Style Corrections

The following style issues need to be fixed during import (per `.github\copilot-instructions.md`):

### 1. Always Use Braces

**Before:**
```csharp
if (condition)
    DoSomething();
```

**After:**
```csharp
if (condition)
{
    DoSomething();
}
```

### 2. Multi-line Properties with Logic

**Before:**
```csharp
public bool MotorOn { get => _motorOn; set { _motorOn = value; Debug.WriteLine("..."); } }
```

**After:**
```csharp
public bool MotorOn
{
    get => _motorOn;
    set
    {
        _motorOn = value;
        Debug.WriteLine("...");
    }
}
```

### 3. Consistent Indentation

Ensure 4-space indentation throughout.

---

## Verification Checklist

### After Each Phase

- [ ] Solution builds without errors
- [ ] No new warnings introduced
- [ ] Existing tests pass

### After Complete Integration

- [ ] DiskII controller cards appear in CardFactory
- [ ] Card can be installed in slot 6
- [ ] NIB disk images can be loaded
- [ ] WOZ disk images can be loaded
- [ ] DSK/DO/PO disk images can be loaded
- [ ] Motor control works (on/off with 1-sec delay)
- [ ] Track stepping works (0-34.75 quarter-tracks)
- [ ] Telemetry messages are published
- [ ] Telemetry resend requests work
- [ ] Boot from disk works (test with DOS 3.3 or ProDOS)

### Test Disk Images

Store test disk images in `assets/test-disks/`:
- `dos33-master.dsk` - DOS 3.3 system master
- `prodos.po` - ProDOS system disk
- `test.nib` - NIB format test
- `test.woz` - WOZ format test

---

## Notes & Reminders

1. **Git Operations:** Use `git mv` for any file moves to preserve history
2. **Build Often:** Run build after each file addition to catch errors early
3. **Incremental Commits:** Commit after each phase for easy rollback
4. **VBlankOccurred Event:** Critical foundation - must be added first
5. **Remove DiskIIStatusDecorator:** This pattern is replaced by telemetry
6. **Test Coverage:** Don't skip Phase 7 - these are critical components

---

## Appendix: File Mapping Summary

| Source File | Target Location | Action |
|-------------|-----------------|--------|
| `IDiskImageProvider.cs` | `Interfaces/` | Copy |
| `IDiskIIDrive.cs` | `Interfaces/` | Copy |
| `IDiskImageFactory.cs` | `Interfaces/` | Copy |
| `IDiskIIFactory` (inline) | `Interfaces/IDiskIIFactory.cs` | Extract |
| `DiskIIControllerCards.cs` | `Cards/DiskIIControllerCard*.cs` | Split into 3 |
| `DiskIIDrive.cs` | `DiskII/` | Modify for telemetry |
| `DiskIIFactory.cs` | `DiskII/` | Simplify |
| `DiskIIDebugDecorator.cs` | `DiskII/` | Copy |
| `DiskIIStatusDecorator.cs` | N/A | **Do not copy** |
| `NullDiskIIDrive.cs` | `DiskII/` | Copy |
| `GcrEncoder.cs` | `DiskII/` | Copy |
| `DiskImageFactory.cs` | `DiskII/Providers/` | Copy |
| `NibDiskImageProvider.cs` | `DiskII/Providers/` | Copy |
| `SectorDiskImageProvider.cs` | `DiskII/Providers/` | Copy |
| `InternalWozDiskImageProvider.cs` | `DiskII/Providers/` | Copy |
| `WozDiskImageProvider.cs` | `DiskII/Providers/` | Copy |

---

*Document Created: 2025*  
*Last Updated: Phase 1.1 Complete - VBlankOccurred event added*
