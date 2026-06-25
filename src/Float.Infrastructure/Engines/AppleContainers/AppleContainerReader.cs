using System.Text;
using System.Text.Json;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerReader : IContainerReader
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IEngineProvisioner _engineProvisioner;
    private readonly IProcessHost _processHost;

    public AppleContainerReader(
        [FromKeyedServices(ContainerEngine.AppleContainers)] IEngineProvisioner engineProvisioner,
        IProcessHost processHost)
    {
        _engineProvisioner = engineProvisioner;
        _processHost = processHost;
    }

    public async Task<Result<IReadOnlyList<Container>>> ListAsync(bool includeAll = true, CancellationToken cancellationToken = default)
    {
        if (!await _engineProvisioner.IsEngineInstalled())
            return Result.WithFailure<IReadOnlyList<Container>>(
                DomainErrors.EngineNotAvailable("Apple Container is not available."));

        var output = new StringBuilder();
        var args = new List<string> { "ls", "--format", "json" };

        if (includeAll)
        {
            args.Insert(1, "--all");
        }

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError:  line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<IReadOnlyList<Container>>(result.Failure);

        var outputStr = output.ToString();
        if (string.IsNullOrWhiteSpace(outputStr))
            return Result.WithSuccess<IReadOnlyList<Container>>([]);

        try
        {
            var containers = ParseOutput(outputStr);
            return Result.WithSuccess<IReadOnlyList<Container>>(containers);
        }
        catch (InvalidOperationException ex)
        {
            return Result.WithFailure<IReadOnlyList<Container>>(
                DomainErrors.ParseError(ex.Message));
        }
    }

    private static Container[] ParseOutput(string output)
    {
        try
        {
            var trimmed = output.TrimStart();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                var items = JsonSerializer.Deserialize(
                    output,
                    AppleContainerJsonContext.Default.AppleManagedContainerArray);
                return (items ?? throw new InvalidOperationException("Apple container list returned null JSON."))
                    .Select(Map)
                    .ToArray();
            }

            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => JsonSerializer.Deserialize(line, AppleContainerJsonContext.Default.AppleManagedContainer)
                    ?? throw new InvalidOperationException("Apple container list returned null JSON item."))
                .Select(Map)
                .ToArray();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Apple container list returned invalid JSON.", ex);
        }
    }

    private static Container Map(AppleManagedContainer src)
    {
        var config = src.Configuration;
        var status = src.Status;

        var name = RequireValue(src.Id, "container", "id");

        var image = new ContainerImage(RequireValue(config?.Image?.Reference, name, "configuration.image.reference"));

        var ports = config?.PublishedPorts?.Select(p => new ContainerPortMapping(
            p.HostPort,
            p.ContainerPort,
            MapProtocol(name, p.Proto)
        )).ToArray() ?? [];

        var envVars = config?.InitProcess?.Environment
            ?.Select(e => e.Split('=', 2))
            .Select(parts => parts.Length == 2
                ? new ContainerEnvironment(parts[0], parts[1])
                : throw new InvalidOperationException($"Apple container '{name}' has invalid environment entry."))
            .ToArray() ?? [];

        var volumes = config?.Mounts?.Select(m => new ContainerVolume(
            RequireValue(m.Source, name, "configuration.mounts.source"),
            RequireValue(m.Destination, name, "configuration.mounts.destination"),
            m.ReadOnly
        )).ToArray() ?? [];

        var createdAt = ParseDate(config?.CreationDate ?? status?.StartedDate, name);

        var containerStatus = status?.State?.ToLowerInvariant() switch
        {
            "running"  => ContainerStatus.Running,
            "stopping" => ContainerStatus.Exited,
            "stopped"  => ContainerStatus.Exited,
            "created"  => ContainerStatus.Created,
            _          => throw new InvalidOperationException($"Apple container '{name}' has unknown status '{status?.State}'.")
        };

        return new Container
        {
            Name                 = name,
            Image                = image,
            Ports                = ports,
            EnvironmentVariables = envVars,
            Volumes              = volumes,
            Instance             = new ContainerInstance
            {
                Status    = containerStatus,
                Health    = ContainerHealth.Unknown,
                CreatedAt = createdAt
            }
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
