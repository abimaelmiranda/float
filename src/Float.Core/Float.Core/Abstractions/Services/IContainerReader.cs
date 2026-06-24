using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface IContainerReader : IContainerEngineAware
{
    Task<IReadOnlyList<Container>> ListAsync(CancellationToken cancellationToken = default);
}
