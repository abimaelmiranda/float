using CommunityToolkit.Mvvm.ComponentModel;

namespace Float.UI.ViewModels.Wizards;

public partial class PortMappingItem : ObservableObject
{
    public static string[] Protocols { get; } = ["TCP", "UDP"];

    [ObservableProperty] public partial string HostPort { get; set; } = "";
    [ObservableProperty] public partial string ContainerPort { get; set; } = "";
    [ObservableProperty] public partial string Protocol { get; set; } = "TCP";
}
