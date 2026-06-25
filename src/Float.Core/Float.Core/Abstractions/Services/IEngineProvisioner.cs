using Float.Core.Models.Results;

namespace Float.Core.Abstractions.Services;

public interface IEngineProvisioner : IContainerEngineAware
{
    Task<bool> IsEngineInstalled();
    Task<bool> IsEngineRunningAsync(CancellationToken cancellationToken = default);
    Task<Result> StartEngineAsync(CancellationToken cancellationToken = default);
    Task<Result> StopEngineAsync(CancellationToken cancellationToken = default);
    Task<Result> InstallEngineAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<Result> UninstallEngineAsync();
}
