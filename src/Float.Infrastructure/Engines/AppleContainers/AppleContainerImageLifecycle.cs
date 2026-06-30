using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerImageLifecycle : IContainerImageLifecycle
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IProcessHost _processHost;

    public AppleContainerImageLifecycle(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public async Task<Result> DeleteAsync(
        ContainerImage image,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        var target = string.IsNullOrWhiteSpace(image.Tag) ? image.Id : image.Tag;
        if (string.IsNullOrWhiteSpace(target))
            return Result.WithFailure(DomainErrors.Validation("Image id or tag is required."));

        progress?.Report($"/usr/local/bin/container image delete {target}");
        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            ["image", "delete", target],
            workingDirectory: null,
            onOutput: line => progress?.Report(line),
            onError: line => progress?.Report(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Apple container image delete failed for '{target}'. {result.Failure.Message}"));

        return Result.WithSuccess();
    }
}
