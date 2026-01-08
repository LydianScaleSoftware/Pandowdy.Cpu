using Pandowdy.EmuCore.Services;
using Pandowdy.EmuCore.Interfaces;
using Pandowdy.EmuCore.Tests.Helpers;

namespace Pandowdy.EmuCore.Tests;

/// <summary>
/// Unit tests for VA2M emulator orchestration layer.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Test Scope:</strong> VA2M is responsible for command queueing, thread marshaling,
/// and orchestration - NOT for keyboard protocol implementation (strobe bits, etc.).
/// </para>
/// <para>
/// <strong>What VA2M Does:</strong>
/// <list type="bullet">
/// <item>Enqueue commands for cross-thread execution (UI → emulator thread)</item>
/// <item>Process pending commands at instruction boundaries</item>
/// <item>Pass commands to Bus unchanged (no transformation)</item>
/// <item>Coordinate Clock(), Reset(), and state publishing</item>
/// </list>
/// </para>
/// <para>
/// <strong>What VA2M Does NOT Do:</strong>
/// Keyboard protocol (strobe bit setting) - tested in <see cref="SingularKeyHandlerTests"/>
/// </para>
/// </remarks>
public class VA2MTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        // Arrange & Act
        var va2m = VA2MTestHelpers.CreateBuilder().Build();

        // Assert
        Assert.NotNull(va2m);
        Assert.NotNull(va2m.MemoryPool);
        Assert.NotNull(va2m.Bus);
    }

    [Fact]
    public void Constructor_WithNullEmulatorState_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VA2MTestHelpers.CreateBuilder()
                .WithEmulatorState(null!)
                .Build());
    }

    [Fact]
    public void Constructor_WithNullFrameProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VA2MTestHelpers.CreateBuilder()
                .WithFrameProvider(null!)
                .Build());
    }

    [Fact]
    public void Constructor_WithNullSystemStatusProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VA2MTestHelpers.CreateBuilder()
                .WithSystemStatusProvider(null!)
                .Build());
    }

    [Fact]
    public void Constructor_WithNullBus_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VA2MTestHelpers.CreateBuilder()
                .WithBus(null!)
                .Build());
    }

    [Fact]
    public void Constructor_WithNullMemoryPool_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VA2MTestHelpers.CreateBuilder()
                .WithMemoryPool(null!)
                .Build());
    }

    [Fact]
    public void Constructor_WithNullFrameGenerator_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            VA2MTestHelpers.CreateBuilder()
                .WithFrameGenerator(null!)
                .Build());
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ThrottleEnabled_DefaultsToTrue()
    {
        // Arrange
        var va2m = VA2MTestHelpers.CreateBuilder().Build();

        // Act & Assert
        Assert.True(va2m.ThrottleEnabled);
    }

    [Fact]
    public void ThrottleEnabled_CanBeSetToFalse()
    {
        // Arrange
        var va2m = VA2MTestHelpers.CreateBuilder().Build();

        // Act
        va2m.ThrottleEnabled = false;

        // Assert
        Assert.False(va2m.ThrottleEnabled);
    }

    [Fact]
    public void TargetHz_DefaultsTo1023000()
    {
        // Arrange
        var va2m = VA2MTestHelpers.CreateBuilder().Build();

        // Act & Assert
        Assert.Equal(1_023_000d, va2m.TargetHz);
    }

    [Fact]
    public void TargetHz_CanBeChanged()
    {
        // Arrange
        var va2m = VA2MTestHelpers.CreateBuilder().Build();

        // Act
        va2m.TargetHz = 2_000_000d;

        // Assert
        Assert.Equal(2_000_000d, va2m.TargetHz);
    }

    [Fact]
    public void SystemClock_ReflectsBusSystemClockCounter()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act
        testBus.Clock();
        testBus.Clock();
        testBus.Clock();

        // Assert
        Assert.Equal(3UL, va2m.SystemClock);
    }

    #endregion

    #region Clock and Reset Tests

    [Fact]
    public void Clock_IncrementsBusClock()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        var initialClock = va2m.SystemClock;

        // Act
        va2m.Clock();

        // Assert
        Assert.Equal(initialClock + 1, va2m.SystemClock);
    }

    [Fact]
    public void Clock_PublishesStateToEmulatorState()
    {
        // Arrange
        var testState = new TestEmulatorState();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithEmulatorState(testState)
            .Build();

        var initialCount = testState.UpdateCount;

        // Act
        va2m.Clock();

        // Assert
        Assert.True(testState.UpdateCount > initialCount, "State should be updated after Clock()");
    }

    [Fact]
    public void Reset_CallsBusReset()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act
        va2m.Reset();
        va2m.Clock(); // Process pending queue

        // Assert
        Assert.Equal(1, testBus.ResetCount);
    }

    [Fact]
    public void Reset_ResetsBusClockToZero()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Advance clock
        va2m.Clock();
        va2m.Clock();
        va2m.Clock();
        Assert.Equal(3UL, va2m.SystemClock); // Verify we're at 3 cycles

        // Act
        va2m.Reset(); // Enqueues reset operation
        
        // Process pending queue - this will execute the reset AND increment bus clock by 1
        // After reset executes: SystemClock = 0
        // After Bus.Clock() in va2m.Clock(): SystemClock = 1
        va2m.Clock();

        // Assert - Clock is at 1 because va2m.Clock() both processes reset and increments
        Assert.Equal(1UL, va2m.SystemClock);
    }

    #endregion

    #region Command Queueing Tests - EnqueueKey

    [Fact]
    public void EnqueueKey_PassesValueUnchangedToBus()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - VA2M should pass the value to Bus unchanged
        va2m.EnqueueKey(0x41);
        va2m.Clock(); // Process pending queue

        // Assert - VA2M doesn't modify the value (no strobe bit handling at this layer)
        // The Bus (and ultimately SingularKeyHandler) handles strobe bit setting
        Assert.Equal(0x41, testBus.GetLastEnqueuedKey());
    }

    [Fact]
    public void EnqueueKey_MultipleCommands_ExecutedInOrder()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - Enqueue multiple keys
        va2m.EnqueueKey(0x41); // 'A'
        va2m.EnqueueKey(0x42); // 'B'
        va2m.EnqueueKey(0x43); // 'C'
        va2m.Clock(); // Process all pending commands

        // Assert - All commands executed in FIFO order
        var enqueuedKeys = testBus.GetEnqueuedKeyHistory();
        Assert.Equal(new byte[] { 0x41, 0x42, 0x43 }, enqueuedKeys);
    }

    [Fact]
    public void EnqueueKey_DeferredExecution_CommandNotExecutedUntilClock()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - Enqueue key but don't call Clock()
        va2m.EnqueueKey(0x41);

        // Assert - Command not executed yet (no Clock() called)
        Assert.Empty(testBus.GetEnqueuedKeyHistory());
        
        // Act - Now process pending queue
        va2m.Clock();
        
        // Assert - Command executed after Clock()
        Assert.Single(testBus.GetEnqueuedKeyHistory());
    }

    [Fact]
    public void EnqueueKey_ValueCaptureIndependence_EachActionCapturesOwnValue()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - Enqueue keys in a loop (tests closure capture behavior)
        for (byte i = 0x41; i <= 0x43; i++)
        {
            va2m.EnqueueKey(i);
        }
        va2m.Clock(); // Process all

        // Assert - Each action captured its own value independently
        var enqueuedKeys = testBus.GetEnqueuedKeyHistory();
        Assert.Equal(new byte[] { 0x41, 0x42, 0x43 }, enqueuedKeys);
    }

    #endregion

    #region Command Queueing Tests - SetPushButton

    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(2, false)]
    public void SetPushButton_UpdatesButtonState(byte buttonNum, bool pressed)
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act
        va2m.SetPushButton(buttonNum, pressed);
        va2m.Clock(); // Process pending queue

        // Assert
        Assert.Equal(pressed, testBus.GetPushButton(buttonNum));
    }

    [Fact]
    public void SetPushButton_MultipleButtons_IndependentStates()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - Set different states for each button
        va2m.SetPushButton(0, true);
        va2m.SetPushButton(1, false);
        va2m.SetPushButton(2, true);
        va2m.Clock(); // Process pending queue

        // Assert
        Assert.True(testBus.GetPushButton(0));
        Assert.False(testBus.GetPushButton(1));
        Assert.True(testBus.GetPushButton(2));
    }

    [Fact]
    public void SetPushButton_Toggle_ChangesState()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - Press, release, press again
        va2m.SetPushButton(0, true);
        va2m.Clock();
        Assert.True(testBus.GetPushButton(0));

        va2m.SetPushButton(0, false);
        va2m.Clock();
        Assert.False(testBus.GetPushButton(0));

        va2m.SetPushButton(0, true);
        va2m.Clock();
        Assert.True(testBus.GetPushButton(0));
    }

    #endregion

    #region Throttling Tests

    [Fact]
    public void Clock_WithThrottleDisabled_ExecutesQuickly()
    {
        // Arrange
        var va2m = VA2MTestHelpers.CreateBuilder().Build();
        va2m.ThrottleEnabled = false;

        // Act - Run many cycles
        var startTime = DateTime.UtcNow;
        for (int i = 0; i < 1000; i++)
        {
            va2m.Clock();
        }
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should complete in well under 1 second
        Assert.True(elapsed.TotalMilliseconds < 100, 
            $"1000 unthrottled cycles took {elapsed.TotalMilliseconds}ms (should be < 100ms)");
    }

    #endregion

    #region Bus Interaction Tests

    [Fact]
    public void Bus_IsAccessibleAfterConstruction()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act & Assert
        Assert.Same(testBus, va2m.Bus);
    }

    [Fact]
    public void SystemClock_UpdatesWithBusClock()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act
        for (int i = 0; i < 10; i++)
        {
            testBus.Clock();
        }

        // Assert
        Assert.Equal(10UL, va2m.SystemClock);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var va2m = VA2MTestHelpers.CreateBuilder().Build();

        // Act & Assert - Should not throw
        va2m.Dispose();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var va2m = VA2MTestHelpers.CreateBuilder().Build();

        // Act & Assert - Should not throw on multiple calls
        va2m.Dispose();
        va2m.Dispose();
        va2m.Dispose();
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public void Scenario_BootSequence_InitializesCorrectly()
    {
        // Arrange
        var testState = new TestEmulatorState();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithEmulatorState(testState)
            .Build();

        // Act - Simulate basic boot
        va2m.Reset();
        for (int i = 0; i < 100; i++)
        {
            va2m.Clock(); // First Clock() processes the Reset, subsequent ones execute normally
        }

        // Assert
        Assert.Equal(100UL, va2m.SystemClock);
        Assert.True(testState.UpdateCount > 0, "State should be updated during execution");
    }

    [Fact]
    public void Scenario_CommandQueueing_MixedCommands()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - Mix keyboard and button commands
        va2m.EnqueueKey(0x41);       // 'A'
        va2m.SetPushButton(0, true);  // Button 0 press
        va2m.EnqueueKey(0x42);       // 'B'
        va2m.SetPushButton(0, false); // Button 0 release
        va2m.Clock(); // Process all pending commands

        // Assert - All commands executed in order
        var keyHistory = testBus.GetEnqueuedKeyHistory();
        Assert.Equal(2, keyHistory.Count);
        Assert.Equal(0x41, keyHistory[0]);
        Assert.Equal(0x42, keyHistory[1]);
        Assert.False(testBus.GetPushButton(0)); // Final button state
    }

    [Fact]
    public void Scenario_GameController_MultipleButtonPresses()
    {
        // Arrange
        var testBus = new TestAppleIIBus();
        var va2m = VA2MTestHelpers.CreateBuilder()
            .WithBus(testBus)
            .Build();

        // Act - Simulate game controller inputs
        va2m.SetPushButton(0, true);  // Fire button
        va2m.Clock();
        va2m.SetPushButton(1, true);  // Jump button
        va2m.Clock();
        va2m.SetPushButton(0, false); // Release fire
        va2m.Clock();

        // Assert
        Assert.False(testBus.GetPushButton(0)); // Fire released
        Assert.True(testBus.GetPushButton(1));  // Jump still pressed
        Assert.False(testBus.GetPushButton(2)); // Third button not pressed
    }

    #endregion
}
