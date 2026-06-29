using Float.Core.Models;
using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IContainerVolumeLifecycle : IContainerEngineAware
{
    Task<Result> DeleteAsync(
        ContainerVolumeInfo volume,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);
}
