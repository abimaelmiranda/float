using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Dtos.GitHub;

internal sealed class GitHubRelease
{
    [JsonPropertyName("assets")]
    public GitHubReleaseAsset[]? Assets { get; init; }
}
