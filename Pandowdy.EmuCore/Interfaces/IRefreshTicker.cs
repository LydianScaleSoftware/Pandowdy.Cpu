using System;

namespace Pandowdy.EmuCore.Interfaces;

/// <summary>
/// Provides a 60 Hz refresh signal for UI-driven updates.
/// Used to drive frame rendering at approximately 60 fps.
/// </summary>
public interface IRefreshTicker
{
    /// <summary>
    /// Observable stream that emits timestamps at approximately 60 Hz.
    /// </summary>
    IObservable<DateTime> Stream { get; }
    
    /// <summary>
    /// Starts the refresh ticker.
    /// </summary>
    void Start();
    
    /// <summary>
    /// Stops the refresh ticker.
    /// </summary>
    void Stop();
}
