using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos;

internal sealed record AppleRegistryListItem
{
    [JsonPropertyName("hostname")] public string Hostname { get; init; } = "";
    [JsonPropertyName("username")] public string? Username { get; init; }
}
