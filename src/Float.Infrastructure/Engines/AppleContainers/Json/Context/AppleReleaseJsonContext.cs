using System.Text.Json.Serialization;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.GitHub;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GitHubRelease))]
internal sealed partial class AppleReleaseJsonContext : JsonSerializerContext
{
}
