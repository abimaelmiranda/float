using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

internal sealed class AppleInitProcess
{
    [JsonPropertyName("environment")]
    public string[]? Environment { get; init; }
}
