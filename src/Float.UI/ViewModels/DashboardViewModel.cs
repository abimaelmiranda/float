using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Enums;

namespace Float.UI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial ContainerItemViewModel? SelectedContainer { get; set; }

    public bool HasSelectedContainer => SelectedContainer is not null;

    public ObservableCollection<ContainerItemViewModel> Containers { get; } = new()
    {
        new ContainerItemViewModel("nginx", "nginx:latest", "80→8080, 443→8443", ContainerStatus.Running),
        new ContainerItemViewModel("postgres", "postgres:16", "5432→5432", ContainerStatus.Running),
        new ContainerItemViewModel("redis", "redis:alpine", "—", ContainerStatus.Exited),
        new ContainerItemViewModel("node-dev", "node:20-alpine", "3000→3000", ContainerStatus.Exited),
    };

    partial void OnSelectedContainerChanged(ContainerItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedContainer));
    }

    [RelayCommand]
    private void NewContainer() { }

    [RelayCommand]
    private void RefreshContainers() { }

    [RelayCommand]
    private void CloseDetail() => SelectedContainer = null;
}
