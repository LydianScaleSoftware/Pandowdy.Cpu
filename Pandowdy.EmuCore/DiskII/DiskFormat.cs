// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.EmuCore.DiskII;

/// <summary>
/// Enumeration of supported disk image formats.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration identifies the format of a disk image, both for external files
/// and internal representations. All external formats are converted to an internal
/// unified format (<see cref="Internal"/>) during import.
/// </para>
/// </remarks>
public enum DiskFormat
{
    /// <summary>
    /// Format is unknown or not yet determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// WOZ format (.woz) - stores raw flux transitions with timing information.
    /// Most accurate format, supports variable-length tracks and copy protection.
    /// </summary>
    Woz,

    /// <summary>
    /// NIB format (.nib) - stores raw 6-and-2 GCR-encoded nibble data.
    /// Fixed track lengths (6656 bytes per track), no timing information.
    /// </summary>
    Nib,

    /// <summary>
    /// DSK format (.dsk) - sector-based format with DOS 3.3 sector ordering.
    /// 35 tracks, 16 sectors per track, 256 bytes per sector.
    /// </summary>
    Dsk,

    /// <summary>
    /// DO format (.do) - sector-based format with explicit DOS ordering.
    /// Identical to DSK but with explicit file extension.
    /// </summary>
    Do,

    /// <summary>
    /// PO format (.po) - sector-based format with ProDOS sector ordering.
    /// 35 tracks, 16 sectors per track, 256 bytes per sector.
    /// </summary>
    Po,

    /// <summary>
    /// Internal unified format - the canonical representation used during emulation.
    /// All external formats are converted to this format on import.
    /// Created programmatically or from Pandowdy project files.
    /// </summary>
    Internal
}
