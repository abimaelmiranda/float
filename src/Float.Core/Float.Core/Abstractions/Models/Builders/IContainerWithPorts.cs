using Float.Core.Models;

namespace Float.Core.Abstractions.Models.Builders;

public interface IContainerWithPorts
{
    IContainerBuilderOptions WithPortsMapping(IEnumerable<ContainerPortMapping> ports);
}
