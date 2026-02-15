// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.UI.Models;
using System.Threading.Tasks;

namespace Pandowdy.UI.Interfaces;

/// <summary>
/// Service for loading and saving application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from persistent storage.
    /// Returns default settings if file doesn't exist or fails to load.
    /// </summary>
    Task<PandowdySettings> LoadSettingsAsync();

    /// <summary>
    /// Saves settings to persistent storage.
    /// </summary>
    Task SaveSettingsAsync(PandowdySettings settings);

    /// <summary>
    /// Gets the file path where settings are stored.
    /// </summary>
    string GetSettingsFilePath();
}
