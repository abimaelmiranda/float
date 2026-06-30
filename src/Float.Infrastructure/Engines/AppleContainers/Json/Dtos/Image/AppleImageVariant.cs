using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Image;

internal sealed class AppleImageVariant
{
    [JsonPropertyName("Platform")]
    public AppleImagePlatformInfo? Platform { get; init; }
}
