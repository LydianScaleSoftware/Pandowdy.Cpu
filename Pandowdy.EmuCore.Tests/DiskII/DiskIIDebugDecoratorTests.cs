// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.EmuCore.DiskII;

namespace Pandowdy.EmuCore.Tests.DiskII;

/// <summary>
/// Tests for DiskIIDebugDecorator - debug logging decorator.
/// </summary>
public class DiskIIDebugDecoratorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullInner()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DiskIIDebugDecorator(null!));
    }

    [Fact]
    public void Constructor_WrapsInnerDrive()
    {
        // Arrange
        var inner = new NullDiskIIDrive("TestDrive");

        // Act
        var decorator = new DiskIIDebugDecorator(inner);

        // Assert
        Assert.Equal("TestDrive", decorator.Name);
    }

    #endregion

    #region Property Delegation Tests

    [Fact]
    public void Name_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive("MyDrive");
        var decorator = new DiskIIDebugDecorator(inner);

        // Assert
        Assert.Equal("MyDrive", decorator.Name);
    }

    [Fact]
    public void Track_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Assert - NullDiskIIDrive starts at track 17
        Assert.Equal(17.0, decorator.Track);
    }

    [Fact]
    public void QuarterTrack_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Assert - NullDiskIIDrive starts at quarter-track 68 (17 * 4)
        Assert.Equal(68, decorator.QuarterTrack);
    }

    // PHASE 5: MotorOn delegation tests removed - motor state is controller-level, not drive-level

    [Fact]
    public void HasDisk_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Assert - NullDiskIIDrive never has a disk
        Assert.False(decorator.HasDisk);
    }

    #endregion

    #region Method Delegation Tests

    [Fact]
    public void Reset_DelegatesToInner()
    {
        // Arrange - step to track 17.25
        var inner = new NullDiskIIDrive();
        // PHASE 5: Motor control is controller-level
        inner.StepToHigherTrack(); // Now at track 17.25
        var decorator = new DiskIIDebugDecorator(inner);

        // Act
        decorator.Reset();

        // Assert - head position preserved per interface contract
        Assert.Equal(17.25, inner.Track); // Head position NOT reset
    }

    [Fact]
    public void StepToHigherTrack_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        int initialQuarterTrack = inner.QuarterTrack;
        var decorator = new DiskIIDebugDecorator(inner);

        // Act
        decorator.StepToHigherTrack();

        // Assert
        Assert.Equal(initialQuarterTrack + 1, inner.QuarterTrack);
    }

    [Fact]
    public void StepToLowerTrack_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        int initialQuarterTrack = inner.QuarterTrack;
        var decorator = new DiskIIDebugDecorator(inner);

        // Act
        decorator.StepToLowerTrack();

        // Assert
        Assert.Equal(initialQuarterTrack - 1, inner.QuarterTrack);
    }

    [Fact]
    public void GetBit_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Act
        bool? bit = decorator.GetBit(1000);

        // Assert - NullDiskIIDrive always returns null
        Assert.Null(bit);
    }

    [Fact]
    public void SetBit_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Act
        bool result = decorator.SetBit(true);

        // Assert - NullDiskIIDrive always returns false
        Assert.False(result);
    }

    [Fact]
    public void IsWriteProtected_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Act
        bool result = decorator.IsWriteProtected();

        // Assert - NullDiskIIDrive always returns false
        Assert.False(result);
    }

    [Fact]
    public void InsertDisk_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Act - should not throw (NullDiskIIDrive ignores inserts)
        decorator.InsertDisk("test.dsk");

        // Assert - still no disk (NullDiskIIDrive ignores inserts)
        Assert.False(decorator.HasDisk);
    }

    [Fact]
    public void EjectDisk_DelegatesToInner()
    {
        // Arrange
        var inner = new NullDiskIIDrive();
        var decorator = new DiskIIDebugDecorator(inner);

        // Act - should not throw
        decorator.EjectDisk();

        // Assert - still no disk
        Assert.False(decorator.HasDisk);
    }

    #endregion

    #region Decorator Chain Tests

    [Fact]
    public void CanWrapAnotherDecorator()
    {
        // Arrange - create a chain of decorators
        var inner = new NullDiskIIDrive("InnerDrive");
        var decorator1 = new DiskIIDebugDecorator(inner);
        var decorator2 = new DiskIIDebugDecorator(decorator1);

        // Act & Assert - all operations should work through the chain
        Assert.Equal("InnerDrive", decorator2.Name);
        // PHASE 5: Motor control is controller-level, decorator only passes through mechanical operations
        decorator2.StepToHigherTrack();
        Assert.Equal(69, inner.QuarterTrack); // Verify delegation through chain
    }

    #endregion
}
