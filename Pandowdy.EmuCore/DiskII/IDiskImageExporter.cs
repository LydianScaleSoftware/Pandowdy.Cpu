// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.EmuCore.DiskII;

/// <summary>
/// Interface for exporting the unified internal format to external disk image formats.
/// </summary>
/// <remarks>
/// <para>
/// Exporters convert the canonical <see cref="InternalDiskImage"/> representation
/// back to format-specific disk images (WOZ, NIB, DSK/DO/PO). This enables:
/// <list type="bullet">
/// <item>Saving modified disk images back to their original format</item>
/// <item>Converting between different disk image formats</item>
/// <item>Embedding disk images in Pandowdy project files</item>
/// </list>
/// </para>
/// <para>
/// <strong>Lossy Exports:</strong><br/>
/// Some exports are lossy. For example, exporting a copy-protected WOZ disk to
/// DSK format will only work if the GCR data can be successfully decoded to sectors.
/// Many copy-protected disks cannot be exported to sector-based formats.
/// </para>
/// </remarks>
public interface IDiskImageExporter
{
    /// <summary>
    /// Format this exporter produces.
    /// </summary>
    DiskFormat OutputFormat { get; }

    /// <summary>
    /// Export internal format to file.
    /// </summary>
    /// <param name="disk">Internal disk image to export.</param>
    /// <param name="filePath">Path to write the disk image file.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the disk cannot be exported to this format (e.g., copy-protected disk to DSK).
    /// </exception>
    /// <exception cref="IOException">Thrown when file write fails.</exception>
    void Export(InternalDiskImage disk, string filePath);

    /// <summary>
    /// Export to stream (for embedding in project files).
    /// </summary>
    /// <param name="disk">Internal disk image to export.</param>
    /// <param name="stream">Stream to write the disk image data.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the disk cannot be exported to this format (e.g., copy-protected disk to DSK).
    /// </exception>
    /// <exception cref="IOException">Thrown when stream write fails.</exception>
    void Export(InternalDiskImage disk, Stream stream);
}
