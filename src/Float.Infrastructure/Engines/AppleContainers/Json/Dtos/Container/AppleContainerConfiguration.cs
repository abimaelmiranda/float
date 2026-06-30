using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

internal sealed class AppleContainerConfiguration
{
    [JsonPropertyName("image")]
    public AppleImageRef? Image { get; init; }

    [JsonPropertyName("publishedPorts")]
    public ApplePublishedPort[]? PublishedPorts { get; init; }

    [JsonPropertyName("mounts")]
    public AppleMount[]? Mounts { get; init; }

    [JsonPropertyName("initProcess")]
    public AppleInitProcess? InitProcess { get; init; }

    [JsonPropertyName("creationDate")]
    public string? CreationDate { get; init; }
}
