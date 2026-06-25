using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.Docker.Json;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(DockerInspectContainer[]))]
[JsonSerializable(typeof(DockerInspectImage[]))]
public partial class DockerContainerJsonContext : JsonSerializerContext
{

}
