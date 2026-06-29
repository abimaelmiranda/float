using Float.Core.Enums;
using Float.Core.Models;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Image;

namespace Float.Infrastructure.Engines.AppleContainers;

internal static class AppleContainerMapper
{
    public static Container ToContainer(AppleManagedContainer src)
    {
        var config = src.Configuration;
        var status = src.Status;
        var name = RequireValue(src.Id, "container", "id");
        var image = new ContainerImage { Tag = RequireValue(config?.Image?.Reference, name, "configuration.image.reference") };
        var ports = config?.PublishedPorts?.Select(p => new ContainerPortMapping(
            p.HostPort,
            p.ContainerPort,
            MapProtocol(name, p.Proto))).ToArray() ?? [];
        var envVars = config?.InitProcess?.Environment
            ?.Select(e => e.Split('=', 2))
            .Select(parts => parts.Length == 2
                ? new ContainerEnvironment(parts[0], parts[1])
                : throw new InvalidOperationException($"Apple container '{name}' has invalid environment entry."))
            .ToArray() ?? [];
        var volumes = config?.Mounts?.Select(m => new ContainerVolume(
            RequireValue(m.Source, name, "configuration.mounts.source"),
            RequireValue(m.Destination, name, "configuration.mounts.destination"),
            m.ReadOnly)).ToArray() ?? [];

        return new Container
        {
            Name = name,
            Image = image,
            Ports = ports,
            EnvironmentVariables = envVars,
            Volumes = volumes,
            Instance = new ContainerInstance
            {
                Status = MapStatus(name, status?.State),
                Health = ContainerHealth.Unknown,
                CreatedAt = ParseDate(config?.CreationDate ?? status?.StartedDate, name)
            }
        };
    }

    public static ContainerImage ToImage(AppleContainerImage src)
    {
        var name = RequireValue(src.Configuration?.Name, "image", "configuration.name");
        var platform = src.Variants.FirstOrDefault()?.Platform
            ?? throw new InvalidOperationException($"Apple image '{name}' is missing platform information.");

        return new ContainerImage
        {
            Id = RequireValue(src.Id, name, "id"),
            Name = name,
            Tag = name,
            Architecture = ContainerArchitectureExtensions.ParseContainerArchitectureOrNull(platform.Architecture),
            Os = RequireValue(platform.Os, name, "variants.platform.os")
        };
    }

    private static NetworkProtocol MapProtocol(string containerName, string? protocol)
        => protocol?.ToLowerInvariant() switch
        {
            "tcp" => NetworkProtocol.Tcp,
            "udp" => NetworkProtocol.Udp,
            null or "" => throw new InvalidOperationException($"Apple container '{containerName}' is missing port protocol."),
            _ => throw new InvalidOperationException($"Apple container '{containerName}' has unknown port protocol '{protocol}'.")
        };

    private static ContainerStatus MapStatus(string containerName, string? status)
        => status?.ToLowerInvariant() switch
        {
            "running" => ContainerStatus.Running,
            "stopping" => ContainerStatus.Exited,
            "stopped" => ContainerStatus.Exited,
            "created" => ContainerStatus.Created,
            _ => throw new InvalidOperationException($"Apple container '{containerName}' has unknown status '{status}'.")
        };

    private static DateTimeOffset ParseDate(string? value, string containerName)
    {
        if (!DateTimeOffset.TryParse(value, out var date))
            throw new InvalidOperationException($"Apple container '{containerName}' has invalid date value '{value}'.");

        return date;
    }

    private static string RequireValue(string? value, string containerName, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Apple container '{containerName}' is missing required field {propertyName}.");

        return value;
    }
}
