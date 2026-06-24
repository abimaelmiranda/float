using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Float.Infrastructure.Common;
using Float.Infrastructure.Engines.AppleContainers;
using Float.UI.ViewModels;
using Float.UI.Views;

namespace Float.UI;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _mainWindowVm;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var processHost  = new ProcessHost();
            var fileSystem   = new FileSystemService();
            var provisioner  = new AppleContainersEngineProvisioner(fileSystem, processHost);

            _mainWindowVm = new MainWindowViewModel(provisioner);
            _mainWindow = new MainWindow { DataContext = _mainWindowVm };
            desktop.MainWindow = _mainWindow;

            desktop.Exit += async (_, _) => await _mainWindowVm.ShutdownAsync().ConfigureAwait(false);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    // ── Tray icon ────────────────────────────────────────────────────────
    private void OnTrayIconClicked(object? sender, EventArgs e) => ShowMainWindow();
    private void OnShowFloatClick(object? sender, EventArgs e)  => ShowMainWindow();

    // ── Native menu ──────────────────────────────────────────────────────
    private void OnAboutClick(object? sender, EventArgs e) { }

    private void OnQuitClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void OnNewContainerClick(object? sender, EventArgs e) => ShowMainWindow();
    private void OnMigrateClick(object? sender, EventArgs e)      => ShowMainWindow();
    private void OnRefreshClick(object? sender, EventArgs e) { }
}
