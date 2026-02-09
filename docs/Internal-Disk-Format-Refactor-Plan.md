**Goal:** Create a unified internal disk image format that all disk image providers convert to/from, simplifying the emulation layer and enabling a future Pandowdy project format.

**Status:** 🚧 IN PROGRESS - Phase 3 Complete ✅

**Current State:**
- Each disk format (WOZ, NIB, DSK/DO/PO) has its own provider implementation
- `WozDiskImageProvider` uses DiskArc's `CircularBitBuffer` via `INibbleDataAccess`
- `NibDiskImageProvider` uses native byte array implementation
- `SectorDiskImageProvider` synthesizes GCR tracks on-demand using DiskArc's `SectorCodec`
- Each provider has different internal representations and bit-access patterns
- No easy way to convert between formats or save modified disk images

**Problem:**
- Duplicated logic across providers for bit-level access and timing
- No common representation for disk state during emulation
- Write support requires format-specific export logic in each provider
- Future Pandowdy project format (.skillet) needs a canonical disk representation
- Testing is complicated by format-specific behavior differences

**Proposed Solution:**

Create a unified `InternalDiskImage` class that all formats convert to/from:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        External Disk Formats                        │
├─────────────┬─────────────┬─────────────┬─────────────┬─────────────┤
│    .woz     │    .nib     │    .dsk     │    .do      │    .po      │
└──────┬──────┴──────┬──────┴──────┬──────┴──────┬──────┴──────┬──────┘
       │             │             │             │             │
       ▼             ▼             ▼             ▼             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Format-Specific Importers                        │
│  WozImporter    NibImporter    SectorImporter (for .dsk/.do/.po)    │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                       InternalDiskImage                             │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │  CircularBitBuffer[] Tracks (35-40 tracks)                  │    │
│  │  int[] TrackBitCounts (variable per track for WOZ)          │    │
│  │  bool IsWriteProtected                                      │    │
│  │  byte OptimalBitTiming (WOZ timing info)                    │    │
│  │  bool IsDirty (modified since load)                         │    │
│  │  string? SourceFilePath                                     │    │
│  │  DiskFormat OriginalFormat                                  │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    UnifiedDiskImageProvider                         │
│  - Implements IDiskImageProvider                                    │
│  - Uses CircularBitBuffer for all bit-level operations              │
│  - Single implementation for GetBit/WriteBit/GetByte                │
│  - Consistent timing and position tracking                          │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    Format-Specific Exporters                        │
│  WozExporter    NibExporter    SectorExporter (decode GCR→sectors)  │
└──────┬─────────────┬─────────────┬─────────────┬─────────────┬──────┘
       │             │             │             │             │
       ▼             ▼             ▼             ▼             ▼
┌─────────────┬─────────────┬─────────────┬─────────────┬─────────────┐
│    .woz     │    .nib     │    .dsk     │    .do      │    .po      │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────────┘
```

**Key Classes:**

```csharp
/// <summary>
/// Unified internal representation of a 5.25" floppy disk.
/// All external formats convert to/from this format.
/// </summary>
public class InternalDiskImage
{
    /// <summary>Bit-level track data using DiskArc's CircularBitBuffer.</summary>
    public CircularBitBuffer[] Tracks { get; }

    /// <summary>Bit count per track (varies for WOZ, fixed 51200 for NIB/synthesized).</summary>
    public int[] TrackBitCounts { get; }

    /// <summary>Number of tracks (typically 35 for DOS 3.3, up to 40 for some disks).</summary>
    public int TrackCount => Tracks.Length;

    /// <summary>Write protection state.</summary>
    public bool IsWriteProtected { get; set; }

    /// <summary>Optimal bit timing in 125ns units (from WOZ, default 32 = 4µs).</summary>
    public byte OptimalBitTiming { get; init; } = 32;

    /// <summary>True if disk has been modified since load.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Original source file path (null for new/embedded disks).</summary>
    public string? SourceFilePath { get; init; }

    /// <summary>Original format this disk was imported from.</summary>
    public DiskFormat OriginalFormat { get; init; }

    /// <summary>Mark disk as modified.</summary>
    public void MarkDirty() => IsDirty = true;

