using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using DraftRescue.Desktop.Persistence;
using DraftRescue.Desktop.ViewModels;
using DraftRescue.Desktop.Views;

namespace DraftRescue.Desktop;

public sealed partial class App : Avalonia.Application
{
    private DesktopPersistenceRuntime? _persistenceRuntime;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var persistence = DesktopPersistenceRuntime.StartDefaultAsync().GetAwaiter().GetResult();
            _persistenceRuntime = persistence.Runtime;
            desktop.Exit += (_, _) =>
            {
                _persistenceRuntime?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _persistenceRuntime = null;
            };

            var viewModel = new MainWindowViewModel(persistence.Availability);
            viewModel.ThemeChanged += (_, _) =>
            {
                RequestedThemeVariant = viewModel.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
            };
            RequestedThemeVariant = viewModel.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
