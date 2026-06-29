namespace Float.Infrastructure.Engines.Docker.Json.Dtos.Container;

public sealed class DockerInspectConfig
{
    public string? Image { get; init; }
    public string[]? Env { get; init; }
}
