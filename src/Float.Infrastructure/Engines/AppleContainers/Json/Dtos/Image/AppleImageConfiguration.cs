using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Image;

internal sealed class AppleImageConfiguration
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
