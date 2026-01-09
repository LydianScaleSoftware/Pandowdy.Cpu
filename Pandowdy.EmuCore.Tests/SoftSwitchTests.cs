using Pandowdy.EmuCore.DataTypes;
using Pandowdy.EmuCore.Services;

namespace Pandowdy.EmuCore.Tests;

/// <summary>
/// Unit tests for SoftSwitch data structure and SoftSwitches collection manager.
/// Tests basic switch functionality and integration with SystemStatusProvider.
/// </summary>
public class SoftSwitchTests
{
    #region SoftSwitch DataType Tests

    [Fact]
    public void SoftSwitch_Constructor_InitializesWithNameAndDefaultValue()
    {
        // Arrange & Act
        var softSwitch = new SoftSwitch("TEST");

        // Assert
        Assert.Equal("TEST", softSwitch.Name);
        Assert.False(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_Constructor_InitializesWithNameAndValue()
    {
        // Arrange & Act
        var softSwitch = new SoftSwitch("TEST", true);

        // Assert
        Assert.Equal("TEST", softSwitch.Name);
        Assert.True(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_SetValue_ChangesState()
    {
        // Arrange
        var softSwitch = new SoftSwitch("RAMRD");

        // Act
        softSwitch.Value = true;

        // Assert
        Assert.True(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_SetMethod_ChangesState()
    {
        // Arrange
        var softSwitch = new SoftSwitch("80STORE");

        // Act
        softSwitch.Set(true);

        // Assert
        Assert.True(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_GetMethod_ReturnsCurrentState()
    {
        // Arrange
        var softSwitch = new SoftSwitch("TEXT", true);

        // Act
        var value = softSwitch.Get();

        // Assert
        Assert.True(value);
    }

    [Fact]
    public void SoftSwitch_Toggle_FlipsState()
    {
        // Arrange
        var softSwitch = new SoftSwitch("MIXED");
        softSwitch.Set(true);

        // Act
        softSwitch.Toggle();

        // Assert
        Assert.False(softSwitch.Value);
    }

    [Fact]
    public void SoftSwitch_ToString_IncludesNameAndValue()
    {
        // Arrange
        var softSwitch = new SoftSwitch("80STORE", true);

        // Act
        var result = softSwitch.ToString();

        // Assert
        Assert.Contains("80STORE", result);
        Assert.Contains("True", result);
    }

    #endregion

    #region SoftSwitches Collection Tests with SystemStatusProvider Integration

    [Fact]
    public void SoftSwitches_Constructor_InitializesFromStatusProvider()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        statusProvider.SetIntCxRom(true); // Set a non-default value
        statusProvider.SetText(false); // Change from default

        // Act
        var switches = new SoftSwitches(statusProvider);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.IntCxRom));
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Text));
    }

    [Fact]
    public void SoftSwitches_Set_UpdatesStatusProvider()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);

        // Act
        switches.Set(SoftSwitches.SoftSwitchId.RamRd, true);

        // Assert
        Assert.True(statusProvider.StateRamRd);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamRd));
    }

    [Fact]
    public void SoftSwitches_Set_OnlyCallsStatusProviderWhenValueChanges()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);
        int changeCount = 0;
        statusProvider.Changed += (s, e) => changeCount++;

        // Act - Set to same value multiple times
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);
        switches.Set(SoftSwitches.SoftSwitchId.Text, true);

        // Assert - Should only trigger once (from false to true)
        Assert.Equal(1, changeCount);
    }

    [Fact]
    public void SoftSwitches_QuietlySet_DoesNotUpdateStatusProvider()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);
        int changeCount = 0;
        statusProvider.Changed += (s, e) => changeCount++;

        // Act
        switches.QuietlySet(SoftSwitches.SoftSwitchId.HiRes, true);

        // Assert
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HiRes));
        Assert.False(statusProvider.StateHiRes); // Status provider not updated
        Assert.Equal(0, changeCount); // No change event fired
    }

    [Fact]
    public void SoftSwitches_ResetAllSwitches_ResetsToDefaults()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);
        switches.Set(SoftSwitches.SoftSwitchId.Text, false);
        switches.Set(SoftSwitches.SoftSwitchId.Mixed, true);
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);

        // Act
        switches.ResetAllSwitches();

        // Assert - All should be false except IntCxRom
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Text));
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Mixed));
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.HiRes));
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.IntCxRom));
        
        // Verify status provider is also reset
        Assert.False(statusProvider.StateTextMode);
        Assert.False(statusProvider.StateMixed);
        Assert.False(statusProvider.StateHiRes);
        Assert.True(statusProvider.StateIntCxRom);
    }

    [Fact]
    public void SoftSwitches_AllSwitchIds_CanBeSetAndGet()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);

        // Act & Assert - Test all switches can be set and retrieved
        switches.Set(SoftSwitches.SoftSwitchId.Store80, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Store80));
        
        switches.Set(SoftSwitches.SoftSwitchId.RamRd, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamRd));
        
        switches.Set(SoftSwitches.SoftSwitchId.RamWrt, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamWrt));
        
        switches.Set(SoftSwitches.SoftSwitchId.IntCxRom, false);
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.IntCxRom));
        
        switches.Set(SoftSwitches.SoftSwitchId.AltZp, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.AltZp));
        
        switches.Set(SoftSwitches.SoftSwitchId.SlotC3Rom, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.SlotC3Rom));
        
        switches.Set(SoftSwitches.SoftSwitchId.Vid80, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Vid80));
        
        switches.Set(SoftSwitches.SoftSwitchId.AltChar, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.AltChar));
        
        switches.Set(SoftSwitches.SoftSwitchId.Text, false);
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Text));
        
        switches.Set(SoftSwitches.SoftSwitchId.Mixed, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Mixed));
        
        switches.Set(SoftSwitches.SoftSwitchId.Page2, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Page2));
        
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HiRes));
    }

    [Fact]
    public void SoftSwitches_VideoModeSwitches_UpdateStatusProviderCorrectly()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);

        // Act - Set video mode switches
        switches.Set(SoftSwitches.SoftSwitchId.Text, false);
        switches.Set(SoftSwitches.SoftSwitchId.Mixed, true);
        switches.Set(SoftSwitches.SoftSwitchId.Page2, true);
        switches.Set(SoftSwitches.SoftSwitchId.HiRes, true);

        // Assert - Verify both switches and status provider
        Assert.False(switches.Get(SoftSwitches.SoftSwitchId.Text));
        Assert.False(statusProvider.StateTextMode);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Mixed));
        Assert.True(statusProvider.StateMixed);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Page2));
        Assert.True(statusProvider.StatePage2);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HiRes));
        Assert.True(statusProvider.StateHiRes);
    }

    [Fact]
    public void SoftSwitches_MemoryConfigSwitches_UpdateStatusProviderCorrectly()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);

        // Act - Set memory configuration switches
        switches.Set(SoftSwitches.SoftSwitchId.Store80, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamRd, true);
        switches.Set(SoftSwitches.SoftSwitchId.RamWrt, true);
        switches.Set(SoftSwitches.SoftSwitchId.AltZp, true);

        // Assert - Verify both switches and status provider
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Store80));
        Assert.True(statusProvider.State80Store);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamRd));
        Assert.True(statusProvider.StateRamRd);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.RamWrt));
        Assert.True(statusProvider.StateRamWrt);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.AltZp));
        Assert.True(statusProvider.StateAltZp);
    }

    [Fact]
    public void SoftSwitches_LanguageCardSwitches_UpdateStatusProviderCorrectly()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);

        // Act - Set language card switches
        switches.Set(SoftSwitches.SoftSwitchId.Bank1, true);
        switches.Set(SoftSwitches.SoftSwitchId.HighRead, true);
        switches.Set(SoftSwitches.SoftSwitchId.HighWrite, true);
        switches.Set(SoftSwitches.SoftSwitchId.PreWrite, true);

        // Assert - Verify both switches and status provider
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.Bank1));
        Assert.True(statusProvider.StateUseBank1);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HighRead));
        Assert.True(statusProvider.StateHighRead);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.HighWrite));
        Assert.True(statusProvider.StateHighWrite);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.PreWrite));
        Assert.True(statusProvider.StatePreWrite);
    }

    [Fact]
    public void SoftSwitches_AnnunciatorSwitches_UpdateStatusProviderCorrectly()
    {
        // Arrange
        var statusProvider = new SystemStatusProvider();
        var switches = new SoftSwitches(statusProvider);

        // Act - Set all annunciator switches
        switches.Set(SoftSwitches.SoftSwitchId.An0, true);
        switches.Set(SoftSwitches.SoftSwitchId.An1, true);
        switches.Set(SoftSwitches.SoftSwitchId.An2, true);
        switches.Set(SoftSwitches.SoftSwitchId.An3, true);

        // Assert - Verify both switches and status provider
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An0));
        Assert.True(statusProvider.StateAnn0);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An1));
        Assert.True(statusProvider.StateAnn1);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An2));
        Assert.True(statusProvider.StateAnn2);
        
        Assert.True(switches.Get(SoftSwitches.SoftSwitchId.An3));
        Assert.True(statusProvider.StateAnn3_DGR);
    }

    #endregion
}
