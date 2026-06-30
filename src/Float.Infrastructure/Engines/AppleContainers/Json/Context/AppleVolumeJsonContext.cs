using System.Text.Json.Serialization;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Volume;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppleVolume[]))]
internal sealed partial class AppleVolumeJsonContext : JsonSerializerContext
{
}
