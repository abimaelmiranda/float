using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
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
    private TrayIcon? _trayIcon;
    private UserNotificationService? _notificationService;

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
            services.AddSingleton<MigrationWizardViewModel>();
            services.AddSingleton<EngineSetupViewModel>();
            services.AddSingleton<CreateContainerWizardViewModel>();
            services.AddSingleton<MainWindowViewModel>();

            var provider = services.BuildServiceProvider();
            ViewLocator.Services = provider;

            _mainWindowVm = provider.GetRequiredService<MainWindowViewModel>();
            _mainWindow = new MainWindow { DataContext = _mainWindowVm };
            desktop.MainWindow = _mainWindow;

            notificationService.SetMainWindow(_mainWindow);

            desktop.Exit += async (_, _) => await _mainWindowVm.ShutdownAsync().ConfigureAwait(false);
        }

        base.OnFrameworkInitializationCompleted();

        _trayIcon = TrayIcon.GetIcons(this)?.FirstOrDefault();
        ActualThemeVariantChanged += (_, _) => UpdateTrayIcon();
        UpdateTrayIcon();
    }

    private void UpdateTrayIcon()
    {
        if (_trayIcon is null) return;
        var asset = ActualThemeVariant == ThemeVariant.Dark
            ? "avares://Float.UI/Assets/float_tray_dark.png"
            : "avares://Float.UI/Assets/float_tray_light.png";
        using var stream = AssetLoader.Open(new Uri(asset));
        _trayIcon.Icon = new WindowIcon(stream);
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
    private void OnSettingsClick(object? sender, EventArgs e) { }

    private void OnQuitClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void OnNewContainerClick(object? sender, EventArgs e) => ShowMainWindow();
    private void OnMigrateClick(object? sender, EventArgs e)      => ShowMainWindow();
    private void OnRefreshClick(object? sender, EventArgs e) { }

}
