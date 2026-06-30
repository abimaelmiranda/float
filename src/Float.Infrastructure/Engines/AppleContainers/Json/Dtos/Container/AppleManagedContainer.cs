using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

internal sealed class AppleManagedContainer
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("configuration")]
    public AppleContainerConfiguration? Configuration { get; init; }

    [JsonPropertyName("status")]
    public AppleContainerStatus? Status { get; init; }
}
