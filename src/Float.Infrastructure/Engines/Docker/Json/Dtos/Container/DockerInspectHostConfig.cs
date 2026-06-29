namespace Float.Infrastructure.Engines.Docker.Json.Dtos.Container;

public sealed class DockerInspectHostConfig
{
    public string[]? Binds { get; init; }
    public Dictionary<string, DockerInspectPortBinding[]?>? PortBindings { get; init; }
}
