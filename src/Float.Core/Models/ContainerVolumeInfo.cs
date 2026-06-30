namespace Float.Core.Models;

public sealed record ContainerVolumeInfo
{
    public required string Name { get; init; }
    public string Driver { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string Size { get; init; } = string.Empty;
    public long? SizeInBytes { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool IsAnonymous => Name.StartsWith("anon-", StringComparison.OrdinalIgnoreCase);
}
