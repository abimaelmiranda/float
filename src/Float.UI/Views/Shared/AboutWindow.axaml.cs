using System.Reflection;
using Avalonia.Controls;

namespace Float.UI.Views.Shared;

public partial class AboutWindow : Window
{
    public string VersionText { get; } = $"Version {GetVersion()}";

    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void OnClose(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private static string GetVersion()
    {
        var assembly = typeof(App).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        var version = string.IsNullOrWhiteSpace(informationalVersion)
            ? assembly.GetName().Version?.ToString() ?? "unknown"
            : informationalVersion;

        return version.Split('+')[0];
    }
}
