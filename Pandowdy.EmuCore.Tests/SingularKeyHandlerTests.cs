using Xunit;
using Pandowdy.EmuCore;
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore.Tests;

/// <summary>
/// Tests for SingularKeyHandler - Apple IIe keyboard emulation with strobe mechanism.
/// </summary>
public class SingularKeyHandlerTests
{
    #region Constructor and Initial State Tests

    [Fact]
    public void Constructor_InitializesWithNoKeyPending()
    {
        // Arrange & Act
        var handler = new SingularKeyHandler();

        // Assert
        Assert.False(handler.StrobePending(), "No key should be pending initially");
        Assert.Equal(0x00, handler.PeekCurrentKeyValue());
        Assert.Equal(0x00, handler.PeekCurrentKeyAndStrobe());
    }

    #endregion

    #region EnqueueKey Tests

    [Fact]
    public void EnqueueKey_SetsStrobeBit()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act
        handler.EnqueueKey(0x41); // 'A' key (ASCII 65)

        // Assert
        Assert.True(handler.StrobePending(), "Strobe should be set after enqueue");
        Assert.Equal(0xC1, handler.PeekCurrentKeyAndStrobe()); // 0x41 | 0x80
    }

    [Theory]
    [InlineData(0x41, 0xC1)] // 'A' -> 0xC1
    [InlineData(0x0D, 0x8D)] // Return -> 0x8D
    [InlineData(0x1B, 0x9B)] // Escape -> 0x9B
    [InlineData(0x20, 0xA0)] // Space -> 0xA0
    [InlineData(0x00, 0x80)] // Null -> 0x80
    [InlineData(0x7F, 0xFF)] // DEL -> 0xFF
    public void EnqueueKey_SetsStrobeBit_ForVariousKeys(byte asciiCode, byte expectedWithStrobe)
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act
        handler.EnqueueKey(asciiCode);

        // Assert
        Assert.Equal(expectedWithStrobe, handler.PeekCurrentKeyAndStrobe());
        Assert.Equal(asciiCode, handler.PeekCurrentKeyValue());
    }

    [Fact]
    public void EnqueueKey_OverwritesPreviousUnreadKey()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41); // 'A'

        // Act - Enqueue 'B' without reading 'A'
        handler.EnqueueKey(0x42); // 'B'

        // Assert - 'A' should be lost (authentic Apple IIe behavior)
        Assert.Equal(0xC2, handler.PeekCurrentKeyAndStrobe()); // 'B' with strobe
        Assert.Equal(0x42, handler.PeekCurrentKeyValue());
    }

    [Fact]
    public void EnqueueKey_ForcesStrobeBit_EvenIfAlreadySet()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act - Pass key with strobe already set
        handler.EnqueueKey(0xC1); // 'A' with bit 7 already set

        // Assert - Should still result in 0xC1 (strobe forced)
        Assert.Equal(0xC1, handler.PeekCurrentKeyAndStrobe());
        Assert.Equal(0x41, handler.PeekCurrentKeyValue());
    }

    #endregion

    #region StrobePending Tests

    [Fact]
    public void StrobePending_ReturnsFalse_WhenNoKeyEnqueued()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act & Assert
        Assert.False(handler.StrobePending());
    }

    [Fact]
    public void StrobePending_ReturnsTrue_AfterKeyEnqueued()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act
        handler.EnqueueKey(0x41);

        // Assert
        Assert.True(handler.StrobePending());
    }

    [Fact]
    public void StrobePending_ReturnsFalse_AfterStrobeCleared()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41);

        // Act
        handler.FetchPendingAndClearStrobe();

        // Assert
        Assert.False(handler.StrobePending());
    }

    [Fact]
    public void StrobePending_IsIdempotent()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41);

        // Act - Call multiple times
        bool first = handler.StrobePending();
        bool second = handler.StrobePending();
        bool third = handler.StrobePending();

        // Assert - Should not change state
        Assert.True(first);
        Assert.True(second);
        Assert.True(third);
        Assert.Equal(0xC1, handler.PeekCurrentKeyAndStrobe()); // Strobe still set
    }

    #endregion

    #region PeekCurrentKeyValue Tests

    [Fact]
    public void PeekCurrentKeyValue_ReturnsZero_WhenNoKeyEnqueued()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act & Assert
        Assert.Equal(0x00, handler.PeekCurrentKeyValue());
    }

    [Fact]
    public void PeekCurrentKeyValue_ReturnsAsciiCode_WithoutStrobeBit()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41); // 'A'

        // Act
        byte value = handler.PeekCurrentKeyValue();

        // Assert
        Assert.Equal(0x41, value); // ASCII only, no strobe
        Assert.True(handler.StrobePending()); // Strobe still set
    }

    [Theory]
    [InlineData(0x41)] // 'A'
    [InlineData(0x42)] // 'B'
    [InlineData(0x0D)] // Return
    [InlineData(0x20)] // Space
    [InlineData(0x1B)] // Escape
    public void PeekCurrentKeyValue_Returns7BitValue_ForVariousKeys(byte asciiCode)
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(asciiCode);

        // Act
        byte value = handler.PeekCurrentKeyValue();

        // Assert
        Assert.Equal(asciiCode, value);
        Assert.True(value < 128); // Always 7-bit value
    }

    [Fact]
    public void PeekCurrentKeyValue_IsNonDestructive()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41);

        // Act - Call multiple times
        byte first = handler.PeekCurrentKeyValue();
        byte second = handler.PeekCurrentKeyValue();
        byte third = handler.PeekCurrentKeyValue();

        // Assert
        Assert.Equal(0x41, first);
        Assert.Equal(0x41, second);
        Assert.Equal(0x41, third);
        Assert.True(handler.StrobePending()); // Strobe unchanged
    }

    #endregion

    #region PeekCurrentKeyAndStrobe Tests

    [Fact]
    public void PeekCurrentKeyAndStrobe_ReturnsZero_WhenNoKeyEnqueued()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act & Assert
        Assert.Equal(0x00, handler.PeekCurrentKeyAndStrobe());
    }

    [Fact]
    public void PeekCurrentKeyAndStrobe_ReturnsFullByte_WithStrobeBit()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41); // 'A'

        // Act
        byte value = handler.PeekCurrentKeyAndStrobe();

        // Assert
        Assert.Equal(0xC1, value); // 0x41 | 0x80
        Assert.True(value >= 128); // Bit 7 set
    }

    [Theory]
    [InlineData(0x41, 0xC1)] // 'A'
    [InlineData(0x0D, 0x8D)] // Return
    [InlineData(0x1B, 0x9B)] // Escape
    [InlineData(0x20, 0xA0)] // Space
    public void PeekCurrentKeyAndStrobe_ReturnsCorrectValue_WithStrobeSet(byte ascii, byte expected)
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(ascii);

        // Act
        byte value = handler.PeekCurrentKeyAndStrobe();

        // Assert
        Assert.Equal(expected, value);
    }

    [Fact]
    public void PeekCurrentKeyAndStrobe_ReturnsAsciiOnly_AfterStrobeCleared()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41);

        // Act - Clear strobe
        handler.FetchPendingAndClearStrobe();
        byte value = handler.PeekCurrentKeyAndStrobe();

        // Assert
        Assert.Equal(0x41, value); // Strobe cleared, only ASCII remains
        Assert.True(value < 128); // Bit 7 clear
    }

    [Fact]
    public void PeekCurrentKeyAndStrobe_IsNonDestructive()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41);

        // Act - Call multiple times
        byte first = handler.PeekCurrentKeyAndStrobe();
        byte second = handler.PeekCurrentKeyAndStrobe();
        byte third = handler.PeekCurrentKeyAndStrobe();

        // Assert
        Assert.Equal(0xC1, first);
        Assert.Equal(0xC1, second);
        Assert.Equal(0xC1, third);
        Assert.True(handler.StrobePending()); // Strobe unchanged
    }

    #endregion

    #region FetchPendingAndClearStrobe Tests

    [Fact]
    public void FetchPendingAndClearStrobe_ReturnsNull_WhenNoKeyPending()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act
        byte? key = handler.FetchPendingAndClearStrobe();

        // Assert
        Assert.Null(key);
    }

    [Fact]
    public void FetchPendingAndClearStrobe_ReturnsKey_AndClearsStrobe()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41); // 'A'

        // Act
        byte? key = handler.FetchPendingAndClearStrobe();

        // Assert
        Assert.NotNull(key);
        Assert.Equal(0x41, key.Value); // Returns ASCII without strobe
        Assert.False(handler.StrobePending()); // Strobe cleared
        Assert.Equal(0x41, handler.PeekCurrentKeyAndStrobe()); // Bit 7 clear
    }

    [Theory]
    [InlineData(0x41)] // 'A'
    [InlineData(0x42)] // 'B'
    [InlineData(0x0D)] // Return
    [InlineData(0x1B)] // Escape
    [InlineData(0x20)] // Space
    public void FetchPendingAndClearStrobe_ReturnsCorrectAscii_ForVariousKeys(byte asciiCode)
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(asciiCode);

        // Act
        byte? key = handler.FetchPendingAndClearStrobe();

        // Assert
        Assert.Equal(asciiCode, key.Value);
        Assert.False(handler.StrobePending());
    }

    [Fact]
    public void FetchPendingAndClearStrobe_ReturnsNull_AfterAlreadyCleared()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41);
        handler.FetchPendingAndClearStrobe(); // First clear

        // Act - Try to fetch again
        byte? key = handler.FetchPendingAndClearStrobe();

        // Assert
        Assert.Null(key); // No key pending anymore
    }

    [Fact]
    public void FetchPendingAndClearStrobe_IsIdempotent_WhenNoKey()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act - Call multiple times with no key
        byte? first = handler.FetchPendingAndClearStrobe();
        byte? second = handler.FetchPendingAndClearStrobe();
        byte? third = handler.FetchPendingAndClearStrobe();

        // Assert
        Assert.Null(first);
        Assert.Null(second);
        Assert.Null(third);
    }

    #endregion

    #region Integration/Scenario Tests

    [Fact]
    public void Scenario_TypicalKeyPressAndRead()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act & Assert - Simulate Apple IIe software reading keyboard
        // 1. Initially no key
        Assert.False(handler.StrobePending());
        Assert.Equal(0x00, handler.PeekCurrentKeyAndStrobe());

        // 2. User presses 'A'
        handler.EnqueueKey(0x41);
        Assert.True(handler.StrobePending());
        Assert.Equal(0xC1, handler.PeekCurrentKeyAndStrobe()); // Read $C000 with strobe

        // 3. Software reads and clears strobe
        byte? key = handler.FetchPendingAndClearStrobe(); // Read $C010
        Assert.Equal(0x41, key.Value);
        Assert.False(handler.StrobePending());

        // 4. Subsequent reads return ASCII only
        Assert.Equal(0x41, handler.PeekCurrentKeyAndStrobe()); // Read $C000 without strobe
    }

    [Fact]
    public void Scenario_MultipleKeypresses_OverwritesBehavior()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act & Assert - Apple IIe overwrites unread keys
        handler.EnqueueKey(0x41); // 'A'
        handler.EnqueueKey(0x42); // 'B' - overwrites 'A'
        handler.EnqueueKey(0x43); // 'C' - overwrites 'B'

        // Only 'C' survives
        byte? key = handler.FetchPendingAndClearStrobe();
        Assert.Equal(0x43, key.Value);
    }

    [Fact]
    public void Scenario_ReadWithoutClearing_ThenClear()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        handler.EnqueueKey(0x41);

        // Act - Read multiple times without clearing
        byte read1 = handler.PeekCurrentKeyAndStrobe(); // LDA $C000
        byte read2 = handler.PeekCurrentKeyAndStrobe(); // LDA $C000
        byte read3 = handler.PeekCurrentKeyAndStrobe(); // LDA $C000

        // Assert - Strobe still set after reads
        Assert.Equal(0xC1, read1);
        Assert.Equal(0xC1, read2);
        Assert.Equal(0xC1, read3);
        Assert.True(handler.StrobePending());

        // Act - Now clear strobe
        byte? key = handler.FetchPendingAndClearStrobe(); // STA $C010

        // Assert - Strobe cleared
        Assert.Equal(0x41, key.Value);
        Assert.False(handler.StrobePending());
    }

    [Fact]
    public void Scenario_ClearStrobe_ThenEnqueueNewKey()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act - First key
        handler.EnqueueKey(0x41); // 'A'
        byte? firstKey = handler.FetchPendingAndClearStrobe();

        // Second key
        handler.EnqueueKey(0x42); // 'B'
        byte? secondKey = handler.FetchPendingAndClearStrobe();

        // Assert
        Assert.Equal(0x41, firstKey.Value);
        Assert.Equal(0x42, secondKey.Value);
        Assert.False(handler.StrobePending());
    }

    [Fact]
    public void Scenario_AppleIIe_PollingLoop()
    {
        // Arrange - Simulate Apple IIe GETKEY routine
        var handler = new SingularKeyHandler();

        // GETKEY: LDA $C000 ; BPL GETKEY ; STA $C010 ; AND #$7F
        
        // Initially no key (would loop in real code)
        Assert.False(handler.StrobePending());

        // User presses Return
        handler.EnqueueKey(0x0D);

        // LDA $C000 - Check strobe
        Assert.True(handler.StrobePending()); // BPL would fail, exit loop
        byte kbdValue = handler.PeekCurrentKeyAndStrobe();
        Assert.Equal(0x8D, kbdValue); // Return with strobe

        // STA $C010 - Clear strobe
        byte? key = handler.FetchPendingAndClearStrobe();
        Assert.NotNull(key);

        // AND #$7F - Mask off strobe (already done by FetchPendingAndClearStrobe)
        Assert.Equal(0x0D, key.Value);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void Implements_IKeyboardReader()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Assert
        Assert.IsAssignableFrom<IKeyboardReader>(handler);
    }

    [Fact]
    public void Implements_IKeyboardSetter()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Assert
        Assert.IsAssignableFrom<IKeyboardSetter>(handler);
    }

    [Fact]
    public void IKeyboardSetter_EnqueueKey_WorksSameAsIKeyboardReader()
    {
        // Arrange
        var handler = new SingularKeyHandler();
        IKeyboardSetter setter = handler;
        IKeyboardReader reader = handler;

        // Act
        setter.EnqueueKey(0x41);

        // Assert
        Assert.True(reader.StrobePending());
        Assert.Equal(0xC1, reader.PeekCurrentKeyAndStrobe());
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void EnqueueKey_WithZero_WorksCorrectly()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act
        handler.EnqueueKey(0x00); // Null character

        // Assert
        Assert.True(handler.StrobePending());
        Assert.Equal(0x80, handler.PeekCurrentKeyAndStrobe());
        Assert.Equal(0x00, handler.PeekCurrentKeyValue());
    }

    [Fact]
    public void EnqueueKey_WithMaxValue_WorksCorrectly()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act
        handler.EnqueueKey(0x7F); // DEL character (max 7-bit ASCII)

        // Assert
        Assert.True(handler.StrobePending());
        Assert.Equal(0xFF, handler.PeekCurrentKeyAndStrobe());
        Assert.Equal(0x7F, handler.PeekCurrentKeyValue());
    }

    [Fact]
    public void EnqueueKey_ControlCharacters_WorkCorrectly()
    {
        // Arrange
        var handler = new SingularKeyHandler();

        // Act & Assert - Test control characters
        handler.EnqueueKey(0x01); // Ctrl+A
        Assert.Equal(0x81, handler.PeekCurrentKeyAndStrobe());

        handler.EnqueueKey(0x03); // Ctrl+C
        Assert.Equal(0x83, handler.PeekCurrentKeyAndStrobe());

        handler.EnqueueKey(0x1A); // Ctrl+Z
        Assert.Equal(0x9A, handler.PeekCurrentKeyAndStrobe());
    }

    #endregion
}
