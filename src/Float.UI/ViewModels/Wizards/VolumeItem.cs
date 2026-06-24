using CommunityToolkit.Mvvm.ComponentModel;

namespace Float.UI.ViewModels.Wizards;

public partial class VolumeItem : ObservableObject
{
    [ObservableProperty] public partial string HostPath { get; set; } = "";
    [ObservableProperty] public partial string ContainerPath { get; set; } = "";
    [ObservableProperty] public partial bool ReadOnly { get; set; }
}
