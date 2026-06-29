using Float.Core.Enums;

namespace Float.Core.Models;

public record class ContainerImage
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;
    public ContainerArchitecture? Architecture { get; init; }
    public string Os { get; init; } = string.Empty;
}
