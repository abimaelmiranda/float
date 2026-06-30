using Avalonia.Markup.Xaml;

namespace Float.UI.Views;

public partial class SettingsView : Avalonia.Controls.UserControl
{
    public SettingsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
