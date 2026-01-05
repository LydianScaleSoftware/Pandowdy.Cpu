namespace Pandowdy.UI.Models;

/// <summary>
/// Represents saved window position, size, and state information.
/// </summary>
/// <remarks>
/// Used to persist and restore window layout across application sessions,
/// with support for multi-monitor configurations and DPI awareness.
/// </remarks>
public sealed class WindowSettings
{
    /// <summary>
    /// Gets or sets the X coordinate of the window's top-left corner in screen coordinates.
    /// </summary>
    public int Left { get; set; }
    
    /// <summary>
    /// Gets or sets the Y coordinate of the window's top-left corner in screen coordinates.
    /// </summary>
    public int Top { get; set; }
    
    /// <summary>
    /// Gets or sets the window width in pixels.
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Gets or sets the window height in pixels.
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Gets or sets whether the window was maximized when saved.
    /// </summary>
    public bool IsMaximized { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the monitor the window was on when saved.
    /// </summary>
    /// <remarks>
    /// Used to detect when a saved monitor is no longer available (disconnected).
    /// Falls back to primary monitor in that case.
    /// </remarks>
    public string? MonitorName { get; set; }
    
    /// <summary>
    /// Gets or sets the monitor's bounds when the window was saved.
    /// </summary>
    /// <remarks>
    /// Format: "X,Y,Width,Height" (e.g., "0,0,1920,1080" for primary monitor).
    /// Used to validate that the saved position is still valid if monitor configuration changed.
    /// </remarks>
    public string? MonitorBounds { get; set; }
}
