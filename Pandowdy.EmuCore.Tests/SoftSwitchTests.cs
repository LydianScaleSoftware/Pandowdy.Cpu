using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore.Tests;

/// <summary>
/// Comprehensive tests for SoftSwitch-related classes:
/// - SoftSwitch: Named boolean soft switch with Set/Get/Toggle operations
/// - SoftSwitches: Collection manager for all Apple II soft switches
/// 
/// These classes manage soft switch states and provide responder
/// notifications for state changes.
/// </summary>
public class SoftSwitchTests
{

    
    #region SoftSwitch Tests (9 tests)

    [Fact]
    public void SoftSwitch_Constructor_InitializesWithName()
    {
        // Arrange & Act
        var softSwitch = new SoftSwitch("TEST");

        // Assert
        Assert.Equal("TEST", softSwitch.Name);
        Assert.False(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_SetValue_ChangesState()
    {
        // Arrange
        var softSwitch = new SoftSwitch("RAMRD")
        {
            // Act
            Value = true
        };

        // Assert
        Assert.True(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_NameProperty_IsReadable()
    {
        // Arrange
        var softSwitch = new SoftSwitch("80STORE");
        softSwitch.Set();

        // Act & Assert - Verify name is accessible
        Assert.Equal("80STORE", softSwitch.Name);
        Assert.True(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_InheritsBoolBehavior()
    {
        // Arrange
        var softSwitch = new SoftSwitch("TEXT");

        // Act
        softSwitch.Set();
        softSwitch.Toggle();

        // Assert - After Set() and Toggle(), should be False
        Assert.False(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_Name_IsReadOnly()
    {
        // Arrange
        var softSwitch = new SoftSwitch("HIRES");

        // Act & Assert
        Assert.Equal("HIRES", softSwitch.Name);
        // Name property is { get; private set; } - can't be changed externally
    }

    [Fact]
    public void SoftSwitch_WithAppleIINames_WorksCorrectly()
    {
        // Arrange & Act
        var switches = new[]
        {
            new SoftSwitch("80STORE"),
            new SoftSwitch("RAMRD"),
            new SoftSwitch("RAMWRT"),
            new SoftSwitch("INTCXROM"),
            new SoftSwitch("ALTZP")
        };

        // Assert
        Assert.All(switches, sw => Assert.False(sw.Value));
    }

    [Fact]
    public void SoftSwitch_StateTransitions_TrackedCorrectly()
    {
        // Arrange
        var softSwitch = new SoftSwitch("MIXED")
        {
            // Act - Simulate typical soft switch usage
            Value = true  // Enable
        };
        softSwitch.Value = false; // Disable
        softSwitch.Value = true;  // Enable again

        // Assert
        Assert.True(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_ToString_IncludesNameAndValue()
    {
        // Arrange
        var softSwitch = new SoftSwitch("80STORE");
        softSwitch.Set();

        // Act
        var result = softSwitch.ToString();

        // Assert
        Assert.Contains("80STORE", result);
        Assert.Contains("True", result);
    }

    #endregion

    #region SoftSwitches Collection Tests (25 tests)

    [Fact]
    public void SoftSwitches_Constructor_InitializesAllSwitches()
    {
        // Arrange & Act
        var switches = new SoftSwitches();

        // Assert - Verify all 24 switches are initialized
        var list = switches.GetSwitchList();
        Assert.Equal(24, list.Count);
    }

    [Fact]
    public void SoftSwitches_Constructor_AllSwitchesStartFalse()
    {
        // Arrange & Act
        var switches = new SoftSwitches();

        // Assert
        var list = switches.GetSwitchList();
        Assert.All(list, item => Assert.False(item.value));
    }

    [Fact]
    public void SoftSwitches_Set_ChangesSwitch()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act
        switches.Set(SoftSwitches.SoftSwitchId.RamRd, true);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamRd));
    }

    [Fact]
    public void SoftSwitches_Get_ReturnsCurrentValue()
    {
        // Arrange
        var switches = new SoftSwitches();
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);

        // Act
        var value = switches.Get(SoftSwitches.SoftSwitchId.Text);

        // Assert
        Assert.True(value);
    }

    [Fact]
    public void SoftSwitches_Get_ReturnsFalseForUnsetSwitch()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act
        var value = switches.Get(SoftSwitches.SoftSwitchId.HiRes);

        // Assert
        Assert.False(value);
    }

    [Fact]
    public void SoftSwitches_SetMultiple_AllWork()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act
        switches.Set(SoftSwitches.SoftSwitchId.Store80, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamRd, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamWrt, true);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Store80));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamRd));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamWrt));
    }

    [Fact]
    public void SoftSwitches_ResetAllSwitches_ClearsAllToFalse()
    {
        // Arrange
        var switches = new SoftSwitches();
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);
        switches.Set(SoftSwitches.SoftSwitchId.Mixed, true);
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);

        // Act
        switches.ResetAllSwitches();

        // Assert
        var _ = switches.GetSwitchList();
        // All should be false except IntCxRom (which defaults to true)
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Text));
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Mixed));
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.HiRes));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.IntCxRom));
    }

    [Fact]
    public void SoftSwitches_ResetAllSwitches_IntCxRomDefaultsToTrue()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act
        switches.ResetAllSwitches();

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.IntCxRom));
    }

    



    [Fact]
    public void SoftSwitches_GetSwitchList_ReturnsAllSwitches()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act
        var list = switches.GetSwitchList();

        // Assert
        Assert.Equal(24, list.Count);
        Assert.Contains(list, item => item.id == SoftSwitches.SoftSwitchId.Store80);
        Assert.Contains(list, item => item.id == SoftSwitches.SoftSwitchId.RamRd);
        Assert.Contains(list, item => item.id == SoftSwitches.SoftSwitchId.IntCxRom);
    }



    [Fact]
    public void SoftSwitches_AddResponder_StoresResponder()
    {
        // Arrange
        var switches = new SoftSwitches();
        var responder = new TestSoftSwitchResponder();

        // Act
        switches.AddResponder(responder);

        // Assert - Verify by setting a switch and checking responder
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);
        Assert.Equal(1, responder.TextCallCount);
    }

    [Fact]
    public void SoftSwitches_SetWithResponder_NotifiesResponder()
    {
        // Arrange
        var switches = new SoftSwitches();
        var responder = new TestSoftSwitchResponder();
        switches.AddResponder(responder);

        // Act
        switches.Set(SoftSwitches.SoftSwitchId.Store80, true);

        // Assert
        Assert.Equal(1, responder.Store80CallCount);
        Assert.True(responder.LastStore80Value);
    }

    [Fact]
    public void SoftSwitches_MultipleResponders_AllNotified()
    {
        // Arrange
        var switches = new SoftSwitches();
        var responder1 = new TestSoftSwitchResponder();
        var responder2 = new TestSoftSwitchResponder();
        var responder3 = new TestSoftSwitchResponder();
        
        switches.AddResponder(responder1);
        switches.AddResponder(responder2);
        switches.AddResponder(responder3);

        // Act
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);

        // Assert
        Assert.Equal(1, responder1.HiResCallCount);
        Assert.Equal(1, responder2.HiResCallCount);
        Assert.Equal(1, responder3.HiResCallCount);
    }

    [Fact]
    public void SoftSwitches_AllSwitchIds_HaveResponderHandlers()
    {
        // Arrange
        var switches = new SoftSwitches();
        var responder = new TestSoftSwitchResponder();
        switches.AddResponder(responder);

        // Act - Set all switches
        switches.Set(SoftSwitches.SoftSwitchId.Store80, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamRd, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamWrt, true);
        switches.Set(SoftSwitches.SoftSwitchId.IntCxRom, true);
        switches.Set(SoftSwitches.SoftSwitchId.AltZp, true);
        switches.Set(SoftSwitches.SoftSwitchId.SlotC3Rom, true);
        switches.Set(SoftSwitches.SoftSwitchId.Vid80, true);
        switches.Set(SoftSwitches.SoftSwitchId.AltChar, true);
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);
        switches.Set(SoftSwitches.SoftSwitchId.Mixed, true);
        switches.Set(SoftSwitches.SoftSwitchId.Page2, true);
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);
        switches.Set(SoftSwitches.SoftSwitchId.An0, true);
        switches.Set(SoftSwitches.SoftSwitchId.An1, true);
        switches.Set(SoftSwitches.SoftSwitchId.An2, true);
        switches.Set(SoftSwitches.SoftSwitchId.An3, true);
        switches.Set(SoftSwitches.SoftSwitchId.Bank1, true);
        switches.Set(SoftSwitches.SoftSwitchId.HighWrite, true);
        switches.Set(SoftSwitches.SoftSwitchId.HighRead, true);
        switches.Set(SoftSwitches.SoftSwitchId.PreWrite, true);

        // Assert - All responder methods should be called
        Assert.Equal(1, responder.Store80CallCount);
        Assert.Equal(1, responder.RamRdCallCount);
        Assert.Equal(1, responder.RamWrtCallCount);
        Assert.Equal(1, responder.IntCxRomCallCount);
        Assert.Equal(1, responder.AltZpCallCount);
        Assert.Equal(1, responder.SlotC3RomCallCount);
        Assert.Equal(1, responder.Vid80CallCount);
        Assert.Equal(1, responder.AltCharCallCount);
        Assert.Equal(1, responder.TextCallCount);
        Assert.Equal(1, responder.MixedCallCount);
        Assert.Equal(1, responder.Page2CallCount);
        Assert.Equal(1, responder.HiResCallCount);
        Assert.Equal(1, responder.An0CallCount);
        Assert.Equal(1, responder.An1CallCount);
        Assert.Equal(1, responder.An2CallCount);
        Assert.Equal(1, responder.An3CallCount);
        Assert.Equal(1, responder.Bank1CallCount);
        Assert.Equal(1, responder.HighWriteCallCount);
        Assert.Equal(1, responder.HighReadCallCount);
        Assert.Equal(1, responder.PreWriteCallCount);
    }

    [Fact]
    public void SoftSwitches_ResetAllSwitches_NotifiesResponders()
    {
        // Arrange
        var switches = new SoftSwitches();
        var responder = new TestSoftSwitchResponder();
        switches.AddResponder(responder);
        responder.Reset(); // Clear initial constructor notifications

        // Act
        switches.ResetAllSwitches();

        // Assert - All switches should trigger responder
        Assert.True(responder.Store80CallCount > 0);
        Assert.True(responder.TextCallCount > 0);
        Assert.True(responder.IntCxRomCallCount > 0); // Should be set to true
    }

    [Fact]
    public void SoftSwitches_VideoModeSwitches_WorkCorrectly()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act - Set video mode switches
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);
        switches.Set(SoftSwitches.SoftSwitchId.Mixed, true);
        switches.Set(SoftSwitches.SoftSwitchId.Page2, true);
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Text));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Mixed));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Page2));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HiRes));
    }

    [Fact]
    public void SoftSwitches_MemoryConfigSwitches_WorkCorrectly()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act - Set memory configuration switches
        switches.Set(SoftSwitches.SoftSwitchId.Store80, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamRd, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamWrt, true);
        switches.Set(SoftSwitches.SoftSwitchId.AltZp, true);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Store80));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamRd));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamWrt));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.AltZp));
    }

    [Fact]
    public void SoftSwitches_LanguageCardSwitches_WorkCorrectly()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act - Set language card switches
        switches.Set(SoftSwitches.SoftSwitchId.Bank1, true);
        switches.Set(SoftSwitches.SoftSwitchId.HighRead, true);
        switches.Set(SoftSwitches.SoftSwitchId.HighWrite, true);
        switches.Set(SoftSwitches.SoftSwitchId.PreWrite, true);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Bank1));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HighRead));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HighWrite));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.PreWrite));
    }

    [Fact]
    public void SoftSwitches_AnnunciatorSwitches_WorkCorrectly()
    {
        // Arrange
        var switches = new SoftSwitches();

        // Act - Set all annunciator switches
        switches.Set(SoftSwitches.SoftSwitchId.An0, true);
        switches.Set(SoftSwitches.SoftSwitchId.An1, true);
        switches.Set(SoftSwitches.SoftSwitchId.An2, true);
        switches.Set(SoftSwitches.SoftSwitchId.An3, true);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An0));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An1));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An2));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An3));
    }

    [Fact]
    public void SoftSwitches_ComplexScenario_TracksAllChanges()
    {
        // Arrange
        var switches = new SoftSwitches();
        var responder = new TestSoftSwitchResponder();
        switches.AddResponder(responder);
        responder.Reset();

        // Act - Simulate typical Apple II operation
        switches.Set(SoftSwitches.SoftSwitchId.Text, false);  // Enter graphics mode
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);  // Hi-res graphics
        switches.Set(SoftSwitches.SoftSwitchId.Mixed, true);  // Mixed mode
        switches.Set(SoftSwitches.SoftSwitchId.Page2, true);  // Page 2
        switches.Set(SoftSwitches.SoftSwitchId.Store80, true); // 80STORE for aux memory

        // Assert
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Text));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HiRes));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Mixed));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Page2));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Store80));
        
        // Verify responders were notified
        Assert.Equal(1, responder.TextCallCount);
        Assert.Equal(1, responder.HiResCallCount);
        Assert.Equal(1, responder.MixedCallCount);
        Assert.Equal(1, responder.Page2CallCount);
        Assert.Equal(1, responder.Store80CallCount);
    }

    [Fact]
    public void SoftSwitches_DumpSoftSwitchStatus_DoesNotThrow()
    {
        // Arrange
        var switches = new SoftSwitches();
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);

        // Act & Assert - Should not throw (writes to Debug.WriteLine)
        switches.DumpSoftSwitchStatus("Test Header");
        switches.DumpSoftSwitchStatus(); // Without header
    }

    #endregion

    #region Test Helper Class

    /// <summary>
    /// Test implementation of ISoftSwitchResponder that tracks all calls.
    /// </summary>
    private class TestSoftSwitchResponder : ISoftSwitchResponder
    {
        public int Store80CallCount { get; private set; }
        public int RamRdCallCount { get; private set; }
        public int RamWrtCallCount { get; private set; }
        public int IntCxRomCallCount { get; private set; }
        public int AltZpCallCount { get; private set; }
        public int SlotC3RomCallCount { get; private set; }
        public int Vid80CallCount { get; private set; }
        public int AltCharCallCount { get; private set; }
        public int TextCallCount { get; private set; }
        public int MixedCallCount { get; private set; }
        public int Page2CallCount { get; private set; }
        public int HiResCallCount { get; private set; }
        public int An0CallCount { get; private set; }
        public int An1CallCount { get; private set; }
        public int An2CallCount { get; private set; }
        public int An3CallCount { get; private set; }
        public int Bank1CallCount { get; private set; }
        public int HighWriteCallCount { get; private set; }
        public int HighReadCallCount { get; private set; }
        public int PreWriteCallCount { get; private set; }

        public bool LastStore80Value { get; private set; }

        public void Set80Store(bool value) { Store80CallCount++; LastStore80Value = value; }
        public void SetRamRd(bool value) { RamRdCallCount++; }
        public void SetRamWrt(bool value) { RamWrtCallCount++; }
        public void SetIntCxRom(bool value) { IntCxRomCallCount++; }
        public void SetAltZp(bool value) { AltZpCallCount++; }
        public void SetSlotC3Rom(bool value) { SlotC3RomCallCount++; }
        public void Set80Vid(bool value) { Vid80CallCount++; }
        public void SetAltChar(bool value) { AltCharCallCount++; }
        public void SetText(bool value) { TextCallCount++; }
        public void SetMixed(bool value) { MixedCallCount++; }
        public void SetPage2(bool value) { Page2CallCount++; }
        public void SetHiRes(bool value) { HiResCallCount++; }
        public void SetAn0(bool value) { An0CallCount++; }
        public void SetAn1(bool value) { An1CallCount++; }
        public void SetAn2(bool value) { An2CallCount++; }
        public void SetAn3(bool value) { An3CallCount++; }
        public void SetBank1(bool value) { Bank1CallCount++; }
        public void SetHighWrite(bool value) { HighWriteCallCount++; }
        public void SetHighRead(bool value) { HighReadCallCount++; }
        public void SetPreWrite(bool value) { PreWriteCallCount++; }
        public void SetButton0(bool value) { }
        public void SetButton1(bool value) { }
        public void SetButton2(bool value) { }
        public void SetVBlank(bool value) { }

        public void Reset()
        {
            Store80CallCount = 0;
            RamRdCallCount = 0;
            RamWrtCallCount = 0;
            IntCxRomCallCount = 0;
            AltZpCallCount = 0;
            SlotC3RomCallCount = 0;
            Vid80CallCount = 0;
            AltCharCallCount = 0;
            TextCallCount = 0;
            MixedCallCount = 0;
            Page2CallCount = 0;
            HiResCallCount = 0;
            An0CallCount = 0;
            An1CallCount = 0;
            An2CallCount = 0;
            An3CallCount = 0;
            Bank1CallCount = 0;
            HighWriteCallCount = 0;
            HighReadCallCount = 0;
            PreWriteCallCount = 0;
        }
    }

    #endregion
}
