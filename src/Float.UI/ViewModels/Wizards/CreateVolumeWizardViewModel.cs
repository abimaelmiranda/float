using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;

namespace Float.UI.ViewModels.Wizards;

public partial class CreateVolumeWizardViewModel : ViewModelBase
{
    private readonly IContainerVolumeCreator _creator;
    private CancellationTokenSource? _cts;
    private long? _availableDiskBytes;

    public event EventHandler? VolumeCreated;
    public event EventHandler? Cancelled;

    [ObservableProperty] public partial int CurrentStep { get; set; } = 1;
    [ObservableProperty] public partial string VolumeName { get; set; } = "";
    [ObservableProperty] public partial decimal? VolumeSizeValue { get; set; }
    [ObservableProperty] public partial ContainerVolumeSizeUnit VolumeSizeUnit { get; set; } = ContainerVolumeSizeUnit.GB;
    [ObservableProperty] public partial ContainerVolumeJournalMode JournalMode { get; set; }
    [ObservableProperty] public partial decimal? JournalSizeValue { get; set; }
    [ObservableProperty] public partial ContainerVolumeSizeUnit JournalSizeUnit { get; set; } = ContainerVolumeSizeUnit.MB;
    [ObservableProperty] public partial bool IsCreating { get; set; }
    [ObservableProperty] public partial bool IsComplete { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string Output { get; set; } = "";
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";

    public ContainerVolumeJournalMode[] JournalModeOptions { get; } =
    [
        ContainerVolumeJournalMode.Default,
        ContainerVolumeJournalMode.Enabled,
        ContainerVolumeJournalMode.Disabled,
    ];

    public ContainerVolumeSizeUnit[] SizeUnitOptions { get; } =
    [
        ContainerVolumeSizeUnit.B,
        ContainerVolumeSizeUnit.KB,
        ContainerVolumeSizeUnit.MB,
        ContainerVolumeSizeUnit.GB,
        ContainerVolumeSizeUnit.TB,
    ];

    public bool IsDetailsStep => CurrentStep == 1;
    public bool IsCreatingStep => CurrentStep == 2;
    public bool IsJournalSizeVisible => JournalMode == ContainerVolumeJournalMode.Enabled;
    public bool HasVolumeSizeWarning => GetRequestedBytes(VolumeSizeValue, VolumeSizeUnit) is { } requested
                                        && _availableDiskBytes is { } available
                                        && requested > available;
    public string VolumeSizeWarning => _availableDiskBytes is { } available
        ? $"Requested size is larger than available disk space ({FormatBytes(available)})."
        : "";
    public bool CanCreate => !string.IsNullOrWhiteSpace(VolumeName) && !IsCreating;

    public CreateVolumeWizardViewModel(IContainerVolumeCreator creator)
    {
        _creator = creator;
    }

    public void StartNewRun()
    {
        _cts?.Cancel();
        _availableDiskBytes = GetAvailableDiskBytes();
        CurrentStep = 1;
        VolumeName = "";
        VolumeSizeValue = null;
        VolumeSizeUnit = ContainerVolumeSizeUnit.GB;
        JournalMode = ContainerVolumeJournalMode.Default;
        JournalSizeValue = null;
        JournalSizeUnit = ContainerVolumeSizeUnit.MB;
        IsCreating = false;
        IsComplete = false;
        HasError = false;
        Output = "";
        ErrorMessage = "";
        NotifyVolumeSizeWarningChanged();
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        StartNewRun();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task CreateVolumeAsync()
    {
        if (!CanCreate)
            return;

        CurrentStep = 2;
        IsCreating = true;
        IsComplete = false;
        HasError = false;
        ErrorMessage = "";
        Output = "";

        var cts = new CancellationTokenSource();
        _cts = cts;
        var progress = new Progress<string>(line =>
        {
            Output += line + Environment.NewLine;
            OnPropertyChanged(nameof(Output));
        });

        try
        {
            var request = new ContainerVolumeCreateRequest
            {
                Name = VolumeName,
                SizeValue = VolumeSizeValue,
                SizeUnit = VolumeSizeUnit,
                JournalMode = JournalMode,
                JournalSizeValue = JournalSizeValue,
                JournalSizeUnit = JournalSizeUnit,
            };
            var result = await _creator.CreateAsync(request, progress, cts.Token).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsCreating = false;
                result.Match(
                    onSuccess: _ => IsComplete = true,
                    onFailure: error =>
                    {
                        HasError = true;
                        ErrorMessage = error.Message ?? "Volume creation failed";
                    });
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsCreating = false);
        }
        finally
        {
            cts.Dispose();
            if (ReferenceEquals(_cts, cts))
                _cts = null;
        }
    }

    [RelayCommand]
    private void Done() => VolumeCreated?.Invoke(this, EventArgs.Empty);

    partial void OnCurrentStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsDetailsStep));
        OnPropertyChanged(nameof(IsCreatingStep));
    }

    partial void OnVolumeNameChanged(string value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateVolumeCommand.NotifyCanExecuteChanged();
    }

    partial void OnVolumeSizeValueChanged(decimal? value) => NotifyVolumeSizeWarningChanged();

    partial void OnVolumeSizeUnitChanged(ContainerVolumeSizeUnit value) => NotifyVolumeSizeWarningChanged();

    partial void OnJournalModeChanged(ContainerVolumeJournalMode value)
    {
        OnPropertyChanged(nameof(IsJournalSizeVisible));
    }

    partial void OnIsCreatingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCreate));
        CreateVolumeCommand.NotifyCanExecuteChanged();
    }

    private void NotifyVolumeSizeWarningChanged()
    {
        OnPropertyChanged(nameof(HasVolumeSizeWarning));
        OnPropertyChanged(nameof(VolumeSizeWarning));
    }

    private static decimal? GetRequestedBytes(decimal? value, ContainerVolumeSizeUnit unit)
    {
        if (value is null)
            return null;

        var multiplier = unit switch
        {
            ContainerVolumeSizeUnit.B => 1m,
            ContainerVolumeSizeUnit.KB => 1024m,
            ContainerVolumeSizeUnit.MB => 1024m * 1024m,
            ContainerVolumeSizeUnit.GB => 1024m * 1024m * 1024m,
            ContainerVolumeSizeUnit.TB => 1024m * 1024m * 1024m * 1024m,
            _ => 1m,
        };

        return value.Value * multiplier;
    }

    private static long? GetAvailableDiskBytes()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) ?? "/";
            return new DriveInfo(root).AvailableFreeSpace;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }
}
