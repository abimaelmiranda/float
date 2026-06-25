namespace Float.Core.Models;

public sealed record ContainerVolume(
    string HostPath,
    string ContainerPath,
    bool ReadOnly = false,
    bool IsNamedVolume = false,
    string? Name = null);
