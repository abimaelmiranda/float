namespace Float.Core.Enums;

public enum ContainerArchitecture
{
    Arm64,
    Amd64
}

public static class ContainerArchitectureExtensions
{
    public static bool TryParseContainerArchitecture(
        string? value,
        out ContainerArchitecture architecture)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "arm64":
            case "linux/arm64":
            case "aarch64":
                architecture = ContainerArchitecture.Arm64;
                return true;
            case "amd64":
            case "linux/amd64":
            case "x86_64":
                architecture = ContainerArchitecture.Amd64;
                return true;
            default:
                architecture = default;
                return false;
        }
    }

    public static ContainerArchitecture? ParseContainerArchitectureOrNull(string? value)
        => TryParseContainerArchitecture(value, out var architecture) ? architecture : null;

    public static string ToCliValue(this ContainerArchitecture architecture)
        => architecture switch
        {
            ContainerArchitecture.Arm64 => "arm64",
            ContainerArchitecture.Amd64 => "amd64",
            _ => throw new ArgumentOutOfRangeException(nameof(architecture), architecture, null)
        };

    public static string ToDisplayValue(this ContainerArchitecture architecture)
        => architecture.ToCliValue();
}
