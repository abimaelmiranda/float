using System.Text.Json;
using Float.Infrastructure.Engines.Docker.Json.Context;
using Float.Infrastructure.Engines.Docker.Json.Dtos.Container;
using Float.Infrastructure.Engines.Docker.Json.Dtos.Image;

namespace Float.Infrastructure.Engines.Docker;

internal static class DockerJsonParser
{
    public static DockerInspectContainer[] ParseContainers(string json)
        => JsonSerializer.Deserialize(json, DockerContainerJsonContext.Default.DockerInspectContainerArray)
            ?? throw new InvalidOperationException("Docker command 'inspect' returned null JSON.");

    public static DockerInspectImage[] ParseImages(string json)
        => JsonSerializer.Deserialize(json, DockerContainerJsonContext.Default.DockerInspectImageArray)
            ?? throw new InvalidOperationException("Docker command 'image inspect' returned null JSON.");
}
