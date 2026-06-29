using Float.Core.Enums;

namespace Float.Core.Models;

public sealed record ContainerVolumeCreateRequest
{
    public required string Name { get; init; }
    public decimal? SizeValue { get; init; }
    public ContainerVolumeSizeUnit SizeUnit { get; init; } = ContainerVolumeSizeUnit.GB;
    public ContainerVolumeJournalMode JournalMode { get; init; } = ContainerVolumeJournalMode.Default;
    public decimal? JournalSizeValue { get; init; }
    public ContainerVolumeSizeUnit JournalSizeUnit { get; init; } = ContainerVolumeSizeUnit.MB;
}
