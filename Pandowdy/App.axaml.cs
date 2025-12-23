using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using Pandowdy.UI;
using Pandowdy.UI.ViewModels;
using Pandowdy.EmuCore;
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = DesktopServiceProvider.Current;
            var factory = services.GetService(typeof(IMainWindowFactory)) as IMainWindowFactory;
            
            if (factory == null)
            {
                throw new InvalidOperationException("IMainWindowFactory not registered in DI container.");
            }
            
            desktop.MainWindow = factory.Create();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

public static class DesktopServiceProvider
{
    private static IServiceProvider? _provider;

    public static void SetProvider(IServiceProvider provider)
    {
        _provider = provider;
    }

    public static IServiceProvider Current
    {
        get
        {
            if (_provider == null)
            {
                throw new InvalidOperationException("ServiceProvider has not been initialized.");
            }

            return _provider;
        }
    }
}
