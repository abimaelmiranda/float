using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

internal sealed class AppleContainerStatus
{
    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("startedDate")]
    public string? StartedDate { get; init; }
}
