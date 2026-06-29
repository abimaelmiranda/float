using Float.Core.Enums;

namespace Float.Core.Models;

public sealed record Container
{
    public required string Name { get; init; }

    public required ContainerImage Image { get; init; }

    public ContainerArchitecture? Architecture { get; init; }

    public IReadOnlyList<ContainerEnvironment> EnvironmentVariables { get; init; } = [];

    public IReadOnlyList<ContainerVolume> Volumes { get; init; } = [];

    public IReadOnlyList<ContainerPortMapping> Ports { get; init; } = [];

    public ContainerInstance? Instance { get; init; }
}
