using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using Pandowdy.UI.ViewModels;
using Pandowdy.Core;

namespace Pandowdy.UI;

public partial class App : Application
{
    public IServiceProvider Services { get; }

    public App(IServiceProvider services)
    {
        Services = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel;
            var machine = Services.GetService(typeof(VA2M)) as VA2M;
            var frameProvider = Services.GetService(typeof(IFrameProvider)) as IFrameProvider;
            var ticker = Services.GetService(typeof(IRefreshTicker)) as IRefreshTicker;
            var mainWindow = new MainWindow();
            mainWindow.InjectDependencies(vm!, machine!, frameProvider!, ticker);
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
