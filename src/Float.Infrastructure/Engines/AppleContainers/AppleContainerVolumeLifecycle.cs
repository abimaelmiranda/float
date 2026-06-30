using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerVolumeLifecycle : IContainerVolumeLifecycle
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IProcessHost _processHost;

    public AppleContainerVolumeLifecycle(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public async Task<Result> DeleteAsync(
        ContainerVolumeInfo volume,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(volume.Name))
            return Result.WithFailure(DomainErrors.Validation("Volume name is required."));

        progress?.Report($"/usr/local/bin/container volume delete {volume.Name}");
        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            ["volume", "delete", volume.Name],
            workingDirectory: null,
            onOutput: line => progress?.Report(line),
            onError: line => progress?.Report(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Apple container volume delete failed for '{volume.Name}'. {result.Failure.Message}"));

        return Result.WithSuccess();
    }
}
