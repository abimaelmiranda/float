using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading;
using Float.Core.Enums;
using Float.Core.Models;

namespace Float.UI.ViewModels.Wizards;

public partial class MigrationContainerItemViewModel : ObservableObject
{
    public Container Source { get; }
    public string Name { get; }
    public string ImageTag { get; }
    public string StatusLabel { get; }
    public string PortSummary { get; }
    public string VolumeSummary { get; }
    public string ArchitectureLabel => EffectiveArchitecture?.ToDisplayValue() ?? "Architecture not detected";
    public ContainerArchitecture? EffectiveArchitecture
        => ContainerArchitectureExtensions.ParseContainerArchitectureOrNull(ManualArchitecture) ?? Source.Architecture;
    public bool NeedsArchitectureSelection => IsSelected && EffectiveArchitecture is null;
    public bool HasNamedVolumes => Source.Volumes.Any(volume => volume.IsNamedVolume);
    public bool IsRunning => Source.Instance?.Status == ContainerStatus.Running;
    public bool CanCleanup => MigrationSucceeded;
    public bool HasLog => !string.IsNullOrWhiteSpace(LogText);
    public string ToggleLogLabel => IsLogExpanded ? "Hide details" : "Show details";

    [ObservableProperty] public partial bool IsSelected { get; set; }
    [ObservableProperty] public partial bool IsCleanupSelected { get; set; }
    [ObservableProperty] public partial bool IsLogExpanded { get; set; }
    [ObservableProperty] public partial bool IsMigrating { get; set; }
    [ObservableProperty] public partial bool MigrationSucceeded { get; set; }
    [ObservableProperty] public partial bool MigrationFailed { get; set; }
    [ObservableProperty] public partial bool CleanupSucceeded { get; set; }
    [ObservableProperty] public partial bool CleanupFailed { get; set; }
    [ObservableProperty] public partial string MigrationStatus { get; set; } = "Waiting";
    [ObservableProperty] public partial string LogText { get; set; } = "";
    [ObservableProperty] public partial string? ManualArchitecture { get; set; }

    public MigrationContainerItemViewModel(Container source, bool isSelected = false)
    {
        Source = source;
        IsSelected = isSelected;
        Name = source.Name;
        ImageTag = source.Image.Tag;
        StatusLabel = source.Instance?.Status.ToString()
            ?? throw new InvalidOperationException($"Container '{source.Name}' is missing instance status.");
        PortSummary = BuildPortSummary(source.Ports);
        VolumeSummary = BuildVolumeSummary(source.Volumes);
    }

    private static string BuildPortSummary(IEnumerable<ContainerPortMapping> ports)
    {
        var items = ports.Select(port =>
        {
            var suffix = port.Protocol == NetworkProtocol.Udp ? "/udp" : string.Empty;
            return $"{port.HostPort}->{port.ContainerPort}{suffix}";
        }).ToArray();

        return items.Length == 0 ? "No published ports" : string.Join(", ", items);
    }

    private static string BuildVolumeSummary(IReadOnlyCollection<ContainerVolume> volumes)
    {
        var summary = volumes.Count switch
        {
            0 => "No volumes",
            1 => "1 volume",
            _ => $"{volumes.Count} volumes"
        };

        return volumes.Any(volume => volume.IsNamedVolume)
            ? $"{summary}, named volume skipped"
            : summary;
    }

    partial void OnMigrationSucceededChanged(bool value)
    {
        OnPropertyChanged(nameof(CanCleanup));
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(NeedsArchitectureSelection));
    }

    partial void OnManualArchitectureChanged(string? value)
    {
        OnPropertyChanged(nameof(EffectiveArchitecture));
        OnPropertyChanged(nameof(ArchitectureLabel));
        OnPropertyChanged(nameof(NeedsArchitectureSelection));
    }

    [RelayCommand]
    private void ToggleLog()
    {
        IsLogExpanded = !IsLogExpanded;
    }

    public void ClearLog()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            ClearLogCore();
            return;
        }

        Dispatcher.UIThread.Post(ClearLogCore);
    }

    public void AppendLogLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return;

        if (Dispatcher.UIThread.CheckAccess())
        {
            AppendLogLineCore(line);
            return;
        }

        Dispatcher.UIThread.Post(() => AppendLogLineCore(line));
    }

    private void ClearLogCore()
    {
        LogText = "";
        IsLogExpanded = false;
        OnPropertyChanged(nameof(HasLog));
    }

    private void AppendLogLineCore(string line)
    {
        LogText += line + Environment.NewLine;
        OnPropertyChanged(nameof(HasLog));
    }

    partial void OnIsLogExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(ToggleLogLabel));
    }

    partial void OnLogTextChanged(string value)
    {
        OnPropertyChanged(nameof(HasLog));
    }

}
