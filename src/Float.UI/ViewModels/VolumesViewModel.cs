using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Float.UI.ViewModels;

public partial class VolumesViewModel : ViewModelBase
{
    private readonly IContainerReader _containerReader;
    private readonly IContainerVolumeLifecycle _volumeLifecycle;

    [ObservableProperty] public partial VolumeItemViewModel? SelectedVolume { get; set; }
    [ObservableProperty] public partial ContainerVolumeInfo? PendingDeleteVolume { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";

    public bool HasSelectedVolume => SelectedVolume is not null;
    public bool HasPendingDelete => PendingDeleteVolume is not null;
    public string PendingDeleteName => PendingDeleteVolume?.Name ?? "";
    public ObservableCollection<VolumeItemViewModel> Volumes { get; } = [];

    public event EventHandler? RequestCreateVolume;
    public event EventHandler<string>? OperationFailed;

    public VolumesViewModel(
        [FromKeyedServices(ContainerEngine.AppleContainers)] IContainerReader containerReader,
        [FromKeyedServices(ContainerEngine.AppleContainers)] IContainerVolumeLifecycle volumeLifecycle)
    {
        _containerReader = containerReader;
        _volumeLifecycle = volumeLifecycle;
    }

    partial void OnSelectedVolumeChanged(VolumeItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedVolume));
    }

    partial void OnPendingDeleteVolumeChanged(ContainerVolumeInfo? value)
    {
        OnPropertyChanged(nameof(HasPendingDelete));
        OnPropertyChanged(nameof(PendingDeleteName));
    }

    [RelayCommand]
    private Task RefreshVolumesAsync() => RefreshAsync();

    [RelayCommand]
    private void CreateVolume() => RequestCreateVolume?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void CloseDetail() => SelectedVolume = null;

    [RelayCommand]
    private void RequestDelete(VolumeItemViewModel? volume)
    {
        PendingDeleteVolume = volume?.Source;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        PendingDeleteVolume = null;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        var volume = PendingDeleteVolume;
        PendingDeleteVolume = null;
        if (volume is null)
            return;

        var result = await _volumeLifecycle.DeleteAsync(volume).ConfigureAwait(false);
        if (result.IsFailure)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasError = true;
                ErrorMessage = result.Failure.Message ?? "Failed to delete volume";
                OperationFailed?.Invoke(this, ErrorMessage);
            });
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            SelectedVolume = null;
        });
        await RefreshAsync().ConfigureAwait(false);
    }

    public async Task RefreshAsync()
    {
        var result = await _containerReader.ListVolumesAsync().ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Volumes.Clear();
            result.Match(
                onSuccess: volumes =>
                {
                    var items = volumes
                        .OrderBy(volume => volume.Name, StringComparer.OrdinalIgnoreCase)
                        .Select(volume => new VolumeItemViewModel(volume))
                        .ToArray();

                    foreach (var item in items)
                    {
                        Volumes.Add(item);
                    }

                    HasError = false;
                    ErrorMessage = "";
                    SelectedVolume = SelectedVolume is null
                        ? null
                        : Volumes.FirstOrDefault(volume => volume.Name == SelectedVolume.Name);
                },
                onFailure: error =>
                {
                    HasError = true;
                    ErrorMessage = error.Message ?? "Failed to list volumes";
                    OperationFailed?.Invoke(this, ErrorMessage);
                });
        });
    }
}
