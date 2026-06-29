using Avalonia.Controls;
using Avalonia.Interactivity;
using Float.Core.Models;
using Float.UI.ViewModels;

namespace Float.UI.Views;

public partial class ContainerDetailView : UserControl
{
    private const int LogsTabIndex = 2;

    public ContainerDetailView()
    {
        InitializeComponent();
    }

    private DashboardViewModel? DashboardVm =>
        Parent?.DataContext as DashboardViewModel ?? DataContext as DashboardViewModel;

    private void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not ContainerDetailViewModel vm) return;
        if (sender is TabControl tc && tc.SelectedIndex == LogsTabIndex)
            vm.StartLogs();
        else
            vm.StopLogs();
    }

    private void OnStartContainerClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, c => GetDashboard()?.StartContainerCommand.Execute(c));

    private void OnStopContainerClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, c => GetDashboard()?.StopContainerCommand.Execute(c));

    private void OnRestartContainerClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, c => GetDashboard()?.RestartContainerCommand.Execute(c));

    private void OnRequestDeleteClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, c => GetDashboard()?.RequestDeleteCommand.Execute(c));

    private DashboardViewModel? GetDashboard()
    {
        var p = Parent;
        while (p is not null)
        {
            if (p.DataContext is DashboardViewModel vm) return vm;
            p = p.Parent as Control;
        }
        return null;
    }

    private static void ExecuteContainerCommand(object? sender, Action<Container> execute)
    {
        if (sender is Button { Tag: Container container })
            execute(container);
    }
}
