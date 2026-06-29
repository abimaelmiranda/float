using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Models;
using Float.UI.Platform;

namespace Float.UI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    public event EventHandler? CloseRequested;

    // Container Defaults
    [ObservableProperty] public partial string DefaultRegistry { get; set; } = "docker";
    [ObservableProperty] public partial string DefaultArchitecture { get; set; } = "arm64";
    [ObservableProperty] public partial string DefaultCpuCount { get; set; } = "";
    [ObservableProperty] public partial string DefaultMemory { get; set; } = "";
    [ObservableProperty] public partial bool DefaultKeepAlive { get; set; }
    [ObservableProperty] public partial bool StartImmediately { get; set; } = true;

    // Container Behavior
    [ObservableProperty] public partial bool ForceDeleteRunning { get; set; }
    [ObservableProperty] public partial string StopTimeoutSeconds { get; set; } = "5";

    // Application
    [ObservableProperty] public partial bool CloseToTray { get; set; } = true;
    [ObservableProperty] public partial bool StopEngineOnQuit { get; set; } = true;
    [ObservableProperty] public partial bool ShowInDock { get; set; }

    public string[] RegistryOptions { get; } = ["docker", "ghcr"];
    public string[] ArchitectureOptions { get; } = ["arm64", "amd64"];

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        Load();
    }

    [RelayCommand]
    private void Save()
    {
        _ = int.TryParse(StopTimeoutSeconds, out var timeout);
        _settingsService.Save(new AppSettings
        {
            DefaultRegistry = DefaultRegistry,
            DefaultArchitecture = DefaultArchitecture,
            DefaultCpuCount = DefaultCpuCount,
            DefaultMemory = DefaultMemory,
            DefaultKeepAlive = DefaultKeepAlive,
            StartImmediately = StartImmediately,
            ForceDeleteRunning = ForceDeleteRunning,
            StopTimeoutSeconds = timeout > 0 ? timeout : 5,
            CloseToTray = CloseToTray,
            StopEngineOnQuit = StopEngineOnQuit,
            ShowInDock = ShowInDock,
        });
        if (OperatingSystem.IsMacOS())
            MacDockHelper.SetShowInDock(ShowInDock);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    public void Load()
    {
        var s = _settingsService.Get();
        DefaultRegistry = s.DefaultRegistry;
        DefaultArchitecture = s.DefaultArchitecture;
        DefaultCpuCount = s.DefaultCpuCount;
        DefaultMemory = s.DefaultMemory;
        DefaultKeepAlive = s.DefaultKeepAlive;
        StartImmediately = s.StartImmediately;
        ForceDeleteRunning = s.ForceDeleteRunning;
        StopTimeoutSeconds = s.StopTimeoutSeconds.ToString();
        CloseToTray = s.CloseToTray;
        StopEngineOnQuit = s.StopEngineOnQuit;
        ShowInDock = s.ShowInDock;
    }
}
