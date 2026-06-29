using System.Text.Json.Serialization;
using Float.Core.Models;

namespace Float.Infrastructure.Json.Context;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class AppSettingsJsonContext : JsonSerializerContext { }
