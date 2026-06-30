using Float.Core.Enums;
using Float.Core.Models;
using Float.Infrastructure.Engines.Docker.Json.Dtos.Container;

namespace Float.Infrastructure.Engines.Docker;

internal static class DockerContainerMapper
{
    public static Container ToContainer(
        DockerInspectContainer src,
        IReadOnlyDictionary<string, ContainerArchitecture> imageArchitectures)
    {
        var name = NormalizeContainerName(src.Name);
        var image = RequireValue(src.Config?.Image, name, "Config.Image");
        var architecture = ResolveArchitecture(src, imageArchitectures);

        return new Container
        {
            Name = name,
            Image = new ContainerImage { Tag = image },
            Architecture = architecture,
            Ports = MapPorts(name, src.HostConfig?.PortBindings),
            Volumes = MapVolumes(name, src),
            EnvironmentVariables = MapEnvironment(name, src.Config?.Env),
            Instance = new ContainerInstance
            {
                Status = MapStatus(name, src.State?.Status),
                Health = MapHealth(src.State?.Health?.Status),
                CreatedAt = ParseDate(src.Created, name, "Created")
            }
        };
    }

    public static string? GetRawArchitecture(DockerInspectContainer src)
        => src.ImageManifestDescriptor?.Platform?.Architecture
            ?? src.Architecture;

    private static ContainerArchitecture? ResolveArchitecture(
        DockerInspectContainer src,
        IReadOnlyDictionary<string, ContainerArchitecture> imageArchitectures)
    {
        var architecture = ContainerArchitectureExtensions.ParseContainerArchitectureOrNull(GetRawArchitecture(src));
        if (architecture is not null)
            return architecture;

        if (src.Config?.Image is not null
            && imageArchitectures.TryGetValue(src.Config.Image, out var configArchitecture))
            return configArchitecture;

        if (src.Image is not null
            && imageArchitectures.TryGetValue(src.Image, out var imageArchitecture))
            return imageArchitecture;

        return null;
    }

    private static ContainerPortMapping[] MapPorts(
        string containerName,
        Dictionary<string, DockerInspectPortBinding[]?>? bindings)
    {
        if (bindings is null)
            return [];

        return bindings
            .SelectMany(binding =>
            {
                var containerPort = ParseContainerPort(containerName, binding.Key);
                var protocol = ParseProtocol(containerName, binding.Key);
                return binding.Value?
                    .Select(p => new ContainerPortMapping(
                        ParseHostPort(containerName, binding.Key, p.HostPort),
                        containerPort,
                        protocol)) ?? Enumerable.Empty<ContainerPortMapping>();
            })
            .ToArray();
    }

    private static ContainerVolume[] MapVolumes(string containerName, DockerInspectContainer src)
        => src.Mounts?
            .Select(mount => MapMount(containerName, mount))
            .ToArray() ?? [];

    private static ContainerVolume MapMount(string containerName, DockerInspectMount mount)
    {
        var isNamedVolume = mount.Type?.Equals("volume", StringComparison.OrdinalIgnoreCase) == true;
        var hostPath = isNamedVolume
            ? RequireValue(mount.Name ?? mount.Source, containerName, "Mount.Name")
            : NormalizeDockerDesktopPath(RequireValue(mount.Source, containerName, "Mount.Source"));

        return new ContainerVolume(
            hostPath,
            RequireValue(mount.Destination, containerName, "Mount.Destination"),
            !mount.RW,
            isNamedVolume,
            mount.Name);
    }


    private static ContainerEnvironment[] MapEnvironment(string containerName, string[]? env)
        => env?
            .Select(e => e.Split('=', 2))
            .Select(parts => parts.Length == 2
                ? new ContainerEnvironment(parts[0], parts[1])
                : throw new InvalidOperationException($"Docker container '{containerName}' has invalid environment entry."))
            .ToArray() ?? [];

    private static string NormalizeContainerName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Docker inspect returned a container without Name.");

        return name.TrimStart('/');
    }


    private static string NormalizeDockerDesktopPath(string path)
    {
        const string hostMountPrefix = "/host_mnt";
        return path.StartsWith(hostMountPrefix + "/", StringComparison.Ordinal)
            ? path[hostMountPrefix.Length..]
            : path;
    }

    private static int ParseContainerPort(string containerName, string portKey)
    {
        var slashIndex = portKey.IndexOf('/');
        var portValue = slashIndex >= 0 ? portKey[..slashIndex] : portKey;
        if (!int.TryParse(portValue, out var port))
            throw new InvalidOperationException($"Docker container '{containerName}' has invalid container port '{portKey}'.");

        return port;
    }

    private static int ParseHostPort(string containerName, string portKey, string? hostPort)
    {
        if (!int.TryParse(hostPort, out var port))
            throw new InvalidOperationException($"Docker container '{containerName}' has invalid host port for '{portKey}'.");

        return port;
    }

    private static NetworkProtocol ParseProtocol(string containerName, string portKey)
    {
        var slashIndex = portKey.IndexOf('/');
        if (slashIndex < 0)
            return NetworkProtocol.Tcp;

        var protocol = portKey[(slashIndex + 1)..];
        return protocol.ToLowerInvariant() switch
        {
            "tcp" => NetworkProtocol.Tcp,
            "udp" => NetworkProtocol.Udp,
            _ => throw new InvalidOperationException($"Docker container '{containerName}' has unknown port protocol '{protocol}'.")
        };
    }

    private static ContainerStatus MapStatus(string containerName, string? status)
        => status?.ToLowerInvariant() switch
        {
            "created" => ContainerStatus.Created,
            "running" => ContainerStatus.Running,
            "restarting" => ContainerStatus.Restarting,
            "paused" => ContainerStatus.Paused,
            "exited" => ContainerStatus.Exited,
            "dead" => ContainerStatus.Removed,
            _ => throw new InvalidOperationException($"Docker container '{containerName}' has unknown status '{status}'.")
        };

    private static ContainerHealth MapHealth(string? status)
        => status?.ToLowerInvariant() switch
        {
            "healthy" => ContainerHealth.Healthy,
            "unhealthy" => ContainerHealth.Unhealthy,
            _ => ContainerHealth.Unknown
        };

    private static DateTimeOffset ParseDate(string? value, string containerName, string propertyName)
    {
        if (!DateTimeOffset.TryParse(value, out var date))
            throw new InvalidOperationException($"Docker container '{containerName}' has invalid {propertyName} value '{value}'.");

        return date;
    }

    private static string RequireValue(string? value, string containerName, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Docker container '{containerName}' is missing required field {propertyName}.");

        return value;
    }
}
