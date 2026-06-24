namespace Float.Core.Models;

public sealed record ContainerCreateRequest
{
    public required string ImageTag { get; init; }
    public string? Name { get; init; }
    public string Architecture { get; init; } = "arm64";
    public bool EnableRosetta { get; init; }
    public double? CpuCount { get; init; }
    public string? Memory { get; init; }
    public bool StartImmediately { get; init; } = true;
    public bool KeepAlive { get; init; }
    public IReadOnlyList<ContainerPortMapping> Ports { get; init; } = [];
    public IReadOnlyList<ContainerVolume> Volumes { get; init; } = [];
    public IReadOnlyList<ContainerEnvironment> EnvironmentVariables { get; init; } = [];
}
