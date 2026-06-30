using Float.Core.Models;
using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IContainerCreator : IContainerEngineAware
{
    Task<Result<string>> CreateAsync(
        ContainerCreateRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);

    string BuildCommandPreview(ContainerCreateRequest request);
}
