// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.UI.Models;
using Pandowdy.UI.Services;

namespace Pandowdy.UI.Tests.Services;

/// <summary>
/// Tests for SettingsService - application settings persistence.
/// </summary>
public class SettingsServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        // Use a random test directory to avoid conflicts
        _testDirectory = Path.Combine(Path.GetTempPath(), "PandowdyTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create service and override AppData path for testing
        _service = new TestSettingsService(_testDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory after each test
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures - they won't affect test results
            }
        }
    }

    /// <summary>
    /// Test-specific SettingsService that uses a custom directory instead of %AppData%.
    /// </summary>
    private class TestSettingsService(string testDirectory) : SettingsService
    {
        public override string GetSettingsFilePath()
        {
            return Path.Combine(testDirectory, "pandowdy-settings.json");
        }
    }

    #region GetSettingsFilePath Tests

    [Fact]
    public void GetSettingsFilePath_ReturnsExpectedPath()
    {
        // Act
        var path = _service.GetSettingsFilePath();

        // Assert
        Assert.NotNull(path);
        Assert.Contains("pandowdy-settings.json", path);
    }

    #endregion

    #region LoadSettingsAsync Tests

    [Fact]
    public async Task LoadSettingsAsync_WhenFileDoesNotExist_ReturnsDefaultSettings()
    {
        // Act
        var settings = await _service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("1.0", settings.Version);
        Assert.Null(settings.LastExportDirectory);
        Assert.Equal(300.0, settings.DiskPanelWidth);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileExists_LoadsSettings()
    {
        // Arrange - Save settings first
        var originalSettings = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\TestPath",
            DiskPanelWidth = 350.0
        };
        await _service.SaveSettingsAsync(originalSettings);

        // Act
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.NotNull(loadedSettings);
        Assert.Equal("1.0", loadedSettings.Version);
        Assert.Equal(@"C:\TestPath", loadedSettings.LastExportDirectory);
        Assert.Equal(350.0, loadedSettings.DiskPanelWidth);
    }

    [Fact]
    public async Task LoadSettingsAsync_WhenFileIsCorrupt_ReturnsDefaultSettings()
    {
        // Arrange - Write invalid JSON
        var filePath = _service.GetSettingsFilePath();
        await File.WriteAllTextAsync(filePath, "{ invalid json }");

        // Act
        var settings = await _service.LoadSettingsAsync();

        // Assert - Should return default settings on error
        Assert.NotNull(settings);
        Assert.Equal("1.0", settings.Version);
    }

    [Fact]
    public async Task LoadSettingsAsync_MultipleTimes_ReturnsConsistentData()
    {
        // Arrange
        var originalSettings = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\TestPath",
            DiskPanelWidth = 400.0
        };
        await _service.SaveSettingsAsync(originalSettings);

        // Act - Load multiple times
        var settings1 = await _service.LoadSettingsAsync();
        var settings2 = await _service.LoadSettingsAsync();
        var settings3 = await _service.LoadSettingsAsync();

        // Assert - All should match
        Assert.Equal(settings1.LastExportDirectory, settings2.LastExportDirectory);
        Assert.Equal(settings1.LastExportDirectory, settings3.LastExportDirectory);
        Assert.Equal(settings1.DiskPanelWidth, settings2.DiskPanelWidth);
        Assert.Equal(settings1.DiskPanelWidth, settings3.DiskPanelWidth);
    }

    #endregion

    #region SaveSettingsAsync Tests

    [Fact]
    public async Task SaveSettingsAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var settings = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\Test",
            DiskPanelWidth = 250.0
        };

        // Act
        await _service.SaveSettingsAsync(settings);

        // Assert
        var filePath = _service.GetSettingsFilePath();
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task SaveSettingsAsync_WritesValidJson()
    {
        // Arrange
        var settings = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\TestDirectory",
            DiskPanelWidth = 275.0
        };

        // Act
        await _service.SaveSettingsAsync(settings);

        // Assert - Read raw file and verify it's valid JSON
        var filePath = _service.GetSettingsFilePath();
        var json = await File.ReadAllTextAsync(filePath);
        Assert.Contains("\"Version\"", json);
        Assert.Contains("\"LastExportDirectory\"", json);
        Assert.Contains("\"DiskPanelWidth\"", json);
    }

    [Fact]
    public async Task SaveSettingsAsync_ThenLoad_PreservesAllProperties()
    {
        // Arrange
        var originalSettings = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\MyDocuments\DiskImages",
            DiskPanelWidth = 320.0
        };

        // Act
        await _service.SaveSettingsAsync(originalSettings);
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.Equal(originalSettings.Version, loadedSettings.Version);
        Assert.Equal(originalSettings.LastExportDirectory, loadedSettings.LastExportDirectory);
        Assert.Equal(originalSettings.DiskPanelWidth, loadedSettings.DiskPanelWidth);
    }

    [Fact]
    public async Task SaveSettingsAsync_WithNullLastExportDirectory_Succeeds()
    {
        // Arrange
        var settings = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = null,
            DiskPanelWidth = 300.0
        };

        // Act
        await _service.SaveSettingsAsync(settings);
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.Null(loadedSettings.LastExportDirectory);
        Assert.Equal(300.0, loadedSettings.DiskPanelWidth);
    }

    [Fact]
    public async Task SaveSettingsAsync_OverwritesExistingFile()
    {
        // Arrange
        var settings1 = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\OldPath",
            DiskPanelWidth = 300.0
        };
        var settings2 = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\NewPath",
            DiskPanelWidth = 350.0
        };

        // Act
        await _service.SaveSettingsAsync(settings1);
        await _service.SaveSettingsAsync(settings2);
        var loadedSettings = await _service.LoadSettingsAsync();

        // Assert
        Assert.Equal(@"C:\NewPath", loadedSettings.LastExportDirectory);
        Assert.Equal(350.0, loadedSettings.DiskPanelWidth);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Integration_SaveLoadCycle_PreservesData()
    {
        // Arrange
        var settings = new PandowdySettings
        {
            Version = "1.0",
            LastExportDirectory = @"C:\Users\TestUser\Documents\AppleII",
            DiskPanelWidth = 380.5
        };

        // Act
        await _service.SaveSettingsAsync(settings);
        var reloaded = await _service.LoadSettingsAsync();

        // Assert
        Assert.Equal(settings.Version, reloaded.Version);
        Assert.Equal(settings.LastExportDirectory, reloaded.LastExportDirectory);
        Assert.Equal(settings.DiskPanelWidth, reloaded.DiskPanelWidth);
    }

    [Fact]
    public async Task Integration_MultipleSaveLoadCycles_WorkCorrectly()
    {
        // Arrange
        var testPaths = new[]
        {
            @"C:\Path1",
            @"C:\Path2",
            @"C:\Path3"
        };

        // Act & Assert - Multiple save/load cycles
        for (int i = 0; i < testPaths.Length; i++)
        {
            var settings = new PandowdySettings
            {
                Version = "1.0",
                LastExportDirectory = testPaths[i],
                DiskPanelWidth = 300.0 + (i * 10)
            };

            await _service.SaveSettingsAsync(settings);
            var loaded = await _service.LoadSettingsAsync();

            Assert.Equal(testPaths[i], loaded.LastExportDirectory);
            Assert.Equal(300.0 + (i * 10), loaded.DiskPanelWidth);
        }
    }

    #endregion
}
