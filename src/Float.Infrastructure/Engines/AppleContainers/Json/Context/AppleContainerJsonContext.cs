using System.Text.Json.Serialization;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppleManagedContainer[]))]
[JsonSerializable(typeof(AppleManagedContainer))]
internal sealed partial class AppleContainerJsonContext : JsonSerializerContext
{
}
