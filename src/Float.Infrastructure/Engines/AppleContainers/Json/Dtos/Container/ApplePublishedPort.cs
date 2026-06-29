using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

internal sealed class ApplePublishedPort
{
    [JsonPropertyName("hostPort")]
    public int HostPort { get; init; }

    [JsonPropertyName("containerPort")]
    public int ContainerPort { get; init; }

    [JsonPropertyName("proto")]
    public string? Proto { get; init; }
}
