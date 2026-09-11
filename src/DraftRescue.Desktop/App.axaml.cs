using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using DraftRescue.Desktop.ViewModels;
using DraftRescue.Desktop.Views;

namespace DraftRescue.Desktop;

public sealed partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = new MainWindowViewModel();
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
