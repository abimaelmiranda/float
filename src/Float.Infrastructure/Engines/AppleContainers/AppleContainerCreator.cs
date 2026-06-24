using System.Globalization;
using System.Text;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerCreator : IContainerCreator
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IProcessHost _processHost;

    public AppleContainerCreator(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public async Task<string> CreateAsync(
        ContainerCreateRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var args = BuildArgs(request);
        await _processHost.RunAsync(
            "/usr/local/bin/container",
            args,
            workingDirectory: null,
            onOutput: line =>
            {
                progress?.Report(line);
            },
            onError: line => progress?.Report(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return request.Name ?? string.Empty;
    }

    private static List<string> BuildArgs(ContainerCreateRequest request)
    {
        var args = new List<string>
        {
            request.StartImmediately ? "run" : "create"
        };

        if (request.StartImmediately)
            args.Add("--detach");

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            args.Add("--name");
            args.Add(request.Name);
        }

        args.Add("--arch");
        args.Add(request.Architecture);

        if (request.EnableRosetta)
            args.Add("--rosetta");

        if (request.CpuCount.HasValue)
        {
            args.Add("--cpus");
            args.Add(request.CpuCount.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(request.Memory))
        {
            args.Add("--memory");
            args.Add(request.Memory);
        }

        foreach (var port in request.Ports)
        {
            args.Add("--publish");
            var proto = port.Protocol == NetworkProtocol.Udp ? "/udp" : string.Empty;
            args.Add($"{port.HostPort}:{port.ContainerPort}{proto}");
        }

        foreach (var volume in request.Volumes)
        {
            args.Add("--volume");
            var spec = $"{volume.HostPath}:{volume.ContainerPath}";
            if (volume.ReadOnly) spec += ":ro";
            args.Add(spec);
        }

        foreach (var env in request.EnvironmentVariables)
        {
            args.Add("--env");
            args.Add($"{env.Name}={env.Value}");
        }

        args.Add(request.ImageTag);

        if (request.StartImmediately && request.KeepAlive)
        {
            args.Add("sleep");
            args.Add("infinity");
        }

        return args;
    }
}
