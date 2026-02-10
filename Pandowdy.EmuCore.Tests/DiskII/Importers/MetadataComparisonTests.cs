// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.EmuCore.DiskII;
using Pandowdy.EmuCore.DiskII.Importers;
using Pandowdy.EmuCore.DiskII.Providers;
using Xunit;
using Xunit.Abstractions;

namespace Pandowdy.EmuCore.Tests.DiskII.Importers;

/// <summary>
/// Tests to compare metadata between new importer and legacy provider.
/// </summary>
public class MetadataComparisonTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void CompareAllMetadata_NewVsLegacy()
    {

        // Use temp copy to avoid file locking conflicts with parallel tests
        using var sourceCopy = TempDiskImageCopy.TryCreate(TestDiskImages.TestDo);
        if (sourceCopy == null)
        {
            _output.WriteLine("test.do not found");
            return;
        }

        _output.WriteLine("=== Comparing All Metadata ===");
        _output.WriteLine("");

        // New importer + wrapper
        var importer = new SectorImporter();

        InternalDiskImage newImage = importer.Import(sourceCopy.FilePath);
        var newProvider = new UnifiedDiskImageProvider(newImage);

        // Legacy provider
        using var legacyProvider = new SectorDiskImageProvider(sourceCopy.FilePath);

        _output.WriteLine("Metadata Comparison:");
        _output.WriteLine($"  OptimalBitTiming:");
        _output.WriteLine($"    New:    {newProvider.OptimalBitTiming}");
        _output.WriteLine($"    Legacy: {legacyProvider.OptimalBitTiming}");
        _output.WriteLine("");

        _output.WriteLine($"  IsWriteProtected:");
        _output.WriteLine($"    New:    {newProvider.IsWriteProtected}");
        _output.WriteLine($"    Legacy: {legacyProvider.IsWriteProtected}");
        _output.WriteLine("");

        _output.WriteLine($"  IsWritable:");
        _output.WriteLine($"    New:    {newProvider.IsWritable}");
        _output.WriteLine($"    Legacy: {legacyProvider.IsWritable}");
        _output.WriteLine("");

        _output.WriteLine($"  FilePath:");
        _output.WriteLine($"    New:    {newProvider.FilePath}");
        _output.WriteLine($"    Legacy: {legacyProvider.FilePath}");
        _output.WriteLine("");

        // Check a specific track's bit count
        newProvider.SetQuarterTrack(0);
        legacyProvider.SetQuarterTrack(0);

        _output.WriteLine($"  CurrentTrackBitCount (Track 0):");
        _output.WriteLine($"    New:    {newProvider.CurrentTrackBitCount}");
        _output.WriteLine($"    Legacy: {legacyProvider.CurrentTrackBitCount}");
    }
}
