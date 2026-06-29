using System.Text.Json.Serialization;
using Float.Infrastructure.Engines.Docker.Json.Dtos.Container;
using Float.Infrastructure.Engines.Docker.Json.Dtos.Image;

namespace Float.Infrastructure.Engines.Docker.Json.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(DockerInspectContainer[]))]
[JsonSerializable(typeof(DockerInspectImage[]))]
public partial class DockerContainerJsonContext : JsonSerializerContext
{
}
