using System.Text.Json;
using Float.Core.Models;
using Float.Infrastructure.Engines.AppleContainers.Json.Context;
using Float.Infrastructure.Engines.AppleContainers.Json.Dtos.Container;

namespace Float.Infrastructure.Engines.AppleContainers;

internal static class AppleContainerJsonParser
{
    public static Container[] ParseContainers(string output)
    {
        try
        {
            var trimmed = output.TrimStart();
            if (trimmed.StartsWith("[", StringComparison.Ordinal))
            {
                var items = JsonSerializer.Deserialize(
                    output,
                    AppleContainerJsonContext.Default.AppleManagedContainerArray);
                return (items ?? throw new InvalidOperationException("Apple container list returned null JSON."))
                    .Select(AppleContainerMapper.ToContainer)
                    .ToArray();
            }

            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseContainerLine)
                .Select(AppleContainerMapper.ToContainer)
                .ToArray();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Apple container list returned invalid JSON.", ex);
        }
    }

    public static ContainerImage[] ParseImages(string rawInput)
    {
        try
        {
            var items = JsonSerializer.Deserialize(rawInput, AppleImageJsonContext.Default.AppleContainerImageArray);
            return (items ?? throw new InvalidOperationException("Apple container image list returned null JSON."))
                .Select(AppleContainerMapper.ToImage)
                .ToArray();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Apple container image list returned invalid JSON.", ex);
        }
    }

    private static AppleManagedContainer ParseContainerLine(string line)
        => JsonSerializer.Deserialize(line, AppleContainerJsonContext.Default.AppleManagedContainer)
            ?? throw new InvalidOperationException("Apple container list returned null JSON item.");
}
