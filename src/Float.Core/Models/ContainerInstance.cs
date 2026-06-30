using Float.Core.Enums;

namespace Float.Core.Models;

public sealed record ContainerInstance
{
    public required ContainerStatus Status { get; init; }

    public ContainerHealth Health { get; init; } = ContainerHealth.Unknown;

    public DateTimeOffset CreatedAt { get; init; }
}
