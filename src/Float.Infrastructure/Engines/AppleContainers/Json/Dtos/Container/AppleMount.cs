using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

internal sealed class AppleMount
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("destination")]
    public string? Destination { get; init; }

    [JsonPropertyName("readOnly")]
    public bool ReadOnly { get; init; }
}
