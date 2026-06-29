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
using Float.Core.Models.Results;
using Float.UI.Resources;

namespace Float.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase, IHasError, IHasPendingDelete
{
    private readonly IContainerReader _containerReader;
    private readonly IContainerLifecycle _containerLifecycle;

    [ObservableProperty]
    public partial ContainerItemViewModel? SelectedContainer { get; set; }

    [ObservableProperty] public partial bool HasError { get; set; }
    [ObservableProperty] public partial string ErrorMessage { get; set; } = "";
    [ObservableProperty] public partial Container? PendingDeleteContainer { get; set; }

    public bool HasSelectedContainer => SelectedContainer is not null;
    public bool HasPendingDelete => PendingDeleteContainer is not null;
    public string PendingDeleteName => PendingDeleteContainer?.Name ?? "";

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

    partial void OnPendingDeleteContainerChanged(Container? value)
    {
        OnPropertyChanged(nameof(HasPendingDelete));
        OnPropertyChanged(nameof(PendingDeleteName));
    }

    public event EventHandler? RequestCreateContainer;
    public event EventHandler<string>? OperationFailed;
    public event EventHandler<string>? OperationSucceeded;

    [RelayCommand]
    private void NewContainer() => RequestCreateContainer?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private Task RefreshContainersAsync() => RefreshAsync();

    [RelayCommand]
    private void CloseDetail() => SelectedContainer = null;

    [RelayCommand]
    private Task StartContainerAsync(Container? container)
        => RunLifecycleAsync(container, UiStrings.Starting, (c, ct) => _containerLifecycle.StartAsync(c, ct));

    [RelayCommand]
    private Task StopContainerAsync(Container? container)
        => RunLifecycleAsync(container, UiStrings.Get("Stopping"), (c, ct) => _containerLifecycle.StopAsync(c, ct));

    [RelayCommand]
    private Task RestartContainerAsync(Container? container)
        => RunLifecycleAsync(container, UiStrings.Get("Restarting"), (c, ct) => _containerLifecycle.RestartAsync(c, ct));

    [RelayCommand]
    private void RequestDelete(Container? container)
    {
        PendingDeleteContainer = container;
    }

    [RelayCommand]
    private void CancelDelete()
    {
        PendingDeleteContainer = null;
    }

    [RelayCommand]
    private async Task ConfirmDeleteAsync()
    {
        var container = PendingDeleteContainer;
        PendingDeleteContainer = null;
        if (container is null)
            return;

        var name = container.Name;
        var deleted = await RunLifecycleAsync(container, UiStrings.Get("Deleting"), (c, ct) => _containerLifecycle.DeleteAsync(c, ct))
            .ConfigureAwait(false);
        if (deleted)
            OperationSucceeded?.Invoke(this, string.Format(UiStrings.Get("ContainerDeletedFormat"), name));
    }

    public async Task RefreshAsync()
    {
        var result = await _containerReader.ListContainersAsync().ConfigureAwait(false);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Containers.Clear();
            result.Match(
                onSuccess: containers =>
                {
                    var items = containers.Select(container => new ContainerItemViewModel(container)).ToArray();
                    var selectedName = SelectedContainer?.Name;

                    foreach (var item in items)
                    {
                        Containers.Add(item);
                    }

                    HasError = false;
                    ErrorMessage = "";
                    SelectedContainer = selectedName is null
                        ? null
                        : Containers.FirstOrDefault(container => container.Name == selectedName);
                },
                onFailure: error =>
                {
                    HasError = true;
                    ErrorMessage = error.Message ?? UiStrings.Get("FailedListContainers");
                    OperationFailed?.Invoke(this, ErrorMessage);
                });
        });
    }

    private async Task<bool> RunLifecycleAsync(
        Container? container,
        string busyLabel,
        Func<Container, CancellationToken, Task<Result>> action)
    {
        if (container is null)
        {
            return false;
        }

        var item = Containers.FirstOrDefault(c => c.Name == container.Name);
        await SetBusyAsync(item, true, busyLabel).ConfigureAwait(false);

        try
        {
            var result = await Task.Run(() => action(container, CancellationToken.None)).ConfigureAwait(false);
            if (result.IsFailure)
            {
                await ShowErrorAsync(result.Failure.Message ?? UiStrings.Get("ContainerCommandFailed")).ConfigureAwait(false);
                return false;
            }

            await RefreshAsync().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.Message).ConfigureAwait(false);
            return false;
        }
        finally
        {
            await SetBusyAsync(item, false, "").ConfigureAwait(false);
        }
    }

    private static async Task SetBusyAsync(ContainerItemViewModel? item, bool isBusy, string label)
    {
        if (item is null)
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            item.IsBusy = isBusy;
            item.BusyLabel = label;
        });
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