    /// <summary>Clear dirty flag (after save).</summary>
    public void ClearDirty() => IsDirty = false;
}

public enum DiskFormat
{
    Unknown,
    Woz,        // .woz (WOZ 1.0/2.0)
    Nib,        // .nib (nibble)
    Dsk,        // .dsk (DOS order sectors)
    Do,         // .do (DOS order sectors, explicit)
    Po,         // .po (ProDOS order sectors)
    Internal    // Created programmatically or from Pandowdy project
}
```

**Importer/Exporter Interfaces:**

```csharp
public interface IDiskImageImporter
{
    /// <summary>Formats this importer can handle.</summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>Import a disk image file to internal format.</summary>
    InternalDiskImage Import(string filePath);

    /// <summary>Import from a stream (for embedded disk images).</summary>
    InternalDiskImage Import(Stream stream, DiskFormat format);
}

public interface IDiskImageExporter
{
    /// <summary>Format this exporter produces.</summary>
    DiskFormat OutputFormat { get; }

    /// <summary>Export internal format to file.</summary>
    void Export(InternalDiskImage disk, string filePath);

    /// <summary>Export to stream (for embedding in project files).</summary>
    void Export(InternalDiskImage disk, Stream stream);
}
```

**Implementation Phases:**

**Phase 1: Core Infrastructure** ✅ **COMPLETE**
- ✅ `InternalDiskImage` class - Unified internal format with CircularBitBuffer tracks
- ✅ `DiskFormat` enum - Format enumeration
- ✅ `IDiskImageImporter` and `IDiskImageExporter` interfaces
- ✅ `UnifiedDiskImageProvider` - Single provider implementation
- ✅ Unit tests (57 tests passing)

**Phase 2: Importers** ✅ **COMPLETE**
- ✅ `WozImporter`: Extract `CircularBitBuffer` tracks from WOZ via DiskArc
- ✅ `NibImporter`: Convert raw NIB bytes to `CircularBitBuffer` tracks
- ✅ `SectorImporter`: Synthesize GCR tracks from sector data
- ✅ Unit tests (31 tests passing)

**Phase 3: Single Provider** ✅ **COMPLETE**
- ✅ `DiskImageFactory` now uses importer pattern with `UnifiedDiskImageProvider`
- ✅ All format-specific providers replaced with unified provider
- ✅ Backward compatible - existing code unchanged
- ✅ All tests passing (867 tests)

**Phase 4: Exporters (Write Support)** ⏳ **NEXT**
- `WozExporter`: Write internal format back to WOZ (preserve metadata)
- `NibExporter`: Write raw NIB format
- `SectorExporter`: Decode GCR to sectors, write DSK/DO/PO (lossy for copy-protected disks)

**Phase 5: Pandowdy Project Format** -- DEFERRED.  DO NOT IMPLEMENT YET!!
- Define `.pdw` project file format (JSON/binary container)
- Embed `InternalDiskImage` data in project files
- Support disk image versioning within projects
- Import external disk images into project
- Export embedded disks to standard formats

**Files Created (Phase 1):**

*Core:* ✅
- ✅ `Pandowdy.EmuCore\DiskII\InternalDiskImage.cs` - Unified internal format
- ✅ `Pandowdy.EmuCore\DiskII\DiskFormat.cs` - Format enumeration
- ✅ `Pandowdy.EmuCore\DiskII\IDiskImageImporter.cs` - Importer interface
- ✅ `Pandowdy.EmuCore\DiskII\IDiskImageExporter.cs` - Exporter interface
- ✅ `Pandowdy.EmuCore\DiskII\Providers\UnifiedDiskImageProvider.cs` - Single provider implementation

*Tests:* ✅
- ✅ `Pandowdy.EmuCore.Tests\DiskII\InternalDiskImageTests.cs` - 29 passing tests
- ✅ `Pandowdy.EmuCore.Tests\DiskII\Providers\UnifiedDiskImageProviderTests.cs` - 28 passing tests

**Files Created (Phase 2):**

*Importers:* ✅
- ✅ `Pandowdy.EmuCore\DiskII\Importers\WozImporter.cs` - WOZ format importer
- ✅ `Pandowdy.EmuCore\DiskII\Importers\NibImporter.cs` - NIB format importer
- ✅ `Pandowdy.EmuCore\DiskII\Importers\SectorImporter.cs` - Sector format importer (DSK/DO/PO)

*Tests:* ✅
- ✅ `Pandowdy.EmuCore.Tests\DiskII\Importers\WozImporterTests.cs` - 8 passing tests
- ✅ `Pandowdy.EmuCore.Tests\DiskII\Importers\NibImporterTests.cs` - 14 passing tests
- ✅ `Pandowdy.EmuCore.Tests\DiskII\Importers\SectorImporterTests.cs` - 9 passing tests

**Files Modified (Phase 3):**

*Factory Integration:* ✅
- ✅ `Pandowdy.EmuCore\DiskII\Providers\DiskImageFactory.cs` - Now uses importer pattern
- ✅ `Pandowdy.EmuCore.Tests\DiskII\Providers\DiskImageFactoryTests.cs` - Updated to expect `UnifiedDiskImageProvider`

**Files Ready for Removal (Phase 3):**

*Old Providers (No longer used):* 📦
- 📦 `Pandowdy.EmuCore\DiskII\Providers\WozDiskImageProvider.cs` - Replaced by WozImporter + UnifiedDiskImageProvider
- 📦 `Pandowdy.EmuCore\DiskII\Providers\NibDiskImageProvider.cs` - Replaced by NibImporter + UnifiedDiskImageProvider
- 📦 `Pandowdy.EmuCore\DiskII\Providers\SectorDiskImageProvider.cs` - Replaced by SectorImporter + UnifiedDiskImageProvider
- 📦 `Pandowdy.EmuCore\DiskII\GcrEncoder.cs` - Functionality moved to `SectorImporter`

*Note: Old providers kept temporarily for reference but can be safely removed.*

**Files to Create (Future Phases):**

*Exporters:*
- `Pandowdy.EmuCore\DiskII\Exporters\WozExporter.cs`
- `Pandowdy.EmuCore\DiskII\Exporters\NibExporter.cs`
- `Pandowdy.EmuCore\DiskII\Exporters\SectorExporter.cs`

*Tests:*
- `Pandowdy.EmuCore.Tests\DiskII\Exporters\*ExporterTests.cs`

**Current Status Summary:**

✅ **Phase 1 Complete (2026-01):**
- Core infrastructure implemented and tested
- 57 unit tests created and passing
- UnifiedDiskImageProvider fully functional

✅ **Phase 2 Complete (2026-01):**
- WozImporter, NibImporter, SectorImporter implemented
- All importers convert external formats to InternalDiskImage
- 31 unit tests created and passing
- Backward compatible with existing disk images

✅ **Phase 3 Complete (2026-01):**
- DiskImageFactory refactored to use importer + unified provider pattern
- All format-specific providers (Woz/Nib/Sector) replaced
- Complete backward compatibility maintained
- All 867 tests passing

⏳ **Next: Phase 4 - Exporters**
- Implement WozExporter, NibExporter, SectorExporter
- Enable write support for modified disk images
- Format conversion capability (import any, export any)

**Benefits:**
- ✅ Single `IDiskImageProvider` implementation to maintain
- ✅ Guaranteed consistent behavior across all formats
- ✅ Easier testing - test the single provider, test importers/exporters separately
- ✅ Write support becomes format-agnostic (modify internal, export to any format)
- ✅ Enables Pandowdy project format with embedded disk images
- ✅ Import from any format, export to any format (format conversion)
- ✅ CircularBitBuffer provides battle-tested bit-level operations from DiskArc
- ✅ Copy-protected disks preserved in internal format (bit-perfect tracks)

**Technical Considerations:**
- `CircularBitBuffer` is from `CommonUtil` (DiskArc dependency already exists)
- WOZ track lengths vary; internal format preserves per-track bit counts
- Sector export is lossy for copy-protected disks (GCR → sector decode may fail)
- Internal format is transient (not serialized directly, use exporters)
- Future `.pdw` format will need versioning for internal format changes

**Priority:** Medium

**Dependencies:**
- None (builds on existing DiskArc integration)



---
