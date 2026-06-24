namespace Float.Core.Models;

public sealed record Container
{
    public string Name { get; init; } = string.Empty;

    public ContainerImage Image { get; init; } = default!;

    public IReadOnlyList<ContainerEnvironment> EnvironmentVariables { get; init; } = [];

    public IReadOnlyList<ContainerVolume> Volumes { get; init; } = [];

    public IReadOnlyList<ContainerPortMapping> Ports { get; init; } = [];

    public ContainerInstance? Instance { get; init; }
}
