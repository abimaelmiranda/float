using System.Text;
using System.Text.Json;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;
using Float.Infrastructure.Engines.Docker.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Float.Infrastructure.Engines.Docker;

public class DockerReader : IContainerReader
{
    public ContainerEngine Engine => ContainerEngine.Docker;

    private readonly IProcessHost _processHost;
    private readonly IEngineProvisioner _engineProvisioner;

    public DockerReader(
        IProcessHost processHost,
        [FromKeyedServices(ContainerEngine.Docker)] IEngineProvisioner engineProvisioner)
    {
        _processHost = processHost;
        _engineProvisioner = engineProvisioner;
    }

    public async Task<Result<IReadOnlyList<Container>>> ListAsync(
        bool includeAll = true,
        CancellationToken cancellationToken = default)
    {
        if (!await _engineProvisioner.IsEngineInstalled())
            return Result.WithFailure<IReadOnlyList<Container>>(
                DomainErrors.EngineNotAvailable("Docker is not available."));

        var idsResult = await ListContainerIdsAsync(includeAll, cancellationToken).ConfigureAwait(false);
        if (idsResult.IsFailure)
            return Result.WithFailure<IReadOnlyList<Container>>(idsResult.Failure);

        var ids = idsResult.GetValueOrThrow();
        if (ids.Length == 0)
            return Result.WithSuccess<IReadOnlyList<Container>>([]);

        var inspectedResult = await InspectAsync(ids, cancellationToken).ConfigureAwait(false);
        if (inspectedResult.IsFailure)
            return Result.WithFailure<IReadOnlyList<Container>>(inspectedResult.Failure);

        var inspected = inspectedResult.GetValueOrThrow();
        var imageArchitectures = await ResolveMissingImageArchitecturesAsync(inspected, cancellationToken)
            .ConfigureAwait(false);

        var containers = inspected.Select(container => Map(container, imageArchitectures)).ToArray();
        return Result.WithSuccess<IReadOnlyList<Container>>(containers);
    }

    private async Task<Result<string[]>> ListContainerIdsAsync(bool includeAll, CancellationToken cancellationToken)
    {
        var args = new List<string> { "ps", "--quiet" };
        if (includeAll)
            args.Add("--all");

        var output = new StringBuilder();
        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "docker",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<string[]>(result.Failure);

        var ids = output
            .ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Result.WithSuccess(ids);
    }

    private async Task<Result<DockerInspectContainer[]>> InspectAsync(
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var args = new List<string> { "inspect", };
        args.AddRange(ids);

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "docker",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<DockerInspectContainer[]>(result.Failure);

        if (output.Length == 0)
            return Result.WithFailure<DockerInspectContainer[]>(
                DomainErrors.CommandFailed("Docker command 'inspect' returned no output."));

        try
        {
            var containers = JsonSerializer.Deserialize(
                output.ToString(),
                DockerContainerJsonContext.Default.DockerInspectContainerArray);

            if (containers is null)
                return Result.WithFailure<DockerInspectContainer[]>(
                    DomainErrors.ParseError("Docker command 'inspect' returned null JSON."));

            return Result.WithSuccess(containers);
        }
        catch (JsonException ex)
        {
            return Result.WithFailure<DockerInspectContainer[]>(
                DomainErrors.ParseError($"Docker command 'inspect' returned invalid JSON. {ex.Message}"));
        }
    }

    private async Task<Dictionary<string, string>> ResolveMissingImageArchitecturesAsync(
        IReadOnlyList<DockerInspectContainer> containers,
        CancellationToken cancellationToken)
    {
        var refs = containers
            .Where(container => NormalizeArchitectureOrNull(GetRawArchitecture(container)) is null)
            .SelectMany(container => new[] { container.Config?.Image, container.Image })
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .Select(image => image!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (refs.Length == 0)
            return [];

        var architectures = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var imageRef in refs)
        {
            var result = await InspectImageArchitectureAsync(imageRef, cancellationToken)
                .ConfigureAwait(false);
            if (result.IsSuccess && result.GetValueOrThrow() is not null)
                architectures[imageRef] = result.GetValueOrThrow()!;
        }

        return architectures;
    }

