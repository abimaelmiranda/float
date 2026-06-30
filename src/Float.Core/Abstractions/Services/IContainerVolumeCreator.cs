using Float.Core.Models.Results;
using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface IContainerVolumeCreator : IContainerEngineAware
{
    Task<Result<string>> CreateAsync(
        ContainerVolumeCreateRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
