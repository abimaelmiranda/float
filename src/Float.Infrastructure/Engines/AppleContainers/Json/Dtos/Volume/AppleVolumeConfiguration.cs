using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Volume;

internal class AppleVolumeConfiguration
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("driver")]
    public string? Driver { get; init; }

    [JsonPropertyName("format")]
    public string? Format { get; init; }

    [JsonPropertyName("size")]
    public string? Size { get; init; }

    [JsonPropertyName("sizeInBytes")]
    public long? SizeInBytes { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }
}
