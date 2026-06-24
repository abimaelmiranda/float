using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers;

internal sealed class AppleManagedContainer
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("configuration")]
    public AppleContainerConfiguration? Configuration { get; init; }

    [JsonPropertyName("status")]
    public AppleContainerStatus? Status { get; init; }
}

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

internal sealed class AppleImageRef
{
    [JsonPropertyName("reference")]
    public string? Reference { get; init; }
}

internal sealed class ApplePublishedPort
{
    [JsonPropertyName("hostPort")]
    public int HostPort { get; init; }

    [JsonPropertyName("containerPort")]
    public int ContainerPort { get; init; }

    [JsonPropertyName("proto")]
    public string? Proto { get; init; }
}

internal sealed class AppleMount
{
    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("destination")]
    public string? Destination { get; init; }

    [JsonPropertyName("readOnly")]
    public bool ReadOnly { get; init; }
}

internal sealed class AppleInitProcess
{
    // Each entry is "KEY=VALUE"
    [JsonPropertyName("environment")]
    public string[]? Environment { get; init; }
}

internal sealed class AppleContainerStatus
{
    // "running" | "stopped" | "stopping" | "unknown"
    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("startedDate")]
    public string? StartedDate { get; init; }
}
