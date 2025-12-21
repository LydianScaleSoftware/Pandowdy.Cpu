using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;
using CommonUtil;
using DiskArc;
using Pandowdy.Core;
using Pandowdy.UI;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
//using static DiskArc.Defs;
using Pandowdy.UI.ViewModels; // ensure ViewModel type is visible
using System.Text.Json;
//using System.Diagnostics;
using ReactiveUI;
using ReactiveUI.Avalonia;
//using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Pandowdy.UI;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private readonly AppHook mAppHook = new(new SimpleMessageLog());
    private DiskReadTestTemp? mDiskReadTest;
    private string mLastDiskPath = "E:\\develop\\Pandowdy";

    private VA2M _machine; // acquired from DI
    private CancellationTokenSource? _emuCts;
    private Task? _emuTask;
    //private TextBox? _outputTextBox;


    private Menu? _mainMenu;
    private Apple2Display? _screen;
    private IRefreshTicker? _refreshTicker;
    private IDisposable? _refreshSub;
    private bool _menuPointerActive; // true while pointer is over the menu bar

    private bool _capsLockEnabled = true; // default ON
    public bool IsCapsLockEnabledForInput => _capsLockEnabled; // expose to Apple2TextScreen
    private Rect _lastNormalBounds;
    private PixelPoint _lastNormalPosition;

    public MainWindow()
    {
        InitializeComponent();

        var app = (App?)Application.Current;
        var services = app!.Services;
        ViewModel = services.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel;
        _machine = (VA2M)services.GetService(typeof(VA2M))!;
        _refreshTicker = (IRefreshTicker?)services.GetService(typeof(IRefreshTicker));

    //    _outputTextBox = this.FindControl<TextBox>("OutputTextBox");


        _mainMenu = this.FindControl<Menu>("MainMenu");
        //mDiskReadTest = new DiskReadTestTemp(mAppHook, AppendText, this);

        if (_mainMenu != null)
        {
            _mainMenu.PointerEntered += (_, __) => _menuPointerActive = true;
            _mainMenu.PointerExited += (_, __) => _menuPointerActive = false;
        }

        _screen = this.FindControl<Apple2Display>("ScreenDisplay");
        if (_screen != null)
        {
            _screen.AttachMachine(_machine);
            var frameProvider = (IFrameProvider)services.GetService(typeof(IFrameProvider))!;
            _screen.AttachFrameProvider(frameProvider);
            _screen.Focus();
        }

        this.WhenActivated(disposables =>
        {
            var vm = ViewModel ?? (DataContext as MainWindowViewModel);
            if (vm != null)
            {
                var s1 = vm.WhenAnyValue(x => x.ThrottleEnabled)
                    .Subscribe(v => _machine.ThrottleEnabled = v);
                disposables.Add(s1);
                var s2 = vm.WhenAnyValue(x => x.CapsLockEnabled)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(v =>
                    {
                        _capsLockEnabled = v;
                    });
                disposables.Add(s2);
                var s3 = vm.WhenAnyValue(x => x.ShowScanLines)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(v =>
                    {
                        if (_screen != null)
                        {
                            _screen.ShowScanLines = v;
                            _screen.InvalidateVisual();
                        }
                    });
                disposables.Add(s3);
                var s4 = vm.WhenAnyValue(x => x.ForceMonochrome)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(v =>
                    {
                        if (_screen != null)
                        {
                            _screen.ForceMono = v;
                            _screen.InvalidateVisual();
                        }
                    });
                disposables.Add(s4);
                var s5 = vm.WhenAnyValue(x => x.DecreaseContrast)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(v =>
                    {
                        if (_screen != null)
                        {
                            _screen.UseNonLumaContrastMask = v;
                            _screen.InvalidateVisual();
                        }
                    });
                disposables.Add(s5);
                var s6 = vm.WhenAnyValue(x => x.MonoMixed)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(v =>
                    {
                        if (_screen != null)
                        {
                            _screen.DefringeMixedText = v;
                            _screen.InvalidateVisual();
                        }
                    });
                disposables.Add(s6);

                // Bridge emulator commands to actions
                var c1 = vm.StartEmu.Subscribe(_ => OnEmuStartClicked(this, new RoutedEventArgs()));
                disposables.Add(c1);
                var c2 = vm.StopEmu.Subscribe(_ => OnEmuStopClicked(this, new RoutedEventArgs()));
                disposables.Add(c2);
                var c3 = vm.ResetEmu.Subscribe(_ => OnEmuResetClicked(this, new RoutedEventArgs()));
                disposables.Add(c3);
                var c4 = vm.StepOnce.Subscribe(_ => OnEmuStepOnceClicked(this, new RoutedEventArgs()));
                disposables.Add(c4);
            }
        });

        RestoreSettingsFromConfigFile();

        // Track last normal bounds for saving/restoring unmaximized geometry
        _lastNormalBounds = Bounds;
        _lastNormalPosition = Position;
        this.GetObservable(Window.WindowStateProperty).Subscribe(state =>
        {
            if (state == WindowState.Normal)
            {
                _lastNormalBounds = Bounds;
                _lastNormalPosition = Position;
            }
        });
        this.GetObservable(Window.BoundsProperty).Subscribe(_ =>
        {
            if (WindowState == WindowState.Normal)
            {
                _lastNormalBounds = Bounds;
                _lastNormalPosition = Position;
            }
        });
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _screen?.Focus();
        if (_refreshTicker != null && _screen != null)
        {
            _refreshTicker.Start();
            _refreshSub = _refreshTicker.Stream
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    // Request screen refresh; other synced tasks can also hook here
                    _screen.RequestRefresh();
                    _machine.GenerateStatusData();
                });
        }
        Dispatcher.UIThread.Post(() => OnEmuStartClicked(this, new RoutedEventArgs()));
    }

    protected override void OnClosed(EventArgs e)
    {
        SaveSettingsToConfigFile();
        _refreshSub?.Dispose();
        _refreshSub = null;
        _refreshTicker?.Stop();
        StopEmulator();
        base.OnClosed(e);
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        if (!_menuPointerActive)
        {
            _screen?.Focus();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!_menuPointerActive)
        {
            _screen?.Focus();
        }
    }

    private bool IsAnyMenuOpen()
    {
        if (_mainMenu == null)
        {
            return false;
        }
        foreach (var item in _mainMenu.Items)
        {
            if (item is MenuItem mi && mi.IsSubMenuOpen)
            {
                return true;
            }
        }
        return false;
    }

    private void CloseAllMenus()
    {
        if (_mainMenu == null)
        {
            return;
        }
        foreach (var item in _mainMenu.Items)
        {
            if (item is MenuItem mi)
            {
                mi.IsSubMenuOpen = false;
            }
        }
        _menuPointerActive = false;
        _screen?.Focus();
    }

    private async void OnEmuStartClicked(object? sender, RoutedEventArgs e)
    {
        if (_emuCts != null)
        {
            return;
        }
        _machine.Reset();
        _emuCts = new CancellationTokenSource();
        var token = _emuCts.Token;
        _emuTask = Task.Run(async () =>
        {
            try
            {
                await _machine.RunAsync(token, 60).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        });
        _ = _emuTask.ContinueWith(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _emuCts?.Dispose();
                _emuCts = null;
                _emuTask = null;
            });
        });
    }

    private void OnEmuStopClicked(object? sender, RoutedEventArgs e) => StopEmulator();
    private void OnEmuResetClicked(object? sender, RoutedEventArgs e) => _machine.UserReset();

    private void OnEmuStepOnceClicked(object? sender, RoutedEventArgs e)
    {
        if (_emuCts != null)
        {
            return;
        }
        _machine.Clock();
    }

    private void RestoreSettingsFromConfigFile()
    {
        try
        {
            var path = GetConfigPath();
            if (!File.Exists(path))
            {
                return;
            }
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<SettingsConfig>(json);
            if (data == null)
            {
                return;
            }
            if (data.Width > 0 && data.Height > 0)
            {
                Width = data.Width;
                Height = data.Height;
            }
            if (ViewModel != null)
            {
                if (data.ShowScanLines.HasValue) { ViewModel.ShowScanLines = data.ShowScanLines.Value; }
                if (data.MonoMixed.HasValue) { ViewModel.MonoMixed = data.MonoMixed.Value; }
                if (data.ForceMonochrome.HasValue) { ViewModel.ForceMonochrome = data.ForceMonochrome.Value; }
                if (data.DecreaseContrast.HasValue) { ViewModel.DecreaseContrast = data.DecreaseContrast.Value; }
                if (data.ThrottleEnabled.HasValue) { ViewModel.ThrottleEnabled = data.ThrottleEnabled.Value; } else { ViewModel.ThrottleEnabled = true; }
            }
        }
        catch { }
    }

    private void SaveSettingsToConfigFile()
    {
        try
        {
            var data = new SettingsConfig
            {
                Width = (int)Width,
                Height = (int)Height,
                ShowScanLines = ViewModel?.ShowScanLines,
                MonoMixed = ViewModel?.MonoMixed,
                DecreaseContrast = ViewModel?.DecreaseContrast,
                ForceMonochrome = ViewModel?.ForceMonochrome,
                ThrottleEnabled = ViewModel?.ThrottleEnabled,
            };
#pragma warning disable CA1869 // Cache and reuse 'JsonSerializerOptions' instances
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
#pragma warning restore CA1869 // Cache and reuse 'JsonSerializerOptions' instances
            File.WriteAllText(GetConfigPath(), json);
        }
        catch { }
    }

    private void StopEmulator()
    {
        if (_emuCts == null)
        {
            return;
        }
        _emuCts.Cancel();
    }

    private void OnQuitClicked(object? sender, RoutedEventArgs e) => Close();
    
    //private void OnSelectAllClicked(object? sender, RoutedEventArgs e)
    //{
    //    if (_outputTextBox != null)
    //    {
    //        _outputTextBox.SelectAll();
    //        _outputTextBox.Focus();
    //    }
    //}
    //private async void OnTestDiskReadClicked(object? sender, RoutedEventArgs e)
    //{
    //    ClearText();
    //    mLastDiskPath = await mDiskReadTest!.HandleDiskRead(mLastDiskPath);
    //}
    //private void OnCopyClicked(object? sender, RoutedEventArgs e)
    //{
    //    if (_outputTextBox != null && !string.IsNullOrEmpty(_outputTextBox.SelectedText))
    //    {
    //        var clipboard = TopLevel.GetTopLevel(_outputTextBox)?.Clipboard;
    //        clipboard?.SetTextAsync(_outputTextBox.SelectedText);
    //    }
    //    else if (_outputTextBox != null && !string.IsNullOrEmpty(_outputTextBox.Text))
    //    {
    //        var clipboard = TopLevel.GetTopLevel(_outputTextBox)?.Clipboard;
    //        clipboard?.SetTextAsync(_outputTextBox.Text);
    //    }
    //}
    //private void OnClearTextClicked(object? sender, RoutedEventArgs e) => ClearText();

    private static string GetConfigPath()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(baseDir, "LydianScaleSoftware", "Pandowdy");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "settings.json");
    }

    private sealed class SettingsConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool? ShowScanLines { get; set; }
        public bool? MonoMixed { get; set; }
        public bool? DecreaseContrast { get; set; }
        public bool? ForceMonochrome { get; set; }
        public bool? ThrottleEnabled { get; set; }
    }

    //public void AppendText(string text)
    //{
    //    Dispatcher.UIThread.Post(() =>
    //    {
    //        if (_outputTextBox != null)
    //        {
    //            _outputTextBox.Text += text;
    //            _outputTextBox.CaretIndex = _outputTextBox.Text?.Length ?? 0;
    //        }
    //    });
    //}

    //public void SetText(string text)
    //{
    //    Dispatcher.UIThread.Post(() =>
    //    {
    //        if (_outputTextBox != null)
    //        {
    //            _outputTextBox.Text = text;
    //            _outputTextBox.CaretIndex = _outputTextBox.Text?.Length ?? 0;
    //        }
    //    });
    //}

    //public void ClearText()
    //{
    //    Dispatcher.UIThread.Post(() =>
    //    {
    //        if (_outputTextBox != null)
    //        {
    //            _outputTextBox.Text = string.Empty;
    //        }
    //    });
    //}

    private void OnMainWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_menuPointerActive || IsAnyMenuOpen())
        {
            if (HandleAccelerator(e))
            {
                e.Handled = true;
                return;
            }
            if (IsAnyMenuOpen())
            {
                CloseAllMenus();
            }
            if (!TryInjectSpecialKey(e))
            {
            }
            return;
        }
        if (HandleAccelerator(e))
        {
            e.Handled = true;
            return;
        }
        _screen?.Focus();
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.Key == Key.F1)
        {
            _machine.SetPushButton(0, false); // pushbutton released
            e.Handled = true;
        }
        else if (e.Key == Key.F2)
        {
            _machine.SetPushButton(1, false); // pushbutton 2 released
            e.Handled = true;
        }
        else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            _machine.SetPushButton(2, false); // pushbutton 3 (shift) released
            e.Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
        {
            _machine.SetPushButton(2, true); // pushbutton 3 (shift) pressed
            // do not mark handled so other shift combos still work
        }
    }

    private bool HandleAccelerator(KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Alt) != 0)
        {
            switch (e.Key)
            {
                //case Key.A:
                //    OnSelectAllClicked(this, new RoutedEventArgs());
                //    return true;
                //case Key.L:
                //    OnClearTextClicked(this, new RoutedEventArgs());
                //    return true;
                case Key.S:
                    // screen toggled via VM binding
                    ViewModel?.ToggleScanLines.Execute().Subscribe();
                    return true;
                case Key.M:
                    ViewModel?.ToggleMonochrome.Execute().Subscribe();
                    return true;
                case Key.D:
                    ViewModel?.ToggleDecreaseContrast.Execute().Subscribe();
                    return true;
                case Key.X:
                    ViewModel?.ToggleMonoMixed.Execute().Subscribe();
                    return true;
                case Key.F4:
                    Close();
                    return true;
            }
        }
        switch (e.Key)
        {
            case Key.F1:
                _machine.SetPushButton(0, true); // pushbutton pressed
                return true;
            case Key.F2:
                _machine.SetPushButton(1, true); // pushbutton 2 pressed
                return true;
            case Key.F5:
                ViewModel?.StartEmu.Execute().Subscribe();
                return true;
            case Key.F6:
                ViewModel?.ToggleCapsLock.Execute().Subscribe();
                return true;
            case Key.F7:
                ViewModel?.ToggleThrottle.Execute().Subscribe();
                return true;
            case Key.F10:
                ViewModel?.StepOnce.Execute().Subscribe();
                return true;
        }
        if (e.Key == Key.F5 && (e.KeyModifiers & KeyModifiers.Shift) != 0)
        {
            ViewModel?.StopEmu.Execute().Subscribe();
            return true;
        }
        if ((e.KeyModifiers & KeyModifiers.Control) != 0 && (e.KeyModifiers & KeyModifiers.Shift) != 0 && e.Key == Key.D2)
        {
            _machine.InjectKey(0x00);
            e.Handled = true;
            return true;
        }
        // Ctrl+F12 triggers reset
        if (e.Key == Key.F12 && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            ViewModel?.ResetEmu.Execute().Subscribe();
            return true;
        }
        return false;
    }

    private bool TryInjectSpecialKey(KeyEventArgs e)
    {
        if ((e.KeyModifiers & KeyModifiers.Control) != 0 && e.Key >= Key.A && e.Key <= Key.Z)
        {
            byte ctrl = (byte)(e.Key - Key.A + 1);
            _machine.InjectKey((byte)(ctrl | 0x80));
            e.Handled = true;
            return true;
        }
        byte? ascii = e.Key switch
        {
            Key.Up => (byte)0x0B,
            Key.Down => (byte)0x0A,
            Key.Left => (byte)0x08,
            Key.Right => (byte)0x15,
            Key.Delete => (byte)0x7F,
            Key.Enter => (byte)'\r',
            Key.Tab => (byte)'\t',
            Key.Escape => (byte)0x1B,
            Key.Back => (e.KeyModifiers & KeyModifiers.Shift) != 0 ? (byte)0x7F : (byte)0x08,
            _ => null
        };
        if (ascii.HasValue)
        {
            _machine.InjectKey((byte)(ascii.Value | 0x80));
            e.Handled = true;
            return true;
        }
        return false;
    }
}
