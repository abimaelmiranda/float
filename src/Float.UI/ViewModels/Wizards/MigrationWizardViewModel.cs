using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.UI.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace Float.UI.ViewModels.Wizards;

public partial class MigrationWizardViewModel : ViewModelBase
{
    private readonly IContainerReader _dockerReader;
    private readonly IContainerCreator _appleCreator;
    private readonly IContainerLifecycle _appleLifecycle;
    private readonly IContainerLifecycle _dockerLifecycle;
    private readonly IProcessHost _processHost;
    private readonly IFileSystemService _fileSystemService;
    private bool _hasLoaded;
    private CancellationTokenSource? _loadCts;

    public ObservableCollection<MigrationContainerItemViewModel> Containers { get; } = [];
    public ObservableCollection<MigrationContainerItemViewModel> CleanupCandidates { get; } = [];

    public event EventHandler<MigrationCleanupCompletedEventArgs>? MigrationCompleted;

    [ObservableProperty] public partial int CurrentStep { get; set; } = 1;
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial bool IsMigrating { get; set; }
    [ObservableProperty] public partial bool IsCleaningUp { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";
    [ObservableProperty] public partial string MigrationSummary { get; set; } = "";
    [ObservableProperty] public partial string CleanupSummary { get; set; } = "";
    [ObservableProperty] public partial MigrationContainerItemViewModel? PendingArchitectureItem { get; set; }

    public string[] ArchitectureOptions { get; } = ["arm64", "amd64"];
    [ObservableProperty] public partial string? SelectedArchitecture { get; set; }

    public int TotalSteps => 3;
    public int SelectedCount => Containers.Count(container => container.IsSelected);
    public int MigratedCount => Containers.Count(container => container.MigrationSucceeded);
    public int FailedMigrationCount => Containers.Count(container => container.MigrationFailed);
    public int CleanupSelectedCount => CleanupCandidates.Count(container => container.IsCleanupSelected);
    public int ContainerCount => Containers.Count;
    public bool HasContainers => ContainerCount > 0;
    public bool IsEmpty => !IsLoading && !HasError && !HasContainers;
    public bool ShowContainerList => !IsLoading && !HasError && HasContainers;
    public bool HasSelectedContainers => SelectedCount > 0;
    public bool CanGoBack => CurrentStep > 1 && CurrentStep < 3 && !IsMigrating && !IsCleaningUp;
    public bool HasCleanupCandidates => CleanupCandidates.Count > 0;
    public bool HasCleanupSelection => CleanupSelectedCount > 0;
    public bool CanFinishCleanup => IsCleanupStep && !IsCleaningUp;
    public bool ShowRefresh => CurrentStep == 1;
    public bool IsSelectionStep => CurrentStep == 1;
    public bool IsMigrationStep => CurrentStep == 2;
    public bool IsCleanupStep => CurrentStep == 3;
    public bool HasPendingArchitecturePrompt => PendingArchitectureItem is not null;
    public string PendingArchitectureName => PendingArchitectureItem?.Name ?? "";
    public string PendingArchitectureImageTag => PendingArchitectureItem?.ImageTag ?? "";
    public bool CanUseNext => CurrentStep switch
    {
        1 => HasSelectedContainers && !IsMigrating && !HasPendingArchitecturePrompt,
        2 => !IsMigrating,
        _ => false
    };
    public string FooterSummary => CurrentStep == 3
        ? CleanupSelectionSummary
        : SelectionSummary;
    public string SelectionSummary => SelectedCount == 1
        ? UiStrings.Get("OneContainerSelected")
        : string.Format(UiStrings.Get("ContainersSelectedFormat"), SelectedCount);
    public string CleanupSelectionSummary => CleanupSelectedCount == 0
        ? UiStrings.Get("DockerOriginalsPreserved")
        : CleanupSelectedCount == 1
            ? UiStrings.Get("OneDockerCleanupSelected")
            : string.Format(UiStrings.Get("DockerCleanupSelectedFormat"), CleanupSelectedCount);
    public string NextButtonText => CurrentStep switch
    {
        1 => UiStrings.Migrate,
        2 => IsMigrating ? UiStrings.Get("Migrating") : UiStrings.Continue,
        _ => UiStrings.Done
    };

    public MigrationWizardViewModel(
        [FromKeyedServices(ContainerEngine.Docker)] IContainerReader dockerReader,
        [FromKeyedServices(ContainerEngine.AppleContainers)] IContainerCreator appleCreator,
        [FromKeyedServices(ContainerEngine.AppleContainers)] IContainerLifecycle appleLifecycle,
        [FromKeyedServices(ContainerEngine.Docker)] IContainerLifecycle dockerLifecycle,
        IProcessHost processHost,
        IFileSystemService fileSystemService)
    {
        _dockerReader = dockerReader;
        _appleCreator = appleCreator;
        _appleLifecycle = appleLifecycle;
        _dockerLifecycle = dockerLifecycle;
        _processHost = processHost;
        _fileSystemService = fileSystemService;
    }

    public async Task StartNewRunAsync()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _hasLoaded = false;
            CurrentStep = 1;
            IsLoading = false;
            IsMigrating = false;
            IsCleaningUp = false;
            HasError = false;
            ErrorMessage = "";
            MigrationSummary = "";
            CleanupSummary = "";
            PendingArchitectureItem = null;
            SelectedArchitecture = null;
            Containers.Clear();
            CleanupCandidates.Clear();
            NotifyListStateChanged();
            NotifyCleanupStateChanged();
        });

        try
        {
            await LoadDockerContainersAsync(_loadCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    public void CancelLoad()
    {
        _loadCts?.Cancel();
    }

    [RelayCommand]
    private async Task LoadDockerContainersAsync(CancellationToken cancellationToken)
    {
        if (_hasLoaded)
            return;

        _hasLoaded = true;
        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = "";
        });

        try
        {
            var selectedNames = Containers
                .Where(container => container.IsSelected)
                .Select(container => container.Name)
                .ToHashSet(StringComparer.Ordinal);

            var result = await _dockerReader.ListContainersAsync(includeAll: true, cancellationToken).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Containers.Clear();
                result.Match(
                    onSuccess: containers =>
                    {
                        var items = containers
                            .OrderBy(container => container.Name, StringComparer.OrdinalIgnoreCase)
                            .Select(container => CreateItem(container, selectedNames.Contains(container.Name)))
                            .ToArray();

                        foreach (var item in items)
                        {
                            Containers.Add(item);
                        }
                    },
                    onFailure: error =>
                    {
                        HasError = true;
                        ErrorMessage = error.Message ?? UiStrings.Get("FailedListDockerContainers");
                    });

                IsLoading = false;
                NotifyListStateChanged();
            });
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Containers.Clear();
                IsLoading = false;
                HasError = true;
                ErrorMessage = ex.Message;
                NotifyListStateChanged();
            });
        }
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        var selectAll = Containers.Any(container => !container.IsSelected);
        foreach (var container in Containers)
        {
            container.IsSelected = selectAll;
        }

        NotifySelectionChanged();
    }

    [RelayCommand]
    private async Task NextAsync()
    {
        if (CurrentStep == 1 && !HasSelectedContainers)
            return;

        if (CurrentStep == 1)
        {
            var pendingArchitecture = Containers.FirstOrDefault(container => container.NeedsArchitectureSelection);
            if (pendingArchitecture is not null)
            {
                PendingArchitectureItem = pendingArchitecture;
                return;
            }

            CurrentStep = 2;
            await MigrateSelectedContainersAsync().ConfigureAwait(false);
            return;
        }

        if (CurrentStep < TotalSteps && !IsMigrating)
            CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (HasPendingArchitecturePrompt)
        {
            PendingArchitectureItem = null;
            return;
        }

        if (CanGoBack)
            CurrentStep--;
    }

    [RelayCommand]
    private void ResolveArchitecture(string architecture)
    {
        var parsedArchitecture = ContainerArchitectureExtensions.ParseContainerArchitectureOrNull(architecture);
        if (PendingArchitectureItem is null || parsedArchitecture is null)
            return;

        var item = PendingArchitectureItem;
        item.ManualArchitecture = parsedArchitecture.Value.ToDisplayValue();
        item.AppendLogLine(UiStrings.Get("DockerImageArchitectureUndetected"));
        item.AppendLogLine(string.Format(UiStrings.Get("ArchitectureProvidedManuallyFormat"), item.ManualArchitecture));
        PendingArchitectureItem = null;
        SelectedArchitecture = null;

        var nextPending = Containers.FirstOrDefault(container => container.NeedsArchitectureSelection);
        if (nextPending is not null)
            PendingArchitectureItem = nextPending;
    }

    [RelayCommand]
    private void CancelArchitecturePrompt()
    {
        PendingArchitectureItem = null;
    }

    [RelayCommand]
    private async Task ConfirmCleanupAsync()
    {
        if (IsCleaningUp)
            return;

        var selected = CleanupCandidates
            .Where(container => container.IsCleanupSelected)
            .ToArray();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsCleaningUp = true;
            CleanupSummary = selected.Length == 0
                ? UiStrings.Get("FinishingMigration")
                : UiStrings.Get("CleaningUpDockerOriginals");
        });

        var removed = 0;
        var failed = 0;

        foreach (var item in selected)
        {
            var progress = CreateItemProgress(item);
            var result = await _dockerLifecycle.DeleteAsync(item.Source, progress: progress).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                removed++;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    item.CleanupSucceeded = true;
                    item.CleanupFailed = false;
                    item.IsCleanupSelected = false;
                    item.MigrationStatus = UiStrings.Get("DockerOriginalRemoved");
                });
            }
            else
            {
                var errorMessage = result.Failure.Message ?? UiStrings.UnknownError;
                item.AppendLogLine(string.Format(UiStrings.Get("CleanupFailedFormat"), errorMessage));
                failed++;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    item.CleanupSucceeded = false;
                    item.CleanupFailed = true;
                    item.MigrationStatus = string.Format(UiStrings.Get("CleanupFailedFormat"), errorMessage);
                });
            }
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsCleaningUp = false;
            var preserved = CleanupCandidates.Count - selected.Length;
            var result = new MigrationCleanupCompletedEventArgs(
                MigratedCount,
                removed,
                0,
                preserved,
                failed);
            CleanupSummary = result.Summary;
            NotifyCleanupStateChanged();
            MigrationCompleted?.Invoke(this, result);
        });
    }

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(ShowRefresh));
        OnPropertyChanged(nameof(IsSelectionStep));
        OnPropertyChanged(nameof(IsMigrationStep));
        OnPropertyChanged(nameof(IsCleanupStep));
        OnPropertyChanged(nameof(CanFinishCleanup));
        OnPropertyChanged(nameof(FooterSummary));
        OnPropertyChanged(nameof(NextButtonText));
        OnPropertyChanged(nameof(CanUseNext));
        NextCommand.NotifyCanExecuteChanged();
    }

    private MigrationContainerItemViewModel CreateItem(Container container, bool isSelected)
    {
        var item = new MigrationContainerItemViewModel(container, isSelected);
        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MigrationContainerItemViewModel.IsSelected))
            {
                NotifySelectionChanged();
            }
            else if (e.PropertyName == nameof(MigrationContainerItemViewModel.IsCleanupSelected))
            {
                NotifyCleanupStateChanged();
            }
            else if (e.PropertyName == nameof(MigrationContainerItemViewModel.ManualArchitecture))
            {
                NotifySelectionChanged();
            }
        };

        return item;
    }

    private async Task MigrateSelectedContainersAsync()
    {
        var selected = Containers.Where(container => container.IsSelected).ToArray();
        if (selected.Length == 0)
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsMigrating = true;
            MigrationSummary = "";
            CleanupCandidates.Clear();
            foreach (var item in Containers)
            {
                item.IsMigrating = false;
                item.MigrationSucceeded = false;
                item.MigrationFailed = false;
                item.CleanupSucceeded = false;
                item.CleanupFailed = false;
                item.IsCleanupSelected = false;
                item.ClearLog();
                if (item.Source.Architecture is null && item.ManualArchitecture is not null)
                {
                    item.AppendLogLine(UiStrings.Get("DockerImageArchitectureUndetected"));
                    item.AppendLogLine(string.Format(UiStrings.Get("ArchitectureProvidedManuallyFormat"), item.ManualArchitecture));
                }
                item.MigrationStatus = item.IsSelected ? UiStrings.Get("Waiting") : UiStrings.Get("NotSelected");
            }
        });

        foreach (var item in selected)
        {
            await MigrateContainerAsync(item).ConfigureAwait(false);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsMigrating = false;
            MigrationSummary = BuildMigrationSummary();
            RebuildCleanupCandidates();
            if (FailedMigrationCount == 0)
            {
                CurrentStep = 3;
            }

            NotifyMigrationStateChanged();
        });
    }

    private async Task MigrateContainerAsync(MigrationContainerItemViewModel item)
    {
        var appleContainer = item.Source with
        {
            Name = item.Name,
            Instance = new ContainerInstance { Status = ContainerStatus.Created }
        };
        var dockerWasRunning = item.Source.Instance?.Status == ContainerStatus.Running;
        var dockerStopped = false;
        var appleCreateAttempted = false;
        var progress = CreateItemProgress(item);

        try
        {
            // Stop Docker before export when there are named/anonymous volumes (data consistency)
            var hasNamedVolumes = item.Source.Volumes.Any(v => v.IsNamedVolume);
            if (dockerWasRunning && hasNamedVolumes)
            {
                await Dispatcher.UIThread.InvokeAsync(() => item.MigrationStatus = UiStrings.Get("StoppingDockerOriginal"));
                var stopResult = await _dockerLifecycle.StopAsync(item.Source, progress: progress).ConfigureAwait(false);
                if (stopResult.IsFailure)
                    throw new InvalidOperationException(stopResult.Failure.Message ?? UiStrings.Get("FailedStopDockerContainer"));
                dockerStopped = true;
            }

            var exportedVolumes = await ExportNamedVolumesAsync(item, progress).ConfigureAwait(false);

            var allVolumes = item.Source.Volumes
                .Where(v => !v.IsNamedVolume)
                .Concat(exportedVolumes)
                .ToArray();

            var createRequest = BuildCreateRequest(item, allVolumes);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                item.IsMigrating = true;
                item.MigrationStatus = UiStrings.Get("CreatingAppleContainer");
            });

            appleCreateAttempted = true;
            var createResult = await _appleCreator.CreateAsync(createRequest, progress).ConfigureAwait(false);
            if (createResult.IsFailure)
                throw new InvalidOperationException(createResult.Failure.Message ?? UiStrings.Get("ContainerCreationFailed"));

            if (dockerWasRunning && !dockerStopped)
            {
                await Dispatcher.UIThread.InvokeAsync(() => item.MigrationStatus = UiStrings.Get("StoppingDockerOriginal"));
                var stopResult = await _dockerLifecycle.StopAsync(item.Source, progress: progress).ConfigureAwait(false);
                if (stopResult.IsFailure)
                    throw new InvalidOperationException(stopResult.Failure.Message ?? UiStrings.Get("FailedStopDockerContainer"));
                dockerStopped = true;
            }

            await Dispatcher.UIThread.InvokeAsync(() => item.MigrationStatus = UiStrings.Get("StartingAppleContainer"));
            var startResult = await _appleLifecycle.StartAsync(appleContainer, progress: progress).ConfigureAwait(false);
            if (startResult.IsFailure)
                throw new InvalidOperationException(startResult.Failure.Message ?? UiStrings.Get("FailedStartAppleContainer"));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                item.IsMigrating = false;
                item.MigrationSucceeded = true;
                item.MigrationFailed = false;
                item.MigrationStatus = UiStrings.Get("Migrated");
            });
        }
        catch (Exception ex)
        {
            item.AppendLogLine(string.Format(UiStrings.Get("MigrationFailedFormat"), ex.Message));
            var rollbackMessage = await RollbackAsync(
                appleContainer,
                item.Source,
                appleCreateAttempted,
                dockerStopped,
                progress).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(rollbackMessage))
                item.AppendLogLine(rollbackMessage);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                item.IsMigrating = false;
                item.MigrationSucceeded = false;
                item.MigrationFailed = true;
                item.MigrationStatus = string.IsNullOrWhiteSpace(rollbackMessage)
                    ? string.Format(UiStrings.Get("FailedFormat"), ex.Message)
                    : string.Format(UiStrings.Get("FailedWithRollbackFormat"), ex.Message, rollbackMessage);
            });
        }
    }

    private async Task<ContainerVolume[]> ExportNamedVolumesAsync(
        MigrationContainerItemViewModel item,
        IProgress<string> progress)
    {
        var namedVolumes = item.Source.Volumes.Where(v => v.IsNamedVolume).ToArray();
        if (namedVolumes.Length == 0)
            return [];

        var result = new List<ContainerVolume>(namedVolumes.Length);
        var volumesBase = Path.Combine(_fileSystemService.GetFloatVolumesDir(), item.Source.Name!);

        for (var i = 0; i < namedVolumes.Length; i++)
        {
            var vol = namedVolumes[i];
            var hostDest = Path.Combine(volumesBase, i.ToString());
            Directory.CreateDirectory(hostDest);

            progress.Report(string.Format(UiStrings.Get("ExportingVolumeFormat"), i + 1, namedVolumes.Length));

            var errors = new StringBuilder();
            var runResult = await _processHost.RunWithResultAsync(
                "/usr/local/bin/docker",
                ["cp", $"{item.Source.Name}:{vol.ContainerPath}/.", hostDest],
                null,
                onOutput: line => progress.Report(line),
                onError: line => errors.AppendLine(line)).ConfigureAwait(false);

            if (runResult.IsFailure)
                throw new InvalidOperationException(
                    string.Format(UiStrings.Get("VolumeExportFailed"), errors.ToString().Trim()));

            result.Add(vol with { HostPath = hostDest, IsNamedVolume = false });
        }

        return [.. result];
    }

    private async Task<string> RollbackAsync(
        Container appleContainer,
        Container dockerContainer,
        bool appleCreateAttempted,
        bool dockerStopped,
        IProgress<string> progress)
    {
        var messages = new List<string>();

        if (appleCreateAttempted)
        {
            var deleteResult = await _appleLifecycle.DeleteAsync(appleContainer, progress: progress).ConfigureAwait(false);
            if (deleteResult.IsSuccess)
                messages.Add(UiStrings.Get("RemovedPartialAppleContainer"));
            else
                messages.Add(string.Format(UiStrings.Get("CouldNotRemovePartialAppleContainerFormat"), deleteResult.Failure.Message));
        }

        if (dockerStopped)
        {
            var startResult = await _dockerLifecycle.StartAsync(dockerContainer, progress: progress).ConfigureAwait(false);
            if (startResult.IsSuccess)
                messages.Add(UiStrings.Get("DockerOriginalRestarted"));
            else
                messages.Add(string.Format(UiStrings.Get("CouldNotRestartDockerOriginalFormat"), startResult.Failure.Message));
        }

        return string.Join(" ", messages);
    }

    private static IProgress<string> CreateItemProgress(MigrationContainerItemViewModel item)
        => new Progress<string>(item.AppendLogLine);

    private static ContainerCreateRequest BuildCreateRequest(
        MigrationContainerItemViewModel item,
        ContainerVolume[] volumes)
    {
        var source = item.Source;
        var architecture = item.EffectiveArchitecture;
        if (architecture is null)
            throw new InvalidOperationException(
                string.Format(UiStrings.Get("CouldNotDetectDockerArchitectureFormat"), source.Name));

        return new ContainerCreateRequest
        {
            ImageTag = source.Image.Tag,
            Name = source.Name,
            Architecture = architecture.Value,
            EnableRosetta = architecture == ContainerArchitecture.Amd64,
            StartImmediately = false,
            Ports = source.Ports,
            Volumes = volumes,
            EnvironmentVariables = source.EnvironmentVariables
        };
    }

    private void RebuildCleanupCandidates()
    {
        CleanupCandidates.Clear();
        foreach (var item in Containers.Where(container => container.MigrationSucceeded))
            CleanupCandidates.Add(item);

        NotifyCleanupStateChanged();
    }

    private void NotifyListStateChanged()
    {
        OnPropertyChanged(nameof(ContainerCount));
        OnPropertyChanged(nameof(HasContainers));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ShowContainerList));
        NotifySelectionChanged();
    }

    private void NotifySelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(HasSelectedContainers));
        OnPropertyChanged(nameof(SelectionSummary));
        OnPropertyChanged(nameof(FooterSummary));
        OnPropertyChanged(nameof(CanUseNext));
        OnPropertyChanged(nameof(HasPendingArchitecturePrompt));
        NextCommand.NotifyCanExecuteChanged();
    }

    private void NotifyMigrationStateChanged()
    {
        OnPropertyChanged(nameof(MigratedCount));
        OnPropertyChanged(nameof(FailedMigrationCount));
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(NextButtonText));
        OnPropertyChanged(nameof(CanUseNext));
        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }

    private void NotifyCleanupStateChanged()
    {
        OnPropertyChanged(nameof(CleanupSelectedCount));
        OnPropertyChanged(nameof(HasCleanupCandidates));
        OnPropertyChanged(nameof(HasCleanupSelection));
        OnPropertyChanged(nameof(CanFinishCleanup));
        OnPropertyChanged(nameof(CleanupSelectionSummary));
        OnPropertyChanged(nameof(FooterSummary));
        ConfirmCleanupCommand.NotifyCanExecuteChanged();
    }

    private string BuildMigrationSummary()
        => FailedMigrationCount == 0
            ? string.Format(UiStrings.Get("MigratedCountFormat"), MigratedCount)
            : string.Format(UiStrings.Get("MigratedFailedCountFormat"), MigratedCount, FailedMigrationCount);

    partial void OnIsLoadingChanged(bool value)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        LoadDockerContainersCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ShowContainerList));
    }

    partial void OnHasErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ShowContainerList));
    }

    partial void OnIsMigratingChanged(bool value)
    {
        NotifyMigrationStateChanged();
    }

    partial void OnIsCleaningUpChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanFinishCleanup));
        ConfirmCleanupCommand.NotifyCanExecuteChanged();
    }

    partial void OnPendingArchitectureItemChanged(MigrationContainerItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasPendingArchitecturePrompt));
        OnPropertyChanged(nameof(PendingArchitectureName));
        OnPropertyChanged(nameof(PendingArchitectureImageTag));
        OnPropertyChanged(nameof(CanUseNext));
        OnPropertyChanged(nameof(CanGoBack));
        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedArchitectureChanged(string? value)
    {
        OnPropertyChanged(nameof(CanConfirmArchitecture));
    }

    public bool CanConfirmArchitecture => SelectedArchitecture is not null;

}

public sealed class MigrationCleanupCompletedEventArgs : EventArgs
{
    public MigrationCleanupCompletedEventArgs(
        int migrated,
        int removed,
        int alreadyRemoved,
        int preserved,
        int failed)
    {
        Migrated = migrated;
        Removed = removed;
        AlreadyRemoved = alreadyRemoved;
        Preserved = preserved;
        Failed = failed;
    }

    public int Migrated { get; }
    public int Removed { get; }
    public int AlreadyRemoved { get; }
    public int Preserved { get; }
    public int Failed { get; }

    public string Summary
    {
        get
        {
            var parts = new List<string>
            {
                string.Format(UiStrings.Get("MigratedPartFormat"), Migrated),
                string.Format(UiStrings.Get("RemovedPartFormat"), Removed)
            };

            if (AlreadyRemoved > 0)
                parts.Add(string.Format(UiStrings.Get("AlreadyRemovedPartFormat"), AlreadyRemoved));

            if (Preserved > 0)
                parts.Add(string.Format(UiStrings.Get("PreservedPartFormat"), Preserved));

            if (Failed > 0)
                parts.Add(string.Format(UiStrings.Get("CleanupFailedPartFormat"), Failed));

            return string.Join(", ", parts) + ".";
        }
    }
}
