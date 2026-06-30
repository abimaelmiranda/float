using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Abstractions.Services;
using Float.Core.Models;

namespace Float.UI.ViewModels;

public partial class ContainerDetailViewModel : ViewModelBase
{
    private readonly IContainerLogReader _logReader;
    private readonly Action _goBack;
    private CancellationTokenSource? _logsCts;
    private Task? _logsTask;

    public ContainerItemViewModel Container { get; }
    public IReadOnlyList<ContainerVolume> Volumes => Container.Source.Volumes;
    public ObservableCollection<string> LogLines { get; } = [];

    [ObservableProperty] public partial bool IsLoadingLogs { get; set; }

    public ContainerDetailViewModel(ContainerItemViewModel container, IContainerLogReader logReader, Action goBack)
    {
        Container = container;
        _logReader = logReader;
        _goBack = goBack;
    }

    [RelayCommand]
    private void Back() => _goBack();

    public void StartLogs()
    {
        if (_logsTask is { IsCompleted: false }) return;

        StopLogs();
        LogLines.Clear();
        _logsCts = new CancellationTokenSource();
        var ct = _logsCts.Token;
        IsLoadingLogs = true;
        _logsTask = Task.Run(async () =>
        {
            await _logReader.ReadLogsAsync(
                Container.Name,
                line => Dispatcher.UIThread.Post(() =>
                {
                    LogLines.Add(line);
                    IsLoadingLogs = false;
                }),
                ct).ConfigureAwait(false);

            await Dispatcher.UIThread.InvokeAsync(() => IsLoadingLogs = false);
        }, ct);
    }

    public void StopLogs()
    {
        _logsCts?.Cancel();
        _logsCts?.Dispose();
        _logsCts = null;
        _logsTask = null;
        IsLoadingLogs = false;
    }
}
