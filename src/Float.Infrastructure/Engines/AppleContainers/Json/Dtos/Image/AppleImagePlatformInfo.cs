using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Image;

internal sealed class AppleImagePlatformInfo
{
    [JsonPropertyName("architecture")]
    public string? Architecture { get; init; }

    [JsonPropertyName("os")]
    public string? Os { get; init; }
}
