using Float.Core.Models;
using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IContainerImageLifecycle : IContainerEngineAware
{
    Task<Result> DeleteAsync(
        ContainerImage image,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);
}
