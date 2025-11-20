namespace Pandowdy.Core;

// Placeholder service interfaces & simple stub implementations.
// Incremental behavior will be added later without breaking existing UI.

public interface IFrameProvider {
    int Width { get; } // 80 row bytes per scanline
    int Height { get; } // 192 scanlines (24 rows * 8)
    bool IsGraphics { get; } // true if in graphics mode
    bool IsMixed { get; } // true if in mixed text/graphics mode
    event EventHandler? FrameAvailable; // raised after new frame committed
    byte[] GetFrame(); // returns current front buffer (length = Width*Height)
    byte[] BorrowWritable(); // returns back buffer for composition
    void CommitWritable(); // swap buffers & raise event
}

public interface IErrorProvider {
    IObservable<LogEvent> Events { get; }
    void Publish(LogEvent evt);
}

public interface IEmulatorState {
    IObservable<StateSnapshot> Stream { get; }
    StateSnapshot GetCurrent();
    void Update(StateSnapshot snapshot);
    void RequestPause();
    void RequestContinue();
    void RequestStep();
}

public interface IDisassemblyProvider {
    IObservable<DisassemblyUpdate> Updates { get; }
    Task<Line[]> QueryRange(AddressRange range);
    void Invalidate(AddressRange range);
    void SetHighlight(ushort pc);
}

public record StateSnapshot(ushort PC, byte SP, ulong Cycles, int? LineNumber, bool IsRunning, bool IsPaused);
public record LogEvent(DateTime Timestamp, string Severity, string Message, ushort? PC = null);
public record DisassemblyUpdate(AddressRange Range, IReadOnlyList<Line> Lines);
public record Line(ushort Address, string BytesHex, string Mnemonic, string Comment);
public record AddressRange(ushort Start, ushort End);

public sealed class FrameProvider : IFrameProvider {
    private const int W = 80;
    private const int H = 192;
    private byte[] _front = new byte[W * H];
    private byte[] _back = new byte[W * H];
    public int Width => W;
    public int Height => H;
    public event EventHandler? FrameAvailable;
    public bool IsGraphics { get; private set; } = false;
    public bool IsMixed { get; private set; } = false;
    public byte[] GetFrame() => _front;
    public byte[] BorrowWritable() => _back;
    public void CommitWritable() {
        // swap
        var tmp = _front;
        _front = _back;
        _back = tmp;
        FrameAvailable?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class ErrorProvider : IErrorProvider {
    private readonly System.Reactive.Subjects.Subject<LogEvent> _subject = new();
    public IObservable<LogEvent> Events => _subject;
    public void Publish(LogEvent evt) => _subject.OnNext(evt);
}

public sealed class EmulatorStateProvider : IEmulatorState {
    private readonly System.Reactive.Subjects.BehaviorSubject<StateSnapshot> _subject = new(new StateSnapshot(0,0,0,null,false,false));
    public IObservable<StateSnapshot> Stream => _subject;
    public StateSnapshot GetCurrent() => _subject.Value;
    public void Update(StateSnapshot snapshot) => _subject.OnNext(snapshot);
    public void RequestPause() { /* placeholder */ }
    public void RequestContinue() { /* placeholder */ }
    public void RequestStep() { /* placeholder */ }
}

public sealed class DisassemblyProvider : IDisassemblyProvider {
    private readonly System.Reactive.Subjects.Subject<DisassemblyUpdate> _updates = new();
    public IObservable<DisassemblyUpdate> Updates => _updates;
    public Task<Line[]> QueryRange(AddressRange range) => Task.FromResult(Array.Empty<Line>());
    public void Invalidate(AddressRange range) { }
    public void SetHighlight(ushort pc) { }
}
