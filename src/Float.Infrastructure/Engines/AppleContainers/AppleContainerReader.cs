using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
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

    public async Task<IReadOnlyList<Container>> ListAsync(bool includeAll = true, CancellationToken cancellationToken = default)
    {
        if (!_engineProvisioner.IsEngineInstalled())
            throw new InvalidOperationException("Apple Container is not available.");

        var output = new StringBuilder();
        var args = new List<string> { "ls", "--format", "json" };

        if (includeAll)
        {
            args.Insert(1, "--all");
        }

        await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError:  _ => { },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var outputStr = output.ToString();
        if (string.IsNullOrWhiteSpace(outputStr))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize(
                outputStr,
                AppleContainerJsonContext.Default.AppleManagedContainerArray);
            return items?.Select(Map).ToArray() ?? [];
        }
        catch (JsonException)
        {
            return outputStr
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => JsonSerializer.Deserialize(line, AppleContainerJsonContext.Default.AppleManagedContainer))
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

        DateTimeOffset.TryParse(config?.CreationDate ?? status?.StartedDate, out var createdAt);

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

}
