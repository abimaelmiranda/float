using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Float.Core.Enums;

namespace Float.UI.ViewModels;

public partial class ContainerItemViewModel : ViewModelBase
{
    public string Name { get; }
    public string ImageTag { get; }
    public string PortSummary { get; }
    public ContainerStatus Status { get; }

    public bool IsRunning => Status == ContainerStatus.Running;
    public string StatusLabel => Status.ToString();

    public ContainerItemViewModel(string name, string imageTag, string portSummary, ContainerStatus status)
    {
        Name = name;
        ImageTag = imageTag;
        PortSummary = portSummary;
        Status = status;
    }

    [RelayCommand]
    private void Start() { }

    [RelayCommand]
    private void Stop() { }

    [RelayCommand]
    private void Restart() { }

    [RelayCommand]
    private void Delete() { }
}
