using Float.Core.Enums;

namespace Float.Core.Abstractions.Services;

public interface IContainerEngineAware
{
    ContainerEngine Engine { get; }
}
