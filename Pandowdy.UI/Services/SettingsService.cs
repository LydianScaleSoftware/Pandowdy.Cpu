// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.UI.Interfaces;
using Pandowdy.UI.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Pandowdy.UI.Services;

/// <summary>
/// Service for loading and saving application settings to JSON file in %AppData%/LydianScaleSoftware/Pandowdy/.
/// </summary>
public class SettingsService : ISettingsService
{
    private const string SettingsFileName = "pandowdy-settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc/>
    public virtual string GetSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pandowdyPath = Path.Combine(appDataPath, "LydianScaleSoftware", "Pandowdy");
        return Path.Combine(pandowdyPath, SettingsFileName);
    }

    /// <inheritdoc/>
    public async Task<PandowdySettings> LoadSettingsAsync()
    {
        var filePath = GetSettingsFilePath();

        if (!File.Exists(filePath))
        {
            return new PandowdySettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var settings = JsonSerializer.Deserialize<PandowdySettings>(json);
            return settings ?? new PandowdySettings();
        }
        catch
        {
            // If deserialization fails, return default settings
            return new PandowdySettings();
        }
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync(PandowdySettings settings)
    {
        var filePath = GetSettingsFilePath();
        var directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
}
