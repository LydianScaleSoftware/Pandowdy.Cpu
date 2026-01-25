using Pandowdy.EmuCore.DataTypes;

namespace Pandowdy.EmuCore.Tests;

/// <summary>
/// Tests for CpuClockingCounters, focusing on VBlank timing and the VBlankOccurred event.
/// </summary>
public class CpuClockingCountersTests
{
    #region VBlankOccurred Event Tests

    [Fact]
    public void VBlankOccurred_DoesNotFire_BeforeVBlankCycle()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        var eventCount = 0;
        counters.VBlankOccurred += () => eventCount++;

        // Act - Increment cycles but stay below VBlankStartCycle (12,480)
        for (int i = 0; i < 12_000; i++)
        {
            counters.IncrementCycles(1);
            counters.CheckAndAdvanceVBlank();
        }

        // Assert
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void VBlankOccurred_Fires_WhenVBlankCycleReached()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        var eventCount = 0;
        counters.VBlankOccurred += () => eventCount++;

        // Act - Increment to just past VBlankStartCycle (12,480)
        for (int i = 0; i < CpuClockingCounters.VBlankStartCycle + 1; i++)
        {
            counters.IncrementCycles(1);
            counters.CheckAndAdvanceVBlank();
        }

        // Assert
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void VBlankOccurred_FiresOncePerFrame()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        var eventCount = 0;
        counters.VBlankOccurred += () => eventCount++;

        // Act - Run through 3 complete frames (17,030 cycles each)
        var totalCycles = CpuClockingCounters.CyclesPerVBlank * 3;
        for (int i = 0; i < totalCycles; i++)
        {
            counters.IncrementCycles(1);
            counters.CheckAndAdvanceVBlank();
        }

        // Assert - Should have fired 3 times (once per frame)
        Assert.Equal(3, eventCount);
    }

    [Fact]
    public void VBlankOccurred_FiresOnlyOnce_WhenMultipleFramesSkipped()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        var eventCount = 0;
        counters.VBlankOccurred += () => eventCount++;

        // Act - Simulate fast unthrottled mode: increment many cycles at once
        // This simulates 5 frames worth of cycles in one batch
        counters.IncrementCycles(CpuClockingCounters.CyclesPerVBlank * 5);
        counters.CheckAndAdvanceVBlank();

        // Assert - Should only fire once (catch-up logic, not spam)
        Assert.Equal(1, eventCount);
    }

    [Fact]
    public void VBlankOccurred_NoSubscribers_DoesNotThrow()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        // No subscribers attached

        // Act - Increment past VBlank threshold
        counters.IncrementCycles(CpuClockingCounters.VBlankStartCycle + 1);
        
        // Assert - Should not throw when invoking null event
        var exception = Record.Exception(() => counters.CheckAndAdvanceVBlank());
        Assert.Null(exception);
    }

    [Fact]
    public void CheckAndAdvanceVBlank_ReturnsFalse_BeforeVBlankCycle()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        counters.IncrementCycles(100); // Well before VBlankStartCycle

        // Act
        var result = counters.CheckAndAdvanceVBlank();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CheckAndAdvanceVBlank_ReturnsTrue_WhenVBlankCycleReached()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        counters.IncrementCycles(CpuClockingCounters.VBlankStartCycle + 1);

        // Act
        var result = counters.CheckAndAdvanceVBlank();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CheckAndAdvanceVBlank_AdvancesNextVBlankCycle()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        var initialNextVBlank = counters.NextVBlankCycle;
        counters.IncrementCycles(CpuClockingCounters.VBlankStartCycle + 1);

        // Act
        counters.CheckAndAdvanceVBlank();

        // Assert - NextVBlankCycle should have advanced by CyclesPerVBlank
        Assert.Equal(initialNextVBlank + CpuClockingCounters.CyclesPerVBlank, counters.NextVBlankCycle);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsEventSubscription_DoesNotAffectSubscribers()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        var eventCount = 0;
        counters.VBlankOccurred += () => eventCount++;
        
        // Increment past VBlank once
        counters.IncrementCycles(CpuClockingCounters.VBlankStartCycle + 1);
        counters.CheckAndAdvanceVBlank();
        Assert.Equal(1, eventCount);

        // Act - Reset counters
        counters.Reset();

        // Increment past VBlank again
        counters.IncrementCycles(CpuClockingCounters.VBlankStartCycle + 1);
        counters.CheckAndAdvanceVBlank();

        // Assert - Subscriber should still be attached and fire again
        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void Reset_ResetsNextVBlankCycle_ToVBlankStartCycle()
    {
        // Arrange
        var counters = new CpuClockingCounters();
        counters.IncrementCycles(CpuClockingCounters.CyclesPerVBlank * 10);
        counters.CheckAndAdvanceVBlank();

        // Act
        counters.Reset();

        // Assert
        Assert.Equal((ulong)CpuClockingCounters.VBlankStartCycle, counters.NextVBlankCycle);
    }

    #endregion
}
