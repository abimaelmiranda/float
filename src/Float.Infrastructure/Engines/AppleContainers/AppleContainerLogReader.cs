using Float.Core.Abstractions.Services;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerLogReader : IContainerLogReader
{
    private readonly IProcessHost _processHost;

    public AppleContainerLogReader(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public async Task ReadLogsAsync(string containerName, Action<string> onLine, CancellationToken cancellationToken)
    {
        await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            new List<string> { "logs", "--follow", containerName },
            workingDirectory: null,
            onOutput: onLine,
            onError: _ => { },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
