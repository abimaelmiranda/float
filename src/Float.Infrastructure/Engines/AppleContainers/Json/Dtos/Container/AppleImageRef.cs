using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

internal sealed class AppleImageRef
{
    [JsonPropertyName("reference")]
    public string? Reference { get; init; }
}
