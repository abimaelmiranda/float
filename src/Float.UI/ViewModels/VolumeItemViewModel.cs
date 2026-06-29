using Float.Core.Models;
using Float.UI.Resources;

namespace Float.UI.ViewModels;

public sealed class VolumeItemViewModel
{
    public ContainerVolumeInfo Source { get; }
    public string Name { get; }
    public string Driver { get; }
    public string Format { get; }
    public string Size { get; }
    public string SourcePath { get; }
    public string Kind => Source.IsAnonymous ? UiStrings.Get("Anonymous") : UiStrings.Get("Named");
    public string DetailLabel => string.IsNullOrWhiteSpace(Size) ? Kind : $"{Kind} · {Size}";

    public VolumeItemViewModel(ContainerVolumeInfo source)
    {
        Source = source;
        Name = source.Name;
        Driver = source.Driver;
        Format = source.Format;
        Size = source.Size;
        SourcePath = source.Source;
    }
}
