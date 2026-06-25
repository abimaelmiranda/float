using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.Docker;

public class DockerContainerEngineProvisioner : IEngineProvisioner
{
    public ContainerEngine Engine => ContainerEngine.Docker;
    private readonly IProcessHost _processHost;

    public DockerContainerEngineProvisioner(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public Task<Result> InstallEngineAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.WithFailure(DomainErrors.NotSupported("Engine manipulation is not supported for Docker")));

    public async Task<bool> IsEngineInstalled()
    {
        var result = await _processHost.RunWithResultAsync("docker",
            "-v", workingDirectory:
            null, onOutput: _ => { }, onError: _ => { });

        return result.IsSuccess;
    }

    public async Task<bool> IsEngineRunningAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processHost.RunWithResultAsync("docker",
            "info", workingDirectory:
            null, onOutput: _ => { }, onError: _ => { });

        return result.IsSuccess;
    }

    public Task<Result> StartEngineAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.WithFailure(DomainErrors.NotSupported("Engine manipulation is not supported for Docker")));

    public Task<Result> StopEngineAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.WithFailure(DomainErrors.NotSupported("Engine manipulation is not supported for Docker")));

    public Task<Result> UninstallEngineAsync()
        => Task.FromResult(Result.WithFailure(DomainErrors.NotSupported("Engine manipulation is not supported for Docker")));
}
