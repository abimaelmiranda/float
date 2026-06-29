using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Float.Core.Abstractions.Services;
using Float.Infrastructure;
using Float.UI.Services;
using Float.UI.ViewModels;
using Float.UI.ViewModels.Setup;
using Float.UI.ViewModels.Wizards;
using Float.UI.Views;

using Microsoft.Extensions.DependencyInjection;

namespace Float.UI;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _mainWindowVm;
    private UserNotificationService? _notificationService;
    private ISettingsService? _settingsService;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddFloatInfrastructure();

            var notificationService = new UserNotificationService();
            _notificationService = notificationService;
            services.AddSingleton<IUserNotificationService>(notificationService);

            services.AddSingleton<DashboardViewModel>();
            services.AddSingleton<ImagesViewModel>();
            services.AddSingleton<VolumesViewModel>();
            services.AddSingleton<MigrationWizardViewModel>();
            services.AddSingleton<EngineSetupViewModel>();
            services.AddSingleton<CreateContainerWizardViewModel>();
            services.AddSingleton<CreateVolumeWizardViewModel>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<RegistriesViewModel>();
            var provider = services.BuildServiceProvider();
            ViewLocator.Services = provider;

            _settingsService = provider.GetRequiredService<ISettingsService>();
            _mainWindowVm = provider.GetRequiredService<MainWindowViewModel>();
            _mainWindow = new MainWindow { DataContext = _mainWindowVm };
            desktop.MainWindow = _mainWindow;

            notificationService.SetMainWindow(_mainWindow);

            _mainWindow.Closing += (_, e) =>
            {
                if (_settingsService.Get().CloseToTray)
                {
                    e.Cancel = true;
                    _mainWindow.Hide();
                }
            };

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

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        ShowMainWindow();
        _mainWindowVm?.ShowSettingsCommand.Execute(null);
    }

    private void OnStartEngineClick(object? sender, EventArgs e) =>
        _mainWindowVm?.StartEngineCommand.Execute(null);

    private void OnStopEngineClick(object? sender, EventArgs e) =>
        _mainWindowVm?.StopEngineCommand.Execute(null);

    private void OnQuitClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }


}
