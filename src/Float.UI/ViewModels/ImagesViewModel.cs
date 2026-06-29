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

    [ObservableProperty] public partial ImageItemViewModel? SelectedImage { get; set; }
    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";

    public bool HasSelectedImage => SelectedImage is not null;
    public ObservableCollection<ImageItemViewModel> Images { get; } = [];

    public event EventHandler<string>? OperationFailed;

    public ImagesViewModel([FromKeyedServices(ContainerEngine.AppleContainers)] IContainerReader containerReader)
    {
        _containerReader = containerReader;
    }

    partial void OnSelectedImageChanged(ImageItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedImage));
    }

    [RelayCommand]
    private Task RefreshImagesAsync() => RefreshAsync();

    [RelayCommand]
    private void CloseDetail() => SelectedImage = null;

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
