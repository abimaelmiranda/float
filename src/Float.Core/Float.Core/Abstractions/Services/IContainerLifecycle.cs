using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface IContainerLifecycle : IContainerEngineAware
{
    Task StartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);

    Task StopAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);

    Task RestartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);

    Task DeleteAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);
}
