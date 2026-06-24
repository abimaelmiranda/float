using System.Threading;
using System.Threading.Tasks;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;

namespace Float.Infrastructure.Engines.AppleContainers;

public sealed class AppleContainerLifecycle : IContainerLifecycle
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IProcessHost _processHost;

    public AppleContainerLifecycle(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public Task StartAsync(Container container, CancellationToken cancellationToken = default)
        => RunAsync("start", container, cancellationToken);

    public Task StopAsync(Container container, CancellationToken cancellationToken = default)
        => RunAsync("stop", container, cancellationToken);

    public Task RestartAsync(Container container, CancellationToken cancellationToken = default)
        => RunAsync("restart", container, cancellationToken);

    public Task DeleteAsync(Container container, CancellationToken cancellationToken = default)
        => container.Instance?.Status == ContainerStatus.Running
            ? RunDeleteAsync(container, cancellationToken)
            : RunAsync("rm", container, cancellationToken);

    private Task RunAsync(string command, Container container, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(container.Name))
        {
            throw new InvalidOperationException("Container name is required.");
        }

        return _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            [command, container.Name],
            workingDirectory: null,
            onOutput: _ => { },
            onError: _ => { },
            cancellationToken: cancellationToken);
    }

    private async Task RunDeleteAsync(Container container, CancellationToken cancellationToken)
    {
        await RunAsync("stop", container, cancellationToken).ConfigureAwait(false);
        await RunAsync("rm", container, cancellationToken).ConfigureAwait(false);
    }
}
