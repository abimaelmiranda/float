using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.UI.Resources;

namespace Float.UI.ViewModels.Wizards;

public partial class CreateContainerWizardViewModel : ViewModelBase
{
    private readonly IContainerCreator _creator;
    private readonly ISettingsService _settingsService;
    private CancellationTokenSource? _cts;
    private int _outputSession;

    public event EventHandler? ContainerCreated;
    public event EventHandler? Cancelled;
    public event Action<VolumeItem>? RequestFolderPick;

    // ── Navigation ───────────────────────────────────────────────────────

    [ObservableProperty] public partial int CurrentStep { get; set; } = 1;

    // Steps 1-5 are the wizard; step 6 is the dedicated creating screen
    public int TotalSteps => 5;

    public bool IsStep1 => CurrentStep == 1;
    public bool IsStep2 => CurrentStep == 2;
    public bool IsStep3 => CurrentStep == 3;
    public bool IsStep4 => CurrentStep == 4;
    public bool IsStep5 => CurrentStep == 5;
    public bool IsStep6 => CurrentStep == 6;

    // Used to show/hide the wizard header (progress bar hidden on step 6)
    public bool IsWizardStep => CurrentStep <= 5;

    public string StepTitle => CurrentStep switch
    {
        1 => UiStrings.ContainerImage,
        2 => UiStrings.PlatformResources,
        3 => UiStrings.PortsVolumes,
        4 => UiStrings.Environment,
        5 => UiStrings.ReviewCreate,
        6 => UiStrings.CreatingContainer,
        _ => ""
    };

    // ── Step 1 — Image ───────────────────────────────────────────────────

    [ObservableProperty] public partial string Registry { get; set; } = "docker";
    [ObservableProperty] public partial string ImageName { get; set; } = "";
    [ObservableProperty] public partial string Tag { get; set; } = "latest";

    // TODO: validate — no spaces, no uppercase, max 63 chars, only [a-z0-9_.-]
    [ObservableProperty] public partial string ContainerName { get; set; } = "";

    // ── Step 2 — Platform ────────────────────────────────────────────────

    [ObservableProperty] public partial bool EnableRosetta { get; set; }
    [ObservableProperty] public partial string Architecture { get; set; } = "arm64";
    [ObservableProperty] public partial string CpuCount { get; set; } = "";
    [ObservableProperty] public partial string Memory { get; set; } = "";
    [ObservableProperty] public partial bool KeepAlive { get; set; }

    partial void OnEnableRosettaChanged(bool value)
    {
        if (value) Architecture = "amd64";
        else if (Architecture == "amd64") Architecture = "arm64";
    }

    partial void OnArchitectureChanged(string value)
    {
        if (value == "arm64") EnableRosetta = false;
    }

    // ── Step 3 — Ports & Volumes ─────────────────────────────────────────

    public ObservableCollection<PortMappingItem> PortMappings { get; } = [];
    public ObservableCollection<VolumeItem> Volumes { get; } = [];

    // ── Step 4 — Environment ─────────────────────────────────────────────

    public ObservableCollection<EnvVarItem> EnvironmentVariables { get; } = [];

    // ── Step 5 — Command preview ──────────────────────────────────────────

    [ObservableProperty] public partial string GeneratedCommand { get; set; } = "";

    // ── Step 6 — Creation state ───────────────────────────────────────────

