using System.Text.Json.Serialization;

namespace Float.Infrastructure.Engines.AppleContainers;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppleManagedContainer[]))]
[JsonSerializable(typeof(AppleManagedContainer))]
internal sealed partial class AppleContainerJsonContext : JsonSerializerContext
{
}
