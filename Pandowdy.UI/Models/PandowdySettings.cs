// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.UI.Models;

/// <summary>
/// Application settings model for Pandowdy emulator.
/// </summary>
public class PandowdySettings
{
    /// <summary>
    /// Settings file format version for compatibility tracking.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Last directory used for exporting disk images.
    /// </summary>
    public string? LastExportDirectory { get; set; }

    /// <summary>
    /// Width of the disk status panel in pixels.
    /// </summary>
    public double DiskPanelWidth { get; set; } = 300.0;
}
