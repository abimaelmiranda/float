using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.Docker;

public sealed class DockerContainerLifecycle : IContainerLifecycle
{
    public ContainerEngine Engine => ContainerEngine.Docker;

    private readonly IProcessHost _processHost;

    public DockerContainerLifecycle(IProcessHost processHost)
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
        => RunAsync("restart", container, cancellationToken, progress);

    public Task<Result> DeleteAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync("rm", container, cancellationToken, progress);

    private async Task<Result> RunAsync(
        string command,
        Container container,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        if (string.IsNullOrWhiteSpace(container.Name))
            return Result.WithFailure(DomainErrors.Validation("Container name is required."));

        progress?.Report($"$ docker {command} {QuoteArgument(container.Name)}");
        var result = await _processHost.RunWithResultAsync(
            "docker",
            [command, container.Name],
            workingDirectory: null,
            onOutput: line => progress?.Report(line),
            onError: line => progress?.Report(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Docker command '{command}' failed for '{container.Name}'. {result.Failure.Message}"));

        return Result.WithSuccess();
    }

    private static string QuoteArgument(string value)
        => value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
