using ReactiveUI;
using Pandowdy.Core;
using System.Reactive;

namespace Pandowdy.UI.ViewModels;

public sealed class MainWindowViewModel : ReactiveObject
{
    public EmulatorStateViewModel EmulatorState { get; }
    public ErrorLogViewModel ErrorLog { get; }
    public DisassemblyViewModel Disassembly { get; }

    public ReactiveCommand<Unit, Unit> PauseCommand { get; }
    public ReactiveCommand<Unit, Unit> ContinueCommand { get; }
    public ReactiveCommand<Unit, Unit> StepCommand { get; }

    private readonly IEmulatorState _emuState;

    public MainWindowViewModel(EmulatorStateViewModel emulatorState,
                               ErrorLogViewModel errorLog,
                               DisassemblyViewModel disassembly,
                               IEmulatorState emuState)
    {
        EmulatorState = emulatorState;
        ErrorLog = errorLog;
        Disassembly = disassembly;
        _emuState = emuState;

        PauseCommand = ReactiveCommand.Create(() => _emuState.RequestPause());
        ContinueCommand = ReactiveCommand.Create(() => _emuState.RequestContinue());
        StepCommand = ReactiveCommand.Create(() => _emuState.RequestStep());
    }
}
