using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Volume;

internal sealed class AppleVolume
{
    [JsonPropertyName("configuration")]
    public AppleVolumeConfiguration? Configuration { get; init; }
}
