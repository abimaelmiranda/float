using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Models;

namespace Float.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IContainerReader _containerReader;
    private readonly IContainerLifecycle _containerLifecycle;

    [ObservableProperty]
    public partial ContainerItemViewModel? SelectedContainer { get; set; }

    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";

    public bool HasSelectedContainer => SelectedContainer is not null;

    public ObservableCollection<ContainerItemViewModel> Containers { get; } = [];

    public DashboardViewModel(IContainerReader containerReader, IContainerLifecycle containerLifecycle)
    {
        _containerReader = containerReader;
        _containerLifecycle = containerLifecycle;
    }

    partial void OnSelectedContainerChanged(ContainerItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedContainer));
    }

    public event EventHandler? RequestCreateContainer;
    public event EventHandler<string>? OperationFailed;

    [RelayCommand]
    private void NewContainer() => RequestCreateContainer?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private Task RefreshContainersAsync() => RefreshAsync();

    [RelayCommand]
    private void CloseDetail() => SelectedContainer = null;

    [RelayCommand]
    private Task StartContainerAsync(Container? container)
        => RunLifecycleAsync(container, (c, ct) => _containerLifecycle.StartAsync(c, ct));

    [RelayCommand]
    private Task StopContainerAsync(Container? container)
        => RunLifecycleAsync(container, (c, ct) => _containerLifecycle.StopAsync(c, ct));

    [RelayCommand]
    private Task RestartContainerAsync(Container? container)
        => RunLifecycleAsync(container, (c, ct) => _containerLifecycle.RestartAsync(c, ct));

    [RelayCommand]
    private Task DeleteContainerAsync(Container? container)
        => RunLifecycleAsync(container, (c, ct) => _containerLifecycle.DeleteAsync(c, ct));

    public async Task RefreshAsync()
    {
        IReadOnlyList<Container> containers;

        try
        {
            containers = await _containerReader.ListAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message).ConfigureAwait(false);
            return;
        }

        var items = containers.Select(container => new ContainerItemViewModel(container)).ToArray();
        var selectedName = SelectedContainer?.Name;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Containers.Clear();
            foreach (var item in items)
            {
                Containers.Add(item);
            }

            HasError = false;
            ErrorMessage = "";
            SelectedContainer = selectedName is null
                ? null
                : Containers.FirstOrDefault(container => container.Name == selectedName);
        });
    }

    private async Task RunLifecycleAsync(
        Container? container,
        Func<Container, CancellationToken, Task> action)
    {
        if (container is null)
        {
            return;
        }

        try
        {
            await Task.Run(() => action(container, CancellationToken.None)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message).ConfigureAwait(false);
            return;
        }

        await RefreshAsync().ConfigureAwait(false);
    }

    private async Task ShowErrorAsync(string message)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            HasError = true;
            ErrorMessage = message;
            OperationFailed?.Invoke(this, message);
        });
    }
}
