namespace Float.Core.Abstractions.Services;

public interface IContainerLogReader
{
    Task ReadLogsAsync(string containerName, Action<string> onLine, CancellationToken cancellationToken);
}
