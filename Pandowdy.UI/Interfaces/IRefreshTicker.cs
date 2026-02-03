// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using System;

namespace Pandowdy.UI.Interfaces;

/// <summary>
/// Provides a 60 Hz refresh signal for driving UI-based frame rendering and updates.
/// </summary>
/// <remarks>
/// This interface represents a timing source that emits periodic signals at approximately
/// 60 Hz (60 times per second), matching the typical video refresh rate of the Apple IIe
/// and modern displays. It's designed to be implemented by UI-layer components (such as
/// Avalonia's DispatcherTimer) to drive frame rendering in a way that's synchronized with
/// the UI thread and display refresh.
/// <para>
/// The ticker uses the Observable pattern (Rx.NET) to provide a reactive stream of
/// timestamps. Subscribers can use this stream to trigger frame generation, display
/// updates, and other time-based operations that need to occur at regular intervals.
/// </para>
/// <para>
/// <strong>Implementation Note:</strong> This interface is implemented in the
/// UI layer using platform-specific timing mechanisms (DispatcherTimer, animation
/// frames, etc.) that are only available in the UI context.
/// </para>
/// </remarks>
public interface IRefreshTicker
{
    /// <summary>
    /// Gets an observable stream that emits timestamps at approximately 60 Hz.
    /// </summary>
    /// <value>
    /// An <see cref="IObservable{DateTime}"/> that emits the current timestamp each time
    /// the ticker fires. Subscribers can observe this stream to receive periodic
    /// notifications for driving frame updates.
    /// </value>
    /// <remarks>
    /// The stream emits approximately 60 times per second when the ticker is running.
    /// Each emission provides the current <see cref="DateTime"/> at the time of the tick,
    /// which can be used for time-based calculations or simply as a signal to perform
    /// periodic work.
    /// <para>
    /// Typical usage pattern:
    /// <code>
    /// ticker.Stream
    ///     .ObserveOn(RxApp.MainThreadScheduler)
    ///     .Subscribe(_ => RenderFrame());
    /// </code>
    /// </para>
    /// <para>
    /// The actual timing may vary slightly based on system load and UI thread scheduling,
    /// but should maintain an average rate close to 60 Hz for smooth animation.
    /// </para>
    /// </remarks>
    IObservable<DateTime> Stream { get; }
    
    /// <summary>
    /// Starts the refresh ticker, causing it to begin emitting periodic signals.
    /// </summary>
    /// <remarks>
    /// After calling this method, the <see cref="Stream"/> will begin emitting timestamps
    /// at approximately 60 Hz. This method is typically called when the emulator starts
    /// running or when the UI becomes visible and ready to display frames.
    /// <para>
    /// Calling <see cref="Start"/> on an already-running ticker should be safe (idempotent).
    /// </para>
    /// </remarks>
    void Start();
    
    /// <summary>
    /// Stops the refresh ticker, causing it to cease emitting signals.
    /// </summary>
    /// <remarks>
    /// After calling this method, the <see cref="Stream"/> will stop emitting timestamps
    /// until <see cref="Start"/> is called again. This method is typically called when
    /// the emulator is paused or when the UI is no longer visible.
    /// <para>
    /// Calling <see cref="Stop"/> on an already-stopped ticker should be safe (idempotent).
    /// </para>
    /// </remarks>
    void Stop();
}
