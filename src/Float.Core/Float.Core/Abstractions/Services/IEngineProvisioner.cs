namespace Float.Core.Abstractions.Services;

public interface IEngineProvisioner : IContainerEngineAware
{
    bool IsEngineInstalled();
    Task<bool> IsEngineRunningAsync(CancellationToken cancellationToken = default);
    Task StartEngineAsync(CancellationToken cancellationToken = default);
    Task StopEngineAsync(CancellationToken cancellationToken = default);
    Task InstallEngineAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task UninstallEngineAsync();
}
