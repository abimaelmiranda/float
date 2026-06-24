using Float.Core.Abstractions.Models.Builders;
using Float.Core.Utils;

namespace Float.Core.Models.Builders;

public sealed class ContainerBuilder : IContainerWithImage, IContainerWithPorts, IContainerBuilderOptions
{
    private ContainerImage? _image;
    private string? _name;
    private readonly List<ContainerEnvironment> _environmentVariables = new();
    private readonly List<ContainerVolume> _volumes = new();
    private readonly List<ContainerPortMapping> _ports = new();

    public IContainerWithPorts WithImage(ContainerImage image)
    {
        _image = Guard.NotNull(image, nameof(image));
        return this;
    }

    public IContainerBuilderOptions WithPortsMapping(IEnumerable<ContainerPortMapping> ports)
    {
        _ports.Clear();
        _ports.AddRange(Guard.NotNull(ports, nameof(ports)));
        return this;
    }

    public IContainerBuilderOptions WithName(string name)
    {
        _name = Guard.NotWhiteSpace(name, nameof(name));
        return this;
    }

    public IContainerBuilderOptions WithEnvironmentVariables(IEnumerable<ContainerEnvironment> environmentVariables)
    {
        _environmentVariables.Clear();
        _environmentVariables.AddRange(Guard.NotNull(environmentVariables, nameof(environmentVariables)));
        return this;
    }

    public IContainerBuilderOptions WithVolumes(IEnumerable<ContainerVolume> volumes)
    {
        _volumes.Clear();
        _volumes.AddRange(Guard.NotNull(volumes, nameof(volumes)));
        return this;
    }

    public Container Build()
    {
        var image = Guard.NotNull(_image, nameof(_image));

        return new Container
        {
            Name = _name ?? string.Empty,
            Image = image,
            EnvironmentVariables = _environmentVariables.ToArray(),
            Volumes = _volumes.ToArray(),
            Ports = _ports.ToArray(),
        };
    }

}
