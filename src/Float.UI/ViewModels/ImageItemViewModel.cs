using Float.Core.Models;
using Float.Core.Enums;

namespace Float.UI.ViewModels;

public sealed class ImageItemViewModel
{
    public ContainerImage Source { get; }
    public string Id { get; }
    public string IdShort { get; }
    public string Name { get; }
    public string Tag { get; }
    public string Architecture { get; }
    public string Os { get; }
    public string PlatformLabel => string.IsNullOrWhiteSpace(Architecture) && string.IsNullOrWhiteSpace(Os)
        ? "—"
        : $"{Architecture} · {Os}".Trim(' ', '·');

    public ImageItemViewModel(ContainerImage source)
    {
        Source = source;
        Id = source.Id;
        IdShort = source.Id.Length > 12 ? source.Id[..12] : source.Id;
        Name = source.Name;
        Tag = source.Tag;
        Architecture = source.Architecture?.ToDisplayValue() ?? "";
        Os = source.Os;
    }
}
