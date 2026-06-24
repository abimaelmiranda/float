using CommunityToolkit.Mvvm.ComponentModel;

namespace Float.UI.ViewModels.Wizards;

public partial class EnvVarItem : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "";
    [ObservableProperty] public partial string Value { get; set; } = "";
}
