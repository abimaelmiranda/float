namespace Float.Infrastructure.Engines.Docker.Json;

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

public sealed class DockerImageManifestDescriptor
{
    public DockerImagePlatform? Platform { get; init; }
}

public sealed class DockerImagePlatform
{
    public string? Architecture { get; init; }
    public string? Os { get; init; }
}

public sealed class DockerInspectImage
{
    public string? Architecture { get; init; }
}

public sealed class DockerInspectState
{
    public string? Status { get; init; }
    public DockerInspectHealth? Health { get; init; }
}

public sealed class DockerInspectHealth
{
    public string? Status { get; init; }
}

public sealed class DockerInspectHostConfig
{
    public string[]? Binds { get; init; }
    public Dictionary<string, DockerInspectPortBinding[]?>? PortBindings { get; init; }
}

public sealed class DockerInspectPortBinding
{
    public string? HostPort { get; init; }
}

public sealed class DockerInspectConfig
{
    public string? Image { get; init; }
    public string[]? Env { get; init; }
}

public sealed class DockerInspectMount
{
    public string? Type { get; init; }
    public string? Name { get; init; }
    public string? Source { get; init; }
    public string? Destination { get; init; }
    public bool RW { get; init; }
}
