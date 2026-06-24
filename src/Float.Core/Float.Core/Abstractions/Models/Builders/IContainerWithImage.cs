using Float.Core.Models;

namespace Float.Core.Abstractions.Models.Builders;

public interface IContainerWithImage
{
    IContainerWithPorts WithImage(ContainerImage image);
}
