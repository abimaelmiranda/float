using Avalonia.Controls;
using Avalonia.Interactivity;
using Float.Core.Models;
using Float.UI.ViewModels;

namespace Float.UI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

    private void OnStartContainerClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, container => ViewModel?.StartContainerCommand.Execute(container));

    private void OnStopContainerClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, container => ViewModel?.StopContainerCommand.Execute(container));

    private void OnRestartContainerClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, container => ViewModel?.RestartContainerCommand.Execute(container));

    private void OnRequestDeleteClick(object? sender, RoutedEventArgs e) =>
        ExecuteContainerCommand(sender, container => ViewModel?.RequestDeleteCommand.Execute(container));

    private void OnCloseDetailClick(object? sender, RoutedEventArgs e) =>
        ViewModel?.CloseDetailCommand.Execute(null);

    private static void ExecuteContainerCommand(object? sender, Action<Container> execute)
    {
        if (sender is Button { Tag: Container container })
            execute(container);
    }
}
