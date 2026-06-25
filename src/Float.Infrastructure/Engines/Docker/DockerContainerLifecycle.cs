using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using System.Text;

namespace Float.Infrastructure.Engines.Docker;

public sealed class DockerContainerLifecycle : IContainerLifecycle
{
    public ContainerEngine Engine => ContainerEngine.Docker;

    private readonly IProcessHost _processHost;

    public DockerContainerLifecycle(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public Task StartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync("start", container, cancellationToken, progress);

    public Task StopAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync("stop", container, cancellationToken, progress);

    public Task RestartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync("restart", container, cancellationToken, progress);

    public Task DeleteAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
        => RunAsync("rm", container, cancellationToken, progress);

    private async Task RunAsync(
        string command,
        Container container,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        if (string.IsNullOrWhiteSpace(container.Name))
        {
            // TODO: replace flow-control exception with result pattern.
            throw new InvalidOperationException("Container name is required.");
        }

        var output = new StringBuilder();
        var errors = new StringBuilder();
        progress?.Report($"$ docker {command} {QuoteArgument(container.Name)}");
        var result = await _processHost.RunWithResultAsync(
            "docker",
            [command, container.Name],
            workingDirectory: null,
            onOutput: line =>
            {
                output.AppendLine(line);
                progress?.Report(line);
            },
            onError: line =>
            {
                errors.AppendLine(line);
                progress?.Report(line);
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
            throw new InvalidOperationException(
                BuildFailureMessage("Docker", command, container.Name, result.ExitCode, output, errors));
    }

    private static string BuildFailureMessage(
        string engine,
        string command,
        string containerName,
        int exitCode,
        StringBuilder output,
        StringBuilder errors)
    {
        var message = $"{engine} command '{command}' failed for '{containerName}' with exit code {exitCode}.";
        var detail = errors.Length > 0 ? errors.ToString().Trim() : output.ToString().Trim();
        return string.IsNullOrWhiteSpace(detail)
            ? message
            : message + Environment.NewLine + detail;
    }

    private static string QuoteArgument(string value)
        => value.Any(char.IsWhiteSpace) || value.Contains('"')
            ? "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\""
            : value;
}
