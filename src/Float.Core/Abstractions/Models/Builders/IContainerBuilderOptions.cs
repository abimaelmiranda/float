using Float.Core.Models;

namespace Float.Core.Abstractions.Models.Builders;

public interface IContainerBuilderOptions : IBuilder<Container>
{
    IContainerBuilderOptions WithName(string name);
    IContainerBuilderOptions WithEnvironmentVariables(IEnumerable<ContainerEnvironment> environmentVariables);
    IContainerBuilderOptions WithVolumes(IEnumerable<ContainerVolume> volumes);
}
