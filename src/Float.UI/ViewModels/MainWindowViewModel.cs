using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.UI.ViewModels.Setup;
using Float.UI.ViewModels.Wizards;

namespace Float.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardVm;
    private readonly ImagesViewModel _imagesVm;
    private readonly VolumesViewModel _volumesVm;
    private readonly MigrationWizardViewModel _migrationVm;
    private readonly CreateContainerWizardViewModel _createContainerVm;
    private readonly CreateVolumeWizardViewModel _createVolumeVm;
    private readonly SettingsViewModel _settingsVm;
    private readonly IEngineProvisioner _provisioner;
    private readonly ISettingsService _settingsService;
    private readonly DispatcherTimer _statusTimer;
    private readonly DispatcherTimer _notificationTimer;

    [ObservableProperty] public partial ViewModelBase CurrentPageViewModel { get; set; }
    [ObservableProperty] public partial bool ShowingContainers { get; set; }
    [ObservableProperty] public partial bool ShowingImages { get; set; }
    [ObservableProperty] public partial bool ShowingVolumes { get; set; }
    [ObservableProperty] public partial bool ShowingMigration { get; set; }
    [ObservableProperty] public partial bool ShowingSettings { get; set; }
    [ObservableProperty] public partial bool IsSetupMode { get; set; }
    [ObservableProperty] public partial bool IsEngineRunning { get; set; }
    [ObservableProperty] public partial bool IsEngineStarting { get; set; }
    [ObservableProperty] public partial bool IsDarkTheme { get; set; }
    [ObservableProperty] public partial bool IsNotificationVisible { get; set; }
    [ObservableProperty] public partial string NotificationTitle { get; set; } = "";
    [ObservableProperty] public partial string NotificationMessage { get; set; } = "";

    public string EngineStatusLabel => IsEngineStarting ? "Starting…"
                                     : IsEngineRunning  ? "Engine running"
                                                        : "Engine stopped";
    public string ThemeToggleLabel => IsDarkTheme ? "Light mode" : "Dark mode";

    public MainWindowViewModel(
        IEngineProvisioner engineProvisioner,
        ISettingsService settingsService,
        DashboardViewModel dashboardVm,
        ImagesViewModel imagesVm,
        VolumesViewModel volumesVm,
        MigrationWizardViewModel migrationVm,
        EngineSetupViewModel engineSetupVm,
        CreateContainerWizardViewModel createContainerVm,
        CreateVolumeWizardViewModel createVolumeVm,
        SettingsViewModel settingsVm)
    {
        _provisioner = engineProvisioner;
        _settingsService = settingsService;
        _settingsVm = settingsVm;
        settingsVm.CloseRequested += (_, _) => ReturnToDashboard();
        _dashboardVm = dashboardVm;
        _imagesVm = imagesVm;
        _volumesVm = volumesVm;
        _migrationVm = migrationVm;
        _createContainerVm = createContainerVm;
        _createVolumeVm = createVolumeVm;

        dashboardVm.RequestCreateContainer += OnRequestCreateContainer;
        dashboardVm.OperationFailed += OnDashboardOperationFailed;
        dashboardVm.OperationSucceeded += (_, msg) => ShowNotification("Done", msg);
        imagesVm.OperationFailed += OnImagesOperationFailed;
        volumesVm.OperationFailed += OnVolumesOperationFailed;
        volumesVm.RequestCreateVolume += OnRequestCreateVolume;
        migrationVm.MigrationCompleted += OnMigrationCompleted;
        createContainerVm.ContainerCreated += OnContainerCreated;
        createContainerVm.Cancelled += OnCreateContainerCancelled;
        createVolumeVm.VolumeCreated += OnVolumeCreated;
        createVolumeVm.Cancelled += OnCreateVolumeCancelled;

        IsDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += (_, _) =>
                IsDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _statusTimer.Tick += async (_, _) => await RefreshEngineStatusAsync().ConfigureAwait(false);

        _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _notificationTimer.Tick += (_, _) => HideNotification();

        if (!engineProvisioner.IsEngineInstalled().GetAwaiter().GetResult())
        {
            IsSetupMode = true;
            engineSetupVm.SetupCompleted += OnSetupCompleted;
            CurrentPageViewModel = engineSetupVm;
            return;
        }

        CurrentPageViewModel = _dashboardVm;
        ShowingContainers = true;
        ShowingImages = false;
        ShowingVolumes = false;
        _ = InitEngineAsync();
    }

    private async Task InitEngineAsync()
    {
        await RefreshEngineStatusAsync().ConfigureAwait(false);

        if (!IsEngineRunning)
        {
            IsEngineStarting = true;
            var result = await _provisioner.StartEngineAsync().ConfigureAwait(false);
            if (result.IsFailure)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                    ShowNotification("Engine start failed", result.Failure.Message ?? "Unknown error"));
            }
            IsEngineStarting = false;

            await RefreshEngineStatusAsync().ConfigureAwait(false);
        }

        _statusTimer.Start();
        _ = _dashboardVm.RefreshAsync();
    }

    private async Task RefreshEngineStatusAsync()
    {
        var running = await _provisioner.IsEngineRunningAsync().ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsEngineRunning = running;
            OnPropertyChanged(nameof(EngineStatusLabel));
        });
    }

    public async Task ShutdownAsync()
    {
        _statusTimer.Stop();
        if (IsEngineRunning && _settingsService.Get().StopEngineOnQuit)
            await _provisioner.StopEngineAsync().ConfigureAwait(false);
    }

    private void OnSetupCompleted(object? sender, EventArgs e)
    {
        IsSetupMode = false;
        ShowingContainers = true;
        ShowingImages = false;
        ShowingVolumes = false;
        CurrentPageViewModel = _dashboardVm;
        _ = InitEngineAsync();
    }

    private void OnRequestCreateContainer(object? sender, EventArgs e)
    {
        HideNotification();
        CancelMigrationLoad();
        _createContainerVm.StartNewRun();
        ShowingContainers = false;
        ShowingImages = false;
        ShowingVolumes = false;
        ShowingMigration = false;
        CurrentPageViewModel = _createContainerVm;
    }

    private void OnContainerCreated(object? sender, EventArgs e) => ReturnToDashboard();

    private void OnCreateContainerCancelled(object? sender, EventArgs e) => ReturnToDashboard();

    private void OnRequestCreateVolume(object? sender, EventArgs e)
    {
        HideNotification();
        CancelMigrationLoad();
        _createVolumeVm.StartNewRun();
        ShowingContainers = false;
        ShowingImages = false;
        ShowingVolumes = false;
        ShowingMigration = false;
        CurrentPageViewModel = _createVolumeVm;
    }

    private void OnVolumeCreated(object? sender, EventArgs e) => ReturnToVolumes();

    private void OnCreateVolumeCancelled(object? sender, EventArgs e) => ReturnToVolumes();

    private void OnImagesOperationFailed(object? sender, string message)
    {
        ShowNotification("Image refresh failed", message);
    }

    private void OnVolumesOperationFailed(object? sender, string message)
    {
        ShowNotification("Volume refresh failed", message);
    }

    private void OnMigrationCompleted(object? sender, MigrationCleanupCompletedEventArgs e)
    {
        CancelMigrationLoad();
        ReturnToDashboard();
        ShowNotification(
            e.Failed == 0 ? "Migration complete" : "Migration complete with cleanup issues",
            e.Summary);
    }

    private void OnDashboardOperationFailed(object? sender, string message)
    {
        ShowNotification("Container operation failed", message);
    }

    private void ReturnToDashboard()
    {
        CancelMigrationLoad();
        ShowingContainers = true;
        ShowingImages = false;
        ShowingVolumes = false;
        ShowingMigration = false;
        ShowingSettings = false;
        CurrentPageViewModel = _dashboardVm;
        _ = _dashboardVm.RefreshAsync();
    }

    private void ReturnToVolumes()
    {
        CancelMigrationLoad();
        ShowingContainers = false;
        ShowingImages = false;
        ShowingVolumes = true;
        ShowingMigration = false;
        ShowingSettings = false;
        CurrentPageViewModel = _volumesVm;
        _ = _volumesVm.RefreshAsync();
    }

    private void ShowNotification(string title, string message)
    {
        _notificationTimer.Stop();
        NotificationTitle = title;
        NotificationMessage = message;
        IsNotificationVisible = true;
        _notificationTimer.Start();
    }

    [RelayCommand]
    private void HideNotification()
    {
        _notificationTimer.Stop();
        IsNotificationVisible = false;
        NotificationTitle = "";
        NotificationMessage = "";
    }

    [RelayCommand]
    private void NewContainer() => OnRequestCreateContainer(this, EventArgs.Empty);

    [RelayCommand]
    private void NewVolume() => OnRequestCreateVolume(this, EventArgs.Empty);

    [RelayCommand]
    private void ShowContainers()
    {
        CancelMigrationLoad();
        ShowingContainers = true;
        ShowingImages = false;
        ShowingVolumes = false;
        ShowingMigration = false;
        ShowingSettings = false;
        CurrentPageViewModel = _dashboardVm;
        _ = _dashboardVm.RefreshAsync();
    }

    [RelayCommand]
    private async Task ShowImagesAsync()
    {
        HideNotification();
        CancelMigrationLoad();
        ShowingContainers = false;
        ShowingImages = true;
        ShowingVolumes = false;
        ShowingMigration = false;
        ShowingSettings = false;
        CurrentPageViewModel = _imagesVm;
        await _imagesVm.RefreshAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task ShowVolumesAsync()
    {
        HideNotification();
        CancelMigrationLoad();
        ShowingContainers = false;
        ShowingImages = false;
        ShowingVolumes = true;
        ShowingMigration = false;
        ShowingSettings = false;
        CurrentPageViewModel = _volumesVm;
        await _volumesVm.RefreshAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task ShowMigrationAsync()
    {
        HideNotification();
        CancelMigrationLoad();
        ShowingContainers = false;
        ShowingImages = false;
        ShowingVolumes = false;
        ShowingMigration = true;
        ShowingSettings = false;
        CurrentPageViewModel = _migrationVm;
        await _migrationVm.StartNewRunAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private void ShowSettings()
    {
        HideNotification();
        CancelMigrationLoad();
        _settingsVm.Load();
        ShowingContainers = false;
        ShowingImages = false;
        ShowingVolumes = false;
        ShowingMigration = false;
        ShowingSettings = true;
        CurrentPageViewModel = _settingsVm;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var nextTheme = IsDarkTheme ? ThemeVariant.Light : ThemeVariant.Dark;
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = nextTheme;
        IsDarkTheme = nextTheme == ThemeVariant.Dark;
    }

    partial void OnIsEngineRunningChanged(bool value) =>
        OnPropertyChanged(nameof(EngineStatusLabel));

    partial void OnIsEngineStartingChanged(bool value) =>
        OnPropertyChanged(nameof(EngineStatusLabel));

    partial void OnIsDarkThemeChanged(bool value) =>
        OnPropertyChanged(nameof(ThemeToggleLabel));

    private void CancelMigrationLoad() => _migrationVm.CancelLoad();

}
