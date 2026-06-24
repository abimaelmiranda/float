using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerReader : IContainerReader
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IEngineProvisioner _engineProvisioner;
    private readonly IProcessHost _processHost;

    public AppleContainerReader(IEngineProvisioner engineProvisioner, IProcessHost processHost)
    {
        _engineProvisioner = engineProvisioner;
        _processHost = processHost;
    }

    public async Task<IReadOnlyList<Container>> ListAsync(CancellationToken cancellationToken = default)
    {
        if (!_engineProvisioner.IsEngineInstalled())
            throw new InvalidOperationException("Apple Container is not available.");

        var output = new StringBuilder();

        await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            ["ls", "--all", "--format", "json"],
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError:  _ => { },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ParseJson(output.ToString());
    }

    private static IReadOnlyList<Container> ParseJson(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        // Try JSON array first; fall back to NDJSON (one object per line)
        try
        {
            var items = JsonSerializer.Deserialize<AppleManagedContainer[]>(output, JsonOptions);
            return items?.Select(Map).ToArray() ?? [];
        }
        catch (JsonException)
        {
            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => JsonSerializer.Deserialize<AppleManagedContainer>(line, JsonOptions))
                .Where(c => c is not null)
                .Select(c => Map(c!))
                .ToArray();
        }
    }

    private static Container Map(AppleManagedContainer src)
    {
        var config = src.Configuration;
        var status = src.Status;

        // id IS the name — apple/container has no separate name field
        var name = src.Id ?? string.Empty;

        var image = new ContainerImage(config?.Image?.Reference ?? string.Empty);

        var ports = config?.PublishedPorts?.Select(p => new ContainerPortMapping(
            p.HostPort,
            p.ContainerPort,
            p.Proto?.Equals("udp", StringComparison.OrdinalIgnoreCase) is true
                ? NetworkProtocol.Udp
                : NetworkProtocol.Tcp
        )).ToArray() ?? [];

        var envVars = config?.InitProcess?.Environment
            ?.Select(e => e.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new ContainerEnvironment(parts[0], parts[1]))
            .ToArray() ?? [];

        var volumes = config?.Mounts?.Select(m => new ContainerVolume(
            m.Source ?? string.Empty,
            m.Destination ?? string.Empty,
            m.ReadOnly
        )).ToArray() ?? [];

        DateTimeOffset.TryParse(config?.CreationDate, out var createdAt);

        var containerStatus = status?.State?.ToLowerInvariant() switch
        {
            "running"  => ContainerStatus.Running,
            "stopping" => ContainerStatus.Exited,
            "stopped"  => ContainerStatus.Exited,
            "created"  => ContainerStatus.Created,
            _          => ContainerStatus.Unknown
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
