using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Pandowdy.EmuCore.DataTypes;
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore.Services;

/// <summary>
/// Thread-safe implementation of <see cref="ITelemetryAggregator"/> for device telemetry.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Singleton Pattern:</strong> This class should be registered as a singleton in the
/// DI container. All devices share the same aggregator instance, ensuring messages from all
/// sources flow through a single stream.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong> ID allocation uses <see cref="Interlocked"/> methods
/// for lock-free atomic increments. The underlying <see cref="Subject{T}"/> handles
/// concurrent Publish calls safely.
/// </para>
/// <para>
/// <strong>Memory:</strong> The stream does not replay messages to late subscribers.
/// Each subscriber receives only messages published after subscription.
/// </para>
/// <para>
/// <strong>Disposal:</strong> The internal Subject is not explicitly disposed. In a typical
/// application lifecycle, the aggregator lives for the entire session. If explicit cleanup
/// is needed, implement IDisposable and complete the subject.
/// </para>
/// </remarks>
public sealed class TelemetryAggregator : ITelemetryAggregator
{
    /// <summary>
    /// Counter for generating unique telemetry IDs.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Interlocked"/> for thread-safe ID generation.
    /// Starting at 0 means first ID will be 1.
    /// </remarks>
    private int _nextId;
    
    /// <summary>
    /// The reactive subject that acts as both observer and observable.
    /// </summary>
    /// <remarks>
    /// Subject is used rather than ReplaySubject because telemetry is real-time;
    /// late subscribers don't need historical messages.
    /// </remarks>
    private readonly Subject<TelemetryMessage> _subject;
    
    /// <summary>
    /// Cached observable wrapper to prevent direct access to the subject.
    /// </summary>
    private readonly IObservable<TelemetryMessage> _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryAggregator"/> class.
    /// </summary>
    public TelemetryAggregator()
    {
        _subject = new Subject<TelemetryMessage>();
        _stream = _subject.AsObservable();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Thread-safe via <see cref="Interlocked"/>.
    /// </remarks>
    public TelemetryId CreateId(string category)
    {
        ArgumentNullException.ThrowIfNull(category);
        
        int id = Interlocked.Increment(ref _nextId);
        return new TelemetryId(id, category);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Thread-safe via the underlying Subject implementation.
    /// Messages are delivered synchronously to all subscribers.
    /// </remarks>
    public void Publish(TelemetryMessage message)
    {
        _subject.OnNext(message);
    }

    /// <inheritdoc />
    public IObservable<TelemetryMessage> Stream => _stream;
}
