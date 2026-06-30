using CommunityToolkit.Mvvm.ComponentModel;
using Float.Core.Enums;
using Float.Core.Models;

namespace Float.UI.ViewModels;

public partial class ContainerItemViewModel : ViewModelBase
{
    public Container Source { get; }
    public string Name { get; }
    public string ImageTag { get; }
    public string PortSummary { get; }
    public ContainerStatus Status { get; }

    public bool IsRunning => Status == ContainerStatus.Running;
    public string StatusLabel => Status.ToString();
    [ObservableProperty] public partial bool IsBusy { get; set; }
    [ObservableProperty] public partial string BusyLabel { get; set; } = "";

    public ContainerItemViewModel(Container source)
    {
        Source = source;
        Name = source.Name;
        ImageTag = source.Image.Tag;
        PortSummary = BuildPortSummary(source.Ports);
        Status = source.Instance?.Status
            ?? throw new InvalidOperationException($"Container '{source.Name}' is missing instance status.");
    }

    private static string BuildPortSummary(IEnumerable<ContainerPortMapping> ports)
    {
        var items = ports.Select(port =>
        {
            var suffix = port.Protocol == NetworkProtocol.Udp ? "/udp" : string.Empty;
            return $"{port.HostPort}→{port.ContainerPort}{suffix}";
        }).ToArray();

        return items.Length == 0 ? "—" : string.Join(", ", items);
    }
}
