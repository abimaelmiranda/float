using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface IContainerCreator : IContainerEngineAware
{
    Task<string> CreateAsync(
        ContainerCreateRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
