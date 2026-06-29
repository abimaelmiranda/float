using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;
using Float.UI.Resources;

namespace Float.UI.ViewModels;

public abstract partial class ProvisioningViewModelBase : ViewModelBase
{
    [ObservableProperty] public partial bool   IsRunning     { get; set; }
    [ObservableProperty] public partial bool   IsComplete    { get; set; }
    [ObservableProperty] public partial bool   HasError      { get; set; }
    [ObservableProperty] public partial string Output        { get; set; } = string.Empty;
    [ObservableProperty] public partial string ErrorMessage  { get; set; } = string.Empty;

    public bool ShowConsole => IsRunning || IsComplete || HasError;

    private CancellationTokenSource? _cts;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task Install()
    {
        IsRunning    = true;
        IsComplete   = false;
        HasError     = false;
        Output       = string.Empty;
        ErrorMessage = string.Empty;

        _cts = new CancellationTokenSource();

        var progress = new Progress<string>(line =>
            Dispatcher.UIThread.Post(() =>
            {
                Output += line + Environment.NewLine;
                OnPropertyChanged(nameof(Output));
            }));

        try
        {
            var result = await ProvisionAsync(progress, _cts.Token).ConfigureAwait(false);

            Dispatcher.UIThread.Post(async () =>
            {
                IsRunning = false;

                await result.Match(
                    onSuccess: async () =>
                    {
                        IsComplete = true;
                        await OnProvisioningCompletedAsync().ConfigureAwait(false);
                    },
                    onFailure: async error =>
                    {
                        HasError = true;
                        ErrorMessage = error.Message ?? UiStrings.Get("AnUnknownErrorOccurred");
                        await OnProvisioningFailedAsync(error).ConfigureAwait(false);
                    });
            });
        }
        catch (OperationCanceledException)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                IsRunning = false;
                await OnProvisioningCancelledAsync().ConfigureAwait(false);
            });
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanInstall() => !IsRunning;

    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cts?.Cancel();

    [RelayCommand(CanExecute = nameof(CanRetry))]
    private void Retry()
    {
        HasError = false;
        InstallCommand.Execute(null);
    }

    private bool CanRetry() => HasError && !IsRunning;

    partial void OnIsRunningChanged(bool value)
    {
        InstallCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
        RetryCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ShowConsole));
    }

    partial void OnIsCompleteChanged(bool value) =>
        OnPropertyChanged(nameof(ShowConsole));

    partial void OnHasErrorChanged(bool value)
    {
        RetryCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(ShowConsole));
    }

    protected abstract Task<Result> ProvisionAsync(IProgress<string> progress, CancellationToken cancellationToken);

    protected virtual Task OnProvisioningCompletedAsync() => Task.CompletedTask;
    protected virtual Task OnProvisioningCancelledAsync() => Task.CompletedTask;
    protected virtual Task OnProvisioningFailedAsync(Error error) => Task.CompletedTask;
}
