using System.Globalization;
using System.Runtime.InteropServices;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerCreator : IContainerCreator
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IProcessHost _processHost;

    public AppleContainerCreator(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public async Task<Result<string>> CreateAsync(
        ContainerCreateRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        Result result;

        if (request.CommandOverride is { } cmd)
        {
            progress?.Report("$ " + cmd);
            result = await _processHost.RunWithResultAsync(
                "/bin/sh",
                ["-c", cmd],
                workingDirectory: null,
                onOutput: line => progress?.Report(line),
                onError: line => progress?.Report(line),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var args = BuildArgs(request);
            progress?.Report("$ /usr/local/bin/container " + FormatArguments(args, redactEnvironment: true));
            result = await _processHost.RunWithResultAsync(
                "/usr/local/bin/container",
                args,
                workingDirectory: null,
                onOutput: line => progress?.Report(line),
                onError: line => progress?.Report(line),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (result.IsFailure)
            return Result.WithFailure<string>(result.Failure);

        return Result.WithSuccess(request.Name ?? string.Empty);
    }

    public string BuildCommandPreview(ContainerCreateRequest request)
    {
        var args = BuildArgs(request);
        return "/usr/local/bin/container " + FormatArguments(args, redactEnvironment: false);
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
        args.Add(request.Architecture.ToCliValue());

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

    private static string FormatArguments(IReadOnlyList<string> args, bool redactEnvironment)
    {
        var formatted = new List<string>(args.Count);
        for (var i = 0; i < args.Count; i++)
        {
            if (redactEnvironment && args[i] == "--env" && i + 1 < args.Count)
            {
                formatted.Add(args[i]);
                formatted.Add(RedactEnvironment(args[++i]));
                continue;
            }

            formatted.Add(QuoteArgument(args[i]));
        }

        return string.Join(" ", formatted);
    }

    private static string RedactEnvironment(string value)
    {
        var separator = value.IndexOf('=');
        var name = separator < 0 ? value : value[..separator];
        return QuoteArgument($"{name}=***");
    }

    private static string QuoteArgument(string value)
        => value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
