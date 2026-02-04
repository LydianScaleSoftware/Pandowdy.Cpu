// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Avalonia.Controls;

namespace Pandowdy.UI.Controls;

/// <summary>
/// Status bar control container for displaying application status information.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong> Provides a full-width status bar at the bottom of the main window,
/// similar to IDE status bars. Contains the CPU status panel and can be extended with additional
/// status items in the future.
/// </para>
/// <para>
/// <strong>Design Pattern:</strong> Uses a Grid layout with columns for flexible addition of
/// status widgets. The CPU status panel takes up available space (Width="*"), while future
/// status items can use Width="Auto" for fixed-width content.
/// </para>
/// <para>
/// <strong>Future Extensions:</strong>
/// <list type="bullet">
/// <item>Emulator status messages ("Ready", "Running", "Paused")</item>
/// <item>Current emulation speed (MHz display)</item>
/// <item>Frame rate indicator</item>
/// <item>Disk activity indicator</item>
/// <item>Memory usage display</item>
/// </list>
/// </para>
/// </remarks>
public partial class StatusBar : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusBar"/> class.
    /// </summary>
    public StatusBar()
    {
        InitializeComponent();
    }
}
