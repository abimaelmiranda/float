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
    private readonly MigrationWizardViewModel _migrationVm;
    private readonly CreateContainerWizardViewModel _createContainerVm;
    private readonly IEngineProvisioner _provisioner;
    private readonly DispatcherTimer _statusTimer;

    [ObservableProperty] public partial ViewModelBase CurrentPageViewModel { get; set; }
    [ObservableProperty] public partial bool ShowingContainers { get; set; }
    [ObservableProperty] public partial bool ShowingMigration { get; set; }
    [ObservableProperty] public partial bool IsSetupMode { get; set; }
    [ObservableProperty] public partial bool IsEngineRunning { get; set; }
    [ObservableProperty] public partial bool IsEngineStarting { get; set; }
    [ObservableProperty] public partial bool IsDarkTheme { get; set; }

    public string EngineStatusLabel => IsEngineStarting ? "Starting…"
                                     : IsEngineRunning  ? "Engine running"
                                                        : "Engine stopped";
    public string ThemeToggleLabel => IsDarkTheme ? "Light mode" : "Dark mode";

    public MainWindowViewModel(
        IEngineProvisioner engineProvisioner,
        DashboardViewModel dashboardVm,
        MigrationWizardViewModel migrationVm,
        EngineSetupViewModel engineSetupVm,
        CreateContainerWizardViewModel createContainerVm)
    {
        _provisioner = engineProvisioner;
        _dashboardVm = dashboardVm;
        _migrationVm = migrationVm;
        _createContainerVm = createContainerVm;

        dashboardVm.RequestCreateContainer += OnRequestCreateContainer;
        createContainerVm.ContainerCreated += OnContainerCreated;
        createContainerVm.Cancelled += OnCreateContainerCancelled;

        IsDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
        if (Application.Current is { } app)
            app.ActualThemeVariantChanged += (_, _) =>
                IsDarkTheme = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _statusTimer.Tick += async (_, _) => await RefreshEngineStatusAsync().ConfigureAwait(false);

        if (!engineProvisioner.IsEngineInstalled())
        {
            IsSetupMode = true;
            engineSetupVm.SetupCompleted += OnSetupCompleted;
            CurrentPageViewModel = engineSetupVm;
            return;
        }

        CurrentPageViewModel = _dashboardVm;
        ShowingContainers = true;
        _ = InitEngineAsync();
    }

    private async Task InitEngineAsync()
    {
        await RefreshEngineStatusAsync().ConfigureAwait(false);

        if (!IsEngineRunning)
        {
            IsEngineStarting = true;
            try   { await _provisioner.StartEngineAsync().ConfigureAwait(false); }
            catch (Exception) { /* start failed; status refreshed below */ }
            finally { IsEngineStarting = false; }
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
        if (IsEngineRunning)
            await _provisioner.StopEngineAsync().ConfigureAwait(false);
    }

    private void OnSetupCompleted(object? sender, EventArgs e)
    {
        IsSetupMode = false;
        ShowingContainers = true;
        CurrentPageViewModel = _dashboardVm;
        _ = InitEngineAsync();
    }

    private void OnRequestCreateContainer(object? sender, EventArgs e)
    {
        ShowingContainers = false;
        ShowingMigration = false;
        CurrentPageViewModel = _createContainerVm;
    }

    private void OnContainerCreated(object? sender, EventArgs e) => ReturnToDashboard();

    private void OnCreateContainerCancelled(object? sender, EventArgs e) => ReturnToDashboard();

    private void ReturnToDashboard()
    {
        ShowingContainers = true;
        ShowingMigration = false;
        CurrentPageViewModel = _dashboardVm;
        _ = _dashboardVm.RefreshAsync();
    }

    [RelayCommand]
    private void ShowContainers()
    {
        ShowingContainers = true;
        ShowingMigration = false;
        CurrentPageViewModel = _dashboardVm;
        _ = _dashboardVm.RefreshAsync();
    }

    [RelayCommand]
    private void ShowMigration()
    {
        ShowingContainers = false;
        ShowingMigration = true;
        CurrentPageViewModel = _migrationVm;
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
}
