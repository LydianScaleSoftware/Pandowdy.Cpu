// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Pandowdy.UI.Controls;

/// <summary>
/// Code-behind for DiskCardPanel control.
/// </summary>
/// <remarks>
/// Displays a disk controller card with its header (slot + card name) and
/// contains 1-2 DiskStatusWidget children representing individual drives.
/// </remarks>
public partial class DiskCardPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskCardPanel"/> class.
    /// </summary>
    public DiskCardPanel()
    {
        InitializeComponent();  
        AvaloniaXamlLoader.Load(this);
    }
}
