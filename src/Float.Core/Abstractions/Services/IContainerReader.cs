using Float.Core.Models;
using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IContainerReader : IContainerEngineAware
{
    Task<Result<IReadOnlyList<Container>>> ListContainersAsync(bool includeAll = true,
        CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ContainerImage>>> ListImagesAsync(bool includeAll = true, CancellationToken cancellationToken = default);
}
