using Float.Core.Enums;

namespace Float.Core.Models;

public sealed record ContainerPortMapping(int HostPort, int ContainerPort, NetworkProtocol Protocol = NetworkProtocol.Tcp);
