using System.Globalization;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.AppleContainers;

public sealed class AppleContainerLifecycle : IContainerLifecycle
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IProcessHost _processHost;
    private readonly ISettingsService _settingsService;

    public AppleContainerLifecycle(IProcessHost processHost, ISettingsService settingsService)
    {
        _processHost = processHost;
        _settingsService = settingsService;
    }

    public Task<Result> StartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync(["start"], container, cancellationToken, progress);

    public Task<Result> StopAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => StopCoreAsync(container, cancellationToken, progress);

    public Task<Result> RestartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RestartCoreAsync(container, cancellationToken, progress);

    private Task<Result> StopCoreAsync(
        Container container,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        var timeout = _settingsService.Get().StopTimeoutSeconds;
        return RunAsync(
            ["stop", "--time", timeout.ToString(CultureInfo.InvariantCulture)],
            container, cancellationToken, progress);
    }

    public async Task<Result> DeleteAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        var forceDelete = _settingsService.Get().ForceDeleteRunning;

        if (forceDelete)
            return await RunAsync(["rm", "--force"], container, cancellationToken, progress).ConfigureAwait(false);

        if (container.Instance?.Status == ContainerStatus.Running)
        {
            var stopResult = await StopCoreAsync(container, cancellationToken, progress).ConfigureAwait(false);
            if (stopResult.IsFailure)
                return stopResult;
        }

        return await RunAsync(["rm"], container, cancellationToken, progress).ConfigureAwait(false);
    }

    private async Task<Result> RestartCoreAsync(
        Container container,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        var stopResult = await StopCoreAsync(container, cancellationToken, progress).ConfigureAwait(false);
        return stopResult.IsFailure
            ? stopResult
            : await RunAsync(["start"], container, cancellationToken, progress).ConfigureAwait(false);
    }

    // commandParts: the command verb + any flags, e.g. ["stop", "--time", "5"] or ["rm", "--force"]
    private async Task<Result> RunAsync(
        string[] commandParts,
        Container container,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        if (string.IsNullOrWhiteSpace(container.Name))
            return Result.WithFailure(DomainErrors.Validation("Container name is required."));

        var args = new List<string>(commandParts) { container.Name };
        progress?.Report($"$ /usr/local/bin/container {string.Join(" ", commandParts)} {QuoteArgument(container.Name)}");
        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            args,
            workingDirectory: null,
            onOutput: line => progress?.Report(line),
            onError: line => progress?.Report(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Apple container command '{commandParts[0]}' failed for '{container.Name}'. {result.Failure.Message}"));

        return Result.WithSuccess();
    }

    private static string QuoteArgument(string value)
        => value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
