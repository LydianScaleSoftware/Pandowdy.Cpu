# Avalonia Quirks and Workarounds

This document captures non-obvious behaviors and workarounds discovered while developing the Pandowdy UI with Avalonia 11.x.

## Window Geometry Restoration - Maximized State

### The Problem

When restoring a window that was previously maximized, Avalonia has a specific order-of-operations requirement that isn't obvious from the documentation.

### Incorrect Approach ❌

```csharp
// This DOES NOT work correctly:
window.Position = new PixelPoint(savedLeft, savedTop);
window.Width = savedWidth;
window.Height = savedHeight;
window.WindowState = WindowState.Maximized;
```

**Result:** The maximized window appears at the saved position (e.g., 500, 300) instead of at (0, 0), and has strange positioning behavior.

### Correct Approach ✅

**For maximized windows, you must set `WindowState` BEFORE showing the window:**

```csharp
// In MainWindowFactory.Create() - BEFORE window.Show():
if (settings.IsMaximized)
{
    // Set restore bounds while window is still in Normal state
    window.Position = new PixelPoint(settings.Left, settings.Top);
    window.Width = settings.Width;
    window.Height = settings.Height;
    
    // Store a flag to maximize AFTER window is shown
    window.Tag = "ShouldMaximize";
}

// In MainWindow.OnOpened() - AFTER window is shown:
if (Tag is string tag && tag == "ShouldMaximize")
{
    WindowState = WindowState.Maximized;
    Tag = null;
}
```

**Result:** The maximized window appears at (0, 0) full-screen, and un-maximizing restores to the saved position/size correctly.

### Why This Works

1. **Setting Position/Size first** while window is in `Normal` state establishes the "restore bounds"
2. **Showing the window** commits these bounds to Avalonia's internal state
3. **Setting WindowState to Maximized** after the window is shown properly uses the restore bounds

If you set `WindowState = Maximized` before showing, Avalonia treats subsequent Position/Size changes differently (as maximized position rather than restore bounds).

### Implementation Details

See the following files for the complete implementation:
- `MainWindowFactory.cs` - Sets up restore bounds and flag
- `MainWindow.axaml.cs` - `OnOpened()` method handles deferred maximize
- `MainWindow.axaml.cs` - `OnWindowPropertyChanged()` uses circular buffer to track pre-maximize bounds
- `MainWindow.axaml.cs` - `OnClosing()` saves normal bounds when closing maximized

### Related Code

The window geometry persistence system also includes:
- **Circular buffer** - 5-entry timestamped history of window bounds (500ms threshold)
- **Multi-monitor support** - Validates saved positions are still on-screen
- **Windows 11 workaround** - Reapplies position 100ms after opening (but skips for maximized windows)

---

## Windows 11 Window Placement

### The Problem

Windows 11 has an aggressive "smart" window placement algorithm that runs after a window is shown, often overriding programmatically-set positions.

### Workaround

Set position BEFORE showing the window, then reapply it 100ms after opening:

```csharp
// In MainWindowFactory.Create() - BEFORE Show():
window.Position = new PixelPoint(settings.Left, settings.Top);

// In MainWindow.OnOpened():
if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) // Windows 11+
{
    Task.Delay(100).ContinueWith(_ =>
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Reapply position after Windows 11 does its thing
            if (!settings.IsMaximized)
            {
                Position = new PixelPoint(settings.Left, settings.Top);
            }
        });
    });
}
```

### Important

Skip this fallback for maximized windows - the restore bounds are already set correctly, and reapplying position would break them.

---

## Future Quirks

Add additional Avalonia quirks and workarounds here as they're discovered.

### Template

```markdown
## [Issue Name]

### The Problem
[Description of unexpected behavior]

### Workaround
[Code example showing solution]

### Why This Works
[Explanation of the underlying cause]
```

---

*Last Updated: 2025 - Avalonia 11.x*
