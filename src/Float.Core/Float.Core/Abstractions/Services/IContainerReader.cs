using Float.Core.Models;
using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IContainerReader : IContainerEngineAware
{
    Task<Result<IReadOnlyList<Container>>> ListAsync(bool includeAll = true, CancellationToken cancellationToken = default);
}
