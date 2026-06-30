using System.Text.Json;
using Float.Core.Models;
using Float.Infrastructure.Engines.AppleContainers.Json.Context;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Volume;

namespace Float.Infrastructure.Engines.AppleContainers;

internal static class AppleVolumeJsonParser
{
    public static ContainerVolumeInfo[] ParseVolumes(string output)
    {
        try
        {
            var items = JsonSerializer.Deserialize(output, AppleVolumeJsonContext.Default.AppleVolumeArray)
                ?? throw new InvalidOperationException("Apple container volume list returned null JSON.");

            return items.Select(ToVolumeInfo).ToArray();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Apple container volume list returned invalid JSON.", ex);
        }
    }

    private static ContainerVolumeInfo ToVolumeInfo(AppleVolume item)
    {
        var configuration = item.Configuration
            ?? throw new InvalidOperationException("Apple container volume list returned a volume without configuration.");
        var sizeInBytes = RequireSizeInBytes(configuration.SizeInBytes, configuration.Name);

        return new ContainerVolumeInfo
        {
            Name = RequireName(configuration.Name),
            Driver = RequireValue(configuration.Driver, configuration.Name, "driver"),
            Format = RequireValue(configuration.Format, configuration.Name, "format"),
            Size = configuration.Size ?? FormatSize(sizeInBytes),
            SizeInBytes = sizeInBytes,
            Source = RequireValue(configuration.Source, configuration.Name, "source")
        };
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }

    private static string RequireName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Apple container volume list returned a volume without name.");

        return name;
    }

    private static string RequireValue(string? value, string? volumeName, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"Apple container volume '{volumeName ?? "<unknown>"}' is missing required field {propertyName}.");

        return value;
    }

    private static long RequireSizeInBytes(long? value, string? volumeName)
    {
        if (value is null)
            throw new InvalidOperationException($"Apple container volume '{volumeName ?? "<unknown>"}' is missing required field sizeInBytes.");

        return value.Value;
    }
}