    [ObservableProperty] public partial bool IsCreating { get; set; }
    [ObservableProperty] public partial bool IsComplete { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string Output { get; set; } = "";
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";

    // ── Validation ───────────────────────────────────────────────────────

    // TODO: step 1 — warn when ContainerName has spaces or invalid chars
    // TODO: step 2 — validate CpuCount is a positive number when not empty
    // TODO: step 2 — validate Memory format (e.g. 512m, 2g)
    // TODO: step 3 — validate port numbers are 1-65535
    // TODO: step 3 — validate host paths exist before submitting
    public bool CanGoNext => CurrentStep switch
    {
        1 => !string.IsNullOrWhiteSpace(ImageName),
        _ => true
    };

    // ── Constructor ──────────────────────────────────────────────────────

    public string[] ArchitectureOptions { get; } = ["arm64", "amd64"];
    public string[] RegistryOptions { get; } = ["docker", "ghcr"];

    public CreateContainerWizardViewModel(IContainerCreator creator, ISettingsService settingsService)
    {
        _creator = creator;
        _settingsService = settingsService;
        LoadDefaults();
    }

    public void StartNewRun()
    {
        _cts?.Cancel();
        Reset();
    }

    // ── Navigation commands ──────────────────────────────────────────────

    [RelayCommand]
    private void Next()
    {
        if (CurrentStep < TotalSteps && CanGoNext)
            CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 1 && CurrentStep <= 5)
            CurrentStep--;
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        Reset();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    // ── List management ──────────────────────────────────────────────────

    [RelayCommand] private void AddPort() => PortMappings.Add(new PortMappingItem());
    [RelayCommand] private void RemovePort(PortMappingItem item) => PortMappings.Remove(item);

    [RelayCommand] private void AddVolume() => Volumes.Add(new VolumeItem());
    [RelayCommand] private void RemoveVolume(VolumeItem item) => Volumes.Remove(item);
    [RelayCommand] private void PickFolder(VolumeItem item) => RequestFolderPick?.Invoke(item);

    [RelayCommand] private void AddEnvVar() => EnvironmentVariables.Add(new EnvVarItem());
    [RelayCommand] private void RemoveEnvVar(EnvVarItem item) => EnvironmentVariables.Remove(item);

    // ── Creation ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task CreateContainerAsync()
    {
        // Advance to the dedicated creating screen first
        CurrentStep = 6;
        OnCurrentStepChanged(CurrentStep);

        IsCreating = true;
        HasError = false;
        IsComplete = false;
        ErrorMessage = "";

        _cts = new CancellationTokenSource();
        var session = Interlocked.Increment(ref _outputSession);
        var progress = new Progress<string>(line =>
        {
            if (Volatile.Read(ref _outputSession) != session) return;
            Output += line + Environment.NewLine;
            OnPropertyChanged(nameof(Output));
        });

        var request = BuildRequest();
        await RunCreationAsync(request, progress, _cts.Token);
    }

    // Navigates back to the dashboard — called after a successful creation
    [RelayCommand]
    private void Done() => ContainerCreated?.Invoke(this, EventArgs.Empty);

    // Resets and stays in the wizard to create another container
    [RelayCommand]
    private void CreateAnother() => Reset();

    // ── Property change notifications ────────────────────────────────────

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(IsStep4));
        OnPropertyChanged(nameof(IsStep5));
        OnPropertyChanged(nameof(IsStep6));
        OnPropertyChanged(nameof(IsWizardStep));
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(CanGoNext));

        if (value == 5)
            GeneratedCommand = _creator.BuildCommandPreview(BuildRequest());
    }

    partial void OnRegistryChanged(string value) => OnPropertyChanged(nameof(ImageReference));
    partial void OnImageNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(ImageReference));
    }

    partial void OnTagChanged(string value) => OnPropertyChanged(nameof(ImageReference));

    // ── Helpers ──────────────────────────────────────────────────────────

    private ContainerCreateRequest BuildRequest()
    {
        var ports = PortMappings
            .Where(p => !string.IsNullOrWhiteSpace(p.HostPort) && !string.IsNullOrWhiteSpace(p.ContainerPort))
            .Select(p => new ContainerPortMapping(
                int.Parse(p.HostPort),
                int.Parse(p.ContainerPort),
                p.Protocol.Equals("UDP", StringComparison.OrdinalIgnoreCase)
                    ? NetworkProtocol.Udp
                    : NetworkProtocol.Tcp))
            .ToArray();

        var volumes = Volumes
            .Where(v => !string.IsNullOrWhiteSpace(v.HostPath) && !string.IsNullOrWhiteSpace(v.ContainerPath))
            .Select(v => new ContainerVolume(v.HostPath, v.ContainerPath, v.ReadOnly))
            .ToArray();

        var envVars = EnvironmentVariables
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => new ContainerEnvironment(e.Name, e.Value))
            .ToArray();

        return new ContainerCreateRequest
        {
            ImageTag = ImageReference,
            Name = string.IsNullOrWhiteSpace(ContainerName) ? null : ContainerName.Trim(),
            Architecture = ContainerArchitectureExtensions.ParseContainerArchitectureOrNull(Architecture)
                ?? throw new InvalidOperationException($"Unsupported architecture '{Architecture}'."),
            EnableRosetta = EnableRosetta,
            CpuCount = double.TryParse(CpuCount, out var cpu) ? cpu : null,
            Memory = string.IsNullOrWhiteSpace(Memory) ? null : Memory.Trim(),
            KeepAlive = KeepAlive,
            StartImmediately = _settingsService.Get().StartImmediately,
            Ports = ports,
            Volumes = volumes,
            EnvironmentVariables = envVars,
            CommandOverride = string.IsNullOrWhiteSpace(GeneratedCommand) ? null : GeneratedCommand,
        };
    }

    public string ImageReference => BuildImageReference();

    private async Task RunCreationAsync(
        ContainerCreateRequest request,
        IProgress<string> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _creator.CreateAsync(request, progress, cancellationToken).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsCreating = false;
                result.Match(
                    onSuccess: _ =>
                    {
                        IsComplete = true;
                    },
                    onFailure: error =>
                    {
                        HasError = true;
                        ErrorMessage = error.Message ?? UiStrings.Get("ContainerCreationFailed");
                    });
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsCreating = false);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }


    private void Reset()
    {
        CurrentStep = 1;
        ImageName = "";
        Tag = "latest";
        ContainerName = "";
        EnableRosetta = false;
        LoadDefaults();
        PortMappings.Clear();
        Volumes.Clear();
        EnvironmentVariables.Clear();
        GeneratedCommand = "";
        IsCreating = false;
        IsComplete = false;
        HasError = false;
        Output = "";
        ErrorMessage = "";

        Interlocked.Increment(ref _outputSession);
    }

    private void LoadDefaults()
    {
        var s = _settingsService.Get();
        Registry = s.DefaultRegistry;
        Architecture = s.DefaultArchitecture;
        CpuCount = s.DefaultCpuCount;
        Memory = s.DefaultMemory;
        KeepAlive = s.DefaultKeepAlive;
    }

    private string BuildImageReference()
    {
        var image = ImageName.Trim();
        if (string.IsNullOrWhiteSpace(image))
        {
            return "";
        }

        var registry = Registry switch
        {
            "ghcr" => "ghcr.io",
            _ => "docker.io",
        };

        var tag = string.IsNullOrWhiteSpace(Tag) ? "latest" : Tag.Trim();
        var normalizedImage = image.Contains('/')
            ? image
            : registry == "docker.io"
                ? $"library/{image}"
                : image;

        return $"{registry}/{normalizedImage}:{tag}";
    }
}
