// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.UI.Models;
using System.Threading.Tasks;

namespace Pandowdy.UI.Interfaces;

/// <summary>
/// Service for managing disk drive state persistence.
/// </summary>
public interface IDriveStateService
{
    /// <summary>
    /// Loads drive state configuration from persistent storage.
    /// Returns empty configuration if file doesn't exist or fails to load.
    /// </summary>
    Task<DriveStateConfig> LoadDriveStateAsync();

    /// <summary>
    /// Saves drive state configuration to persistent storage.
    /// </summary>
    Task SaveDriveStateAsync(DriveStateConfig config);

    /// <summary>
    /// Gets the file path where drive state is stored.
    /// </summary>
    string GetDriveStateFilePath();

    /// <summary>
    /// Captures current drive states from the emulator and saves them.
    /// </summary>
    Task CaptureDriveStateAsync();

    /// <summary>
    /// Loads drive state from persistent storage and restores disk images to drives.
    /// </summary>
    Task LoadAndRestoreDriveStateAsync();
}
