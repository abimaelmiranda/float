using System.Text.Json.Serialization;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos;

namespace Float.Infrastructure.Engines.AppleContainers.Json.Context;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppleRegistryListItem[]))]
internal sealed partial class AppleRegistryJsonContext : JsonSerializerContext { }
