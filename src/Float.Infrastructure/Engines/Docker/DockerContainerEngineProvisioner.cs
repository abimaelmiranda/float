using System;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;

namespace Float.Infrastructure.Engines.Docker;

public class DockerContainerEngineProvisioner : IEngineProvisioner
{
    public ContainerEngine Engine => ContainerEngine.Docker;
    private readonly IProcessHost _processHost;

    public DockerContainerEngineProvisioner(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public Task InstallEngineAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        // TODO: replace flow-control exception with result pattern.
        throw new NotSupportedException("Engine manipulation is not supported for Docker");
    }

    public async Task<bool> IsEngineInstalled()
    {
        var result = await _processHost.RunWithResultAsync("docker", 
            "-v", workingDirectory: 
            null, onOutput: _ => { }, onError: _ => { });

        return result.ExitCode == 0;
    }

    public async Task<bool> IsEngineRunningAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processHost.RunWithResultAsync("docker", 
            "info", workingDirectory: 
            null, onOutput: _ => { }, onError: _ => { });

        return result.ExitCode == 0; 
    }

    public Task StartEngineAsync(CancellationToken cancellationToken = default)
    {
        // TODO: replace flow-control exception with result pattern.
        throw new NotSupportedException("Engine manipulation is not supported for Docker");
    }

    public Task StopEngineAsync(CancellationToken cancellationToken = default)
    {
        // TODO: replace flow-control exception with result pattern.
        throw new NotSupportedException("Engine manipulation is not supported for Docker");
    }

    public Task UninstallEngineAsync()
    {
        // TODO: replace flow-control exception with result pattern.
        throw new NotSupportedException("Engine manipulation is not supported for Docker");
    }
}
