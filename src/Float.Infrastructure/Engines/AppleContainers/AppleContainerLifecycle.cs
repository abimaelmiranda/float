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

    public AppleContainerLifecycle(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public Task<Result> StartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync("start", container, cancellationToken, progress);

    public Task<Result> StopAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync("stop", container, cancellationToken, progress);

    public Task<Result> RestartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RestartCoreAsync(container, cancellationToken, progress);

    public async Task<Result> DeleteAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        if (container.Instance?.Status == ContainerStatus.Running)
        {
            var stopResult = await RunAsync("stop", container, cancellationToken, progress).ConfigureAwait(false);
            if (stopResult.IsFailure)
                return stopResult;
        }

        return await RunAsync("rm", container, cancellationToken, progress).ConfigureAwait(false);
    }

    private async Task<Result> RestartCoreAsync(
        Container container,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        var stopResult = await RunAsync("stop", container, cancellationToken, progress).ConfigureAwait(false);
        return stopResult.IsFailure
            ? stopResult
            : await RunAsync("start", container, cancellationToken, progress).ConfigureAwait(false);
    }

    private async Task<Result> RunAsync(
        string command,
        Container container,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        if (string.IsNullOrWhiteSpace(container.Name))
            return Result.WithFailure(DomainErrors.Validation("Container name is required."));

        progress?.Report($"$ /usr/local/bin/container {command} {QuoteArgument(container.Name)}");
        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            [command, container.Name],
            workingDirectory: null,
            onOutput: line => progress?.Report(line),
            onError: line => progress?.Report(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Apple container command '{command}' failed for '{container.Name}'. {result.Failure.Message}"));

        return Result.WithSuccess();
    }

    private static string QuoteArgument(string value)
        => value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
