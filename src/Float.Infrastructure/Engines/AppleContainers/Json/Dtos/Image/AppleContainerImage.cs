using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Image;

internal sealed class AppleContainerImage
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("configuration")]
    public AppleImageConfiguration? Configuration { get; init; }

    [JsonPropertyName("variants")]
    public AppleImageVariant[] Variants { get; init; } = [];
}
