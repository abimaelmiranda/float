namespace Float.Infrastructure.Engines.Docker.Json.Dtos.Container;

public sealed class DockerInspectState
{
    public string? Status { get; init; }
    public DockerInspectHealth? Health { get; init; }
}