    private async Task<Result<string?>> InspectImageArchitectureAsync(
        string imageRef,
        CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "docker",
            ["image", "inspect", imageRef],
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<string?>(result.Failure);

        if (output.Length == 0)
            return Result.WithFailure<string?>(
                DomainErrors.CommandFailed($"Docker command 'image inspect' returned no output for image '{imageRef}'."));

        try
        {
            var images = JsonSerializer.Deserialize(
                output.ToString(),
                DockerContainerJsonContext.Default.DockerInspectImageArray);

            if (images is null)
                return Result.WithFailure<string?>(
                    DomainErrors.ParseError($"Docker command 'image inspect' returned null JSON for image '{imageRef}'."));

            if (images.Length == 0)
                return Result.WithFailure<string?>(
                    DomainErrors.ParseError($"Docker command 'image inspect' returned no image entries for '{imageRef}'."));

            return Result.WithSuccess<string?>(NormalizeArchitectureOrNull(images[0].Architecture));
        }
        catch (JsonException ex)
        {
            return Result.WithFailure<string?>(
                DomainErrors.ParseError($"Docker command 'image inspect' returned invalid JSON for image '{imageRef}'. {ex.Message}"));
        }
    }

    private static Container Map(
        DockerInspectContainer src,
        IReadOnlyDictionary<string, string> imageArchitectures)
    {
        var name = NormalizeContainerName(src.Name);
        var image = RequireValue(src.Config?.Image, name, "Config.Image");
        var createdAt = ParseDate(src.Created, name, "Created");
        var architecture = ResolveArchitecture(src, imageArchitectures);

        return new Container
        {
            Name = name,
            Image = new ContainerImage(image),
            Architecture = architecture,
            Ports = MapPorts(name, src.HostConfig?.PortBindings),
            Volumes = MapVolumes(name, src),
            EnvironmentVariables = MapEnvironment(name, src.Config?.Env),
            Instance = new ContainerInstance
            {
                Status = MapStatus(name, src.State?.Status),
                Health = MapHealth(src.State?.Health?.Status),
                CreatedAt = createdAt
            }
        };
    }

    private static string? ResolveArchitecture(
        DockerInspectContainer src,
        IReadOnlyDictionary<string, string> imageArchitectures)
    {
        var architecture = NormalizeArchitectureOrNull(GetRawArchitecture(src));
        if (architecture is not null)
            return architecture;

        if (src.Config?.Image is not null
            && imageArchitectures.TryGetValue(src.Config.Image, out architecture))
            return architecture;

        if (src.Image is not null
            && imageArchitectures.TryGetValue(src.Image, out architecture))
            return architecture;

        return null;
    }

    private static string? GetRawArchitecture(DockerInspectContainer src)
        => src.ImageManifestDescriptor?.Platform?.Architecture
            ?? src.Architecture;

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
    {
        var binds = src.HostConfig?.Binds?
            .Select(bind => ParseBind(containerName, bind))
            .ToArray();

        if (binds is { Length: > 0 })
            return binds;

        return src.Mounts?
            .Select(mount => MapMount(containerName, mount))
            .ToArray() ?? [];
    }

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

    private static ContainerVolume ParseBind(string containerName, string bind)
    {
        var parts = bind.Split(':', 3);
        if (parts.Length < 2)
            throw new InvalidOperationException($"Docker container '{containerName}' has invalid bind mount '{bind}'.");

        var mode = parts.Length == 3 ? parts[2] : string.Empty;
        var isNamedVolume = IsLikelyNamedVolume(parts[0]);
        return new ContainerVolume(
            isNamedVolume ? parts[0] : NormalizeDockerDesktopPath(parts[0]),
            parts[1],
            mode.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(m => m.Equals("ro", StringComparison.OrdinalIgnoreCase)),
            isNamedVolume,
            isNamedVolume ? parts[0] : null);
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

    private static bool IsLikelyNamedVolume(string source)
        => !source.StartsWith("/", StringComparison.Ordinal)
            && !source.StartsWith("~", StringComparison.Ordinal)
            && !source.Contains('\\', StringComparison.Ordinal);

    private static string? NormalizeArchitectureOrNull(string? architecture)
        => architecture?.ToLowerInvariant() switch
        {
            "linux/amd64" => "amd64",
            "x86_64" => "amd64",
            "amd64" => "amd64",
            "linux/arm64" => "arm64",
            "aarch64" => "arm64",
            "arm64" => "arm64",
            _ => null
        };

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
