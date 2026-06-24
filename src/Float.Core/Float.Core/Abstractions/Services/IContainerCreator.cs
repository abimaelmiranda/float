using Float.Core.Models;

namespace Float.Core.Abstractions.Services;

public interface IContainerCreator : IContainerEngineAware
{
    Task CreateAsync(Container container, CancellationToken cancellationToken = default);
}
