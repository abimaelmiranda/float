using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface IContainerLifecycle : IContainerEngineAware
{
    Task StartAsync(Container container, CancellationToken cancellationToken = default);
    Task StopAsync(Container container, CancellationToken cancellationToken = default);
    Task RestartAsync(Container container, CancellationToken cancellationToken = default);
    Task DeleteAsync(Container container, CancellationToken cancellationToken = default);
}
