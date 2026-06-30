namespace Float.Infrastructure.Engines.Docker.Json.Dtos.Container;

public sealed class DockerInspectMount
{
    public string? Type { get; init; }
    public string? Name { get; init; }
    public string? Source { get; init; }
    public string? Destination { get; init; }
    public bool RW { get; init; }
}
