namespace Float.Infrastructure.Engines.Docker.Json.Dtos.Container;

public sealed class DockerInspectContainer
{
    public string? Name { get; init; }
    public string? Created { get; init; }
    public string? Image { get; init; }
    public string? Architecture { get; init; }
    public string? Platform { get; init; }
    public DockerImageManifestDescriptor? ImageManifestDescriptor { get; init; }
    public DockerInspectState? State { get; init; }
    public DockerInspectHostConfig? HostConfig { get; init; }
    public DockerInspectConfig? Config { get; init; }
    public DockerInspectMount[]? Mounts { get; init; }
}
