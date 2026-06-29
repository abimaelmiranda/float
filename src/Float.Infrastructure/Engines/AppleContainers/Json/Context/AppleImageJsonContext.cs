using System.Text.Json.Serialization;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Image;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppleContainerImage[]))]
[JsonSerializable(typeof(AppleContainerImage))]
internal sealed partial class AppleImageJsonContext : JsonSerializerContext
{
}
