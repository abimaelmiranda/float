using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Microsoft.Extensions.DependencyInjection;

namespace Float.UI.ViewModels;

public partial class ImagesViewModel : ViewModelBase
{
    private readonly IContainerReader _containerReader;
    private readonly IContainerImageLifecycle _imageLifecycle;

    [ObservableProperty] public partial ImageItemViewModel? SelectedImage { get; set; }
    [ObservableProperty] public partial ContainerImage? PendingDeleteImage { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";

    public bool HasSelectedImage => SelectedImage is not null;
    public bool HasPendingDelete => PendingDeleteImage is not null;
    public string PendingDeleteName => string.IsNullOrWhiteSpace(PendingDeleteImage?.Tag)
        ? PendingDeleteImage?.Id ?? ""
        : PendingDeleteImage.Tag;
    public ObservableCollection<ImageItemViewModel> Images { get; } = [];

    public event EventHandler<string>? OperationFailed;

    public ImagesViewModel(
        [FromKeyedServices(ContainerEngine.AppleContainers)] IContainerReader containerReader,
        [FromKeyedServices(ContainerEngine.AppleContainers)] IContainerImageLifecycle imageLifecycle)
    {
        _containerReader = containerReader;
        _imageLifecycle = imageLifecycle;
    }

    partial void OnSelectedImageChanged(ImageItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedImage));
    }

    partial void OnPendingDeleteImageChanged(ContainerImage? value)
    {
        OnPropertyChanged(nameof(HasPendingDelete));
        OnPropertyChanged(nameof(PendingDeleteName));
    }

    [RelayCommand]
    private Task RefreshImagesAsync() => RefreshAsync();

    [RelayCommand]
    private void CloseDetail() => SelectedImage = null;

    [RelayCommand]
    private void RequestDelete(ImageItemViewModel? image)
    {
        PendingDeleteImage = image?.Source;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        PendingDeleteImage = null;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        var image = PendingDeleteImage;
        PendingDeleteImage = null;
        if (image is null)
            return;

        var result = await _imageLifecycle.DeleteAsync(image).ConfigureAwait(false);
        if (result.IsFailure)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                HasError = true;
                ErrorMessage = result.Failure.Message ?? "Failed to delete image";
                OperationFailed?.Invoke(this, ErrorMessage);
            });
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => SelectedImage = null);
        await RefreshAsync().ConfigureAwait(false);
    }

    public async Task RefreshAsync()
    {
        var result = await _containerReader.ListImagesAsync().ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Images.Clear();
            result.Match(
                onSuccess: images =>
                {
                    var items = images
                        .OrderBy(image => image.Tag, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(image => image.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(image => new ImageItemViewModel(image))
                        .ToArray();

                    foreach (var item in items)
                    {
                        Images.Add(item);
                    }

                    HasError = false;
                    ErrorMessage = "";
                    SelectedImage = SelectedImage is null
                        ? null
                        : Images.FirstOrDefault(image => image.Id == SelectedImage.Id);
                },
                onFailure: error =>
                {
                    HasError = true;
                    ErrorMessage = error.Message ?? "Failed to list images";
                    OperationFailed?.Invoke(this, ErrorMessage);
                });
        });
    }
}
