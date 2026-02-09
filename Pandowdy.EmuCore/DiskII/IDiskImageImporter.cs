// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.EmuCore.DiskII;

/// <summary>
/// Interface for importing external disk image formats to the unified internal format.
/// </summary>
/// <remarks>
/// <para>
/// Importers convert format-specific disk images (WOZ, NIB, DSK/DO/PO) into the
/// canonical <see cref="InternalDiskImage"/> representation. This allows the emulator
/// to work with a single unified format, while supporting multiple external formats.
/// </para>
/// <para>
/// <strong>Implementation Pattern:</strong><br/>
/// Each importer handles one or more related formats (e.g., DSK/DO/PO are all
/// sector-based formats and can share an importer). Importers are stateless and
/// can be reused to import multiple disk images.
/// </para>
/// </remarks>
public interface IDiskImageImporter
{
    /// <summary>
    /// File extensions this importer can handle (e.g., [".woz", ".woz.gz"]).
    /// </summary>
    IReadOnlyList<string> SupportedExtensions { get; }

    /// <summary>
    /// Import a disk image file to internal format.
    /// </summary>
    /// <param name="filePath">Path to the disk image file.</param>
    /// <returns>Internal disk image representation.</returns>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist.</exception>
    /// <exception cref="InvalidDataException">Thrown when file format is invalid.</exception>
    InternalDiskImage Import(string filePath);

    /// <summary>
    /// Import from a stream (for embedded disk images).
    /// </summary>
    /// <param name="stream">Stream containing disk image data.</param>
    /// <param name="format">Format of the disk image in the stream.</param>
    /// <returns>Internal disk image representation.</returns>
    /// <exception cref="InvalidDataException">Thrown when stream format is invalid.</exception>
    InternalDiskImage Import(Stream stream, DiskFormat format);
}
