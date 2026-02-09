// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using CommonUtil;
using Pandowdy.EmuCore.DiskII;
using Xunit;

namespace Pandowdy.EmuCore.Tests.DiskII;

/// <summary>
/// Unit tests for the InternalDiskImage class.
/// </summary>
public class InternalDiskImageTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultParameters_Creates35TrackDisk()
    {
        // Act
        var disk = new InternalDiskImage();

        // Assert
        Assert.Equal(35, disk.TrackCount);
        Assert.NotNull(disk.Tracks);
        Assert.NotNull(disk.TrackBitCounts);
        Assert.Equal(35, disk.Tracks.Length);
        Assert.Equal(35, disk.TrackBitCounts.Length);
    }

    [Fact]
    public void Constructor_DefaultParameters_SetsStandardBitCount()
    {
        // Act
        var disk = new InternalDiskImage();

        // Assert
        Assert.All(disk.TrackBitCounts, bitCount => Assert.Equal(51200, bitCount));
    }

    [Fact]
    public void Constructor_CustomTrackCount_CreatesCorrectNumberOfTracks()
    {
        // Act
        var disk = new InternalDiskImage(trackCount: 40);

        // Assert
        Assert.Equal(40, disk.TrackCount);
        Assert.Equal(40, disk.Tracks.Length);
        Assert.Equal(40, disk.TrackBitCounts.Length);
    }

    [Fact]
    public void Constructor_CustomBitCount_SetsAllTracksToCustomBitCount()
    {
        // Arrange
        const int customBitCount = 50000;

        // Act
        var disk = new InternalDiskImage(trackCount: 35, standardTrackBitCount: customBitCount);

        // Assert
        Assert.All(disk.TrackBitCounts, bitCount => Assert.Equal(customBitCount, bitCount));
    }

    [Fact]
    public void Constructor_InvalidTrackCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new InternalDiskImage(trackCount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InternalDiskImage(trackCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InternalDiskImage(trackCount: 41));
    }

    [Fact]
    public void Constructor_InvalidBitCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new InternalDiskImage(trackCount: 35, standardTrackBitCount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InternalDiskImage(trackCount: 35, standardTrackBitCount: -1));
    }

    [Fact]
    public void Constructor_WithPreAllocatedTracks_StoresTracksAndBitCounts()
    {
        // Arrange
        var tracks = new CircularBitBuffer[35];
        var bitCounts = new int[35];
        for (int i = 0; i < 35; i++)
        {
            byte[] trackData = new byte[6400]; // 51200 bits = 6400 bytes
            tracks[i] = new CircularBitBuffer(
                trackData,
                byteOffset: 0,
                bitOffset: 0,
                bitCount: 51200,
                new GroupBool(),
                isReadOnly: false
            );
            bitCounts[i] = 51200;
        }

        // Act
        var disk = new InternalDiskImage(tracks, bitCounts);

        // Assert
        Assert.Same(tracks, disk.Tracks);
        Assert.Same(bitCounts, disk.TrackBitCounts);
        Assert.Equal(35, disk.TrackCount);
    }

    [Fact]
    public void Constructor_WithMismatchedArrayLengths_ThrowsArgumentException()
    {
        // Arrange
        var tracks = new CircularBitBuffer[35];
        var bitCounts = new int[36]; // Mismatched length

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new InternalDiskImage(tracks, bitCounts));
    }

    [Fact]
    public void Constructor_WithEmptyArrays_ThrowsArgumentException()
    {
        // Arrange
        var tracks = Array.Empty<CircularBitBuffer>();
        var bitCounts = Array.Empty<int>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new InternalDiskImage(tracks, bitCounts));
    }

    #endregion

    #region Property Tests

    [Fact]
    public void IsWriteProtected_DefaultValue_IsFalse()
    {
        // Arrange
        var disk = new InternalDiskImage();

        // Act & Assert
        Assert.False(disk.IsWriteProtected);
    }

    [Fact]
    public void IsWriteProtected_CanBeSetAndGet()
    {
        // Arrange
        var disk = new InternalDiskImage();

        // Act
        disk.IsWriteProtected = true;

        // Assert
        Assert.True(disk.IsWriteProtected);

        // Act
        disk.IsWriteProtected = false;

        // Assert
        Assert.False(disk.IsWriteProtected);
    }

    [Fact]
    public void OptimalBitTiming_DefaultValue_Is32()
    {
        // Arrange & Act
        var disk = new InternalDiskImage();

        // Assert
        Assert.Equal(32, disk.OptimalBitTiming);
    }

    [Fact]
    public void OptimalBitTiming_CanBeSetViaInitializer()
    {
        // Act
        var disk = new InternalDiskImage { OptimalBitTiming = 31 };

        // Assert
        Assert.Equal(31, disk.OptimalBitTiming);
    }

    [Fact]
    public void IsDirty_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var disk = new InternalDiskImage();

        // Assert
        Assert.False(disk.IsDirty);
    }

    [Fact]
    public void SourceFilePath_DefaultValue_IsNull()
    {
        // Arrange & Act
        var disk = new InternalDiskImage();

        // Assert
        Assert.Null(disk.SourceFilePath);
    }

    [Fact]
    public void SourceFilePath_CanBeSetViaInitializer()
    {
        // Act
        var disk = new InternalDiskImage { SourceFilePath = "test.dsk" };

        // Assert
        Assert.Equal("test.dsk", disk.SourceFilePath);
    }

    [Fact]
    public void OriginalFormat_DefaultValue_IsUnknown()
    {
        // Arrange & Act
        var disk = new InternalDiskImage();

        // Assert
        Assert.Equal(DiskFormat.Unknown, disk.OriginalFormat);
    }

    [Fact]
    public void OriginalFormat_CanBeSetViaInitializer()
    {
        // Act
        var disk = new InternalDiskImage { OriginalFormat = DiskFormat.Woz };

        // Assert
        Assert.Equal(DiskFormat.Woz, disk.OriginalFormat);
    }

    #endregion

    #region Dirty Tracking Tests

    [Fact]
    public void MarkDirty_SetsDirtyFlagToTrue()
    {
        // Arrange
        var disk = new InternalDiskImage();
        Assert.False(disk.IsDirty); // Precondition

        // Act
        disk.MarkDirty();

        // Assert
        Assert.True(disk.IsDirty);
    }

    [Fact]
    public void MarkDirty_CalledMultipleTimes_RemainsDirty()
    {
        // Arrange
        var disk = new InternalDiskImage();

        // Act
        disk.MarkDirty();
        disk.MarkDirty();
        disk.MarkDirty();

        // Assert
        Assert.True(disk.IsDirty);
    }

    [Fact]
    public void ClearDirty_SetsDirtyFlagToFalse()
    {
        // Arrange
        var disk = new InternalDiskImage();
        disk.MarkDirty();
        Assert.True(disk.IsDirty); // Precondition

        // Act
        disk.ClearDirty();

        // Assert
        Assert.False(disk.IsDirty);
    }

    [Fact]
    public void ClearDirty_WhenNotDirty_RemainsFalse()
    {
        // Arrange
        var disk = new InternalDiskImage();
        Assert.False(disk.IsDirty); // Precondition

        // Act
        disk.ClearDirty();

        // Assert
        Assert.False(disk.IsDirty);
    }

    [Fact]
    public void DirtyFlag_CanBeToggledMultipleTimes()
    {
        // Arrange
        var disk = new InternalDiskImage();

        // Act & Assert - Cycle through dirty states
        Assert.False(disk.IsDirty);

        disk.MarkDirty();
        Assert.True(disk.IsDirty);

        disk.ClearDirty();
        Assert.False(disk.IsDirty);

        disk.MarkDirty();
        Assert.True(disk.IsDirty);

        disk.ClearDirty();
        Assert.False(disk.IsDirty);
    }

    #endregion

    #region Track Access Tests

    [Fact]
    public void Tracks_CanBeAccessedByIndex()
    {
        // Arrange
        var disk = new InternalDiskImage();

        // Act
        var track0 = disk.Tracks[0];
        var track34 = disk.Tracks[34];

        // Assert
        Assert.NotNull(track0);
        Assert.NotNull(track34);
    }

    [Fact]
    public void TrackBitCounts_CanBeAccessedByIndex()
    {
        // Arrange
        var disk = new InternalDiskImage();

        // Act
        var bitCount0 = disk.TrackBitCounts[0];
        var bitCount34 = disk.TrackBitCounts[34];

        // Assert
        Assert.Equal(51200, bitCount0);
        Assert.Equal(51200, bitCount34);
    }

    [Fact]
    public void TrackBitCounts_CanBeModified()
    {
        // Arrange
        var disk = new InternalDiskImage();

        // Act
        disk.TrackBitCounts[0] = 50000;
        disk.TrackBitCounts[34] = 52000;

        // Assert
        Assert.Equal(50000, disk.TrackBitCounts[0]);
        Assert.Equal(52000, disk.TrackBitCounts[34]);
    }

    [Fact]
    public void Tracks_CircularBitBuffers_CanReadAndWriteBits()
    {
        // Arrange
        var disk = new InternalDiskImage();
        var track0 = disk.Tracks[0];

        // Act - Write pattern to track 0
        track0.BitPosition = 0;
        for (int i = 0; i < 100; i++)
        {
            track0.WriteBit(i % 2); // Alternating 0,1,0,1...
        }

        // Read back pattern
        track0.BitPosition = 0;
        var readBits = new List<byte>();
        for (int i = 0; i < 100; i++)
        {
            readBits.Add(track0.ReadNextBit());
        }

        // Assert
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal((byte)(i % 2), readBits[i]);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CompleteWorkflow_CreateModifySaveReload()
    {
        // Arrange - Create disk with custom properties
        var originalDisk = new InternalDiskImage
        {
            SourceFilePath = "test.woz",
            OriginalFormat = DiskFormat.Woz,
            OptimalBitTiming = 31,
            IsWriteProtected = false
        };

        // Act - Modify disk
        originalDisk.Tracks[0].BitPosition = 0;
        originalDisk.Tracks[0].WriteBit(1);
        originalDisk.MarkDirty();

        // Assert - Verify state
        Assert.True(originalDisk.IsDirty);
        Assert.Equal("test.woz", originalDisk.SourceFilePath);
        Assert.Equal(DiskFormat.Woz, originalDisk.OriginalFormat);
        Assert.Equal(31, originalDisk.OptimalBitTiming);

        // Act - Simulate save (clear dirty)
        originalDisk.ClearDirty();

        // Assert - Verify saved state
        Assert.False(originalDisk.IsDirty);
    }

    [Fact]
    public void VariableTrackLengths_WozStyle_WorksCorrectly()
    {
        // Arrange - Create disk with variable track lengths (like WOZ format)
        var tracks = new CircularBitBuffer[35];
        var bitCounts = new int[35];
        for (int i = 0; i < 35; i++)
        {
            // Vary bit counts slightly (50000-52000 bits, typical for WOZ)
            bitCounts[i] = 50000 + (i * 50);
            int byteCount = (bitCounts[i] + 7) / 8; // Round up to nearest byte
            byte[] trackData = new byte[byteCount];
            tracks[i] = new CircularBitBuffer(
                trackData,
                byteOffset: 0,
                bitOffset: 0,
                bitCount: bitCounts[i],
                new GroupBool(),
                isReadOnly: false
            );
        }

        // Act
        var disk = new InternalDiskImage(tracks, bitCounts)
        {
            OriginalFormat = DiskFormat.Woz,
            OptimalBitTiming = 32
        };

        // Assert
        Assert.Equal(35, disk.TrackCount);
        for (int i = 0; i < 35; i++)
        {
            Assert.Equal(50000 + (i * 50), disk.TrackBitCounts[i]);
        }
    }

    #endregion
}
