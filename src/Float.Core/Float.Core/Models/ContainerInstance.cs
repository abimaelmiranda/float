using Float.Core.Enums;

namespace Float.Core.Models;

public sealed record ContainerInstance
{
    public ContainerStatus Status { get; init; } = ContainerStatus.Unknown;

    public ContainerHealth Health { get; init; } = ContainerHealth.Unknown;

    public DateTimeOffset CreatedAt { get; init; }
}
