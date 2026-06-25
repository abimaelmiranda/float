using Float.Core.Models;
using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IContainerLifecycle : IContainerEngineAware
{
    Task<Result> StartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);

    Task<Result> StopAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);

    Task<Result> RestartAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);

    Task<Result> DeleteAsync(
        Container container,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);
}
