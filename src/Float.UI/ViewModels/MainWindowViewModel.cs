using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.UI.ViewModels.Setup;
using Float.UI.ViewModels.Wizards;

namespace Float.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DashboardViewModel _dashboardVm = new();
    private readonly MigrationWizardViewModel _migrationVm = new();
    private readonly IEngineProvisioner _provisioner;
    private readonly DispatcherTimer _statusTimer;

    [ObservableProperty] public partial ViewModelBase CurrentPageViewModel { get; set; }
    [ObservableProperty] public partial bool ShowingContainers { get; set; }
    [ObservableProperty] public partial bool ShowingMigration { get; set; }
    [ObservableProperty] public partial bool IsSetupMode { get; set; }
    [ObservableProperty] public partial bool IsEngineRunning { get; set; }
    [ObservableProperty] public partial bool IsEngineStarting { get; set; }

    public string EngineStatusLabel => IsEngineStarting ? "Starting…"
                                     : IsEngineRunning  ? "Engine running"
                                                        : "Engine stopped";

    public MainWindowViewModel(IEngineProvisioner engineProvisioner)
    {
        _provisioner = engineProvisioner;

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _statusTimer.Tick += async (_, _) => await RefreshEngineStatusAsync().ConfigureAwait(false);

        if (!engineProvisioner.IsEngineInstalled())
        {
            IsSetupMode = true;
            var setupVm = new EngineSetupViewModel(engineProvisioner);
            setupVm.SetupCompleted += OnSetupCompleted;
            CurrentPageViewModel = setupVm;
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

    [RelayCommand]
    private void ShowContainers()
    {
        ShowingContainers = true;
        ShowingMigration = false;
        CurrentPageViewModel = _dashboardVm;
    }

    [RelayCommand]
    private void ShowMigration()
    {
        ShowingContainers = false;
        ShowingMigration = true;
        CurrentPageViewModel = _migrationVm;
    }

    partial void OnIsEngineRunningChanged(bool value) =>
        OnPropertyChanged(nameof(EngineStatusLabel));

    partial void OnIsEngineStartingChanged(bool value) =>
        OnPropertyChanged(nameof(EngineStatusLabel));
}
