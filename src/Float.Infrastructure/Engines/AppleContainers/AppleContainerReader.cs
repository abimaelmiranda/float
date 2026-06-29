using System.Text;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerReader : IContainerReader
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IEngineProvisioner _engineProvisioner;
    private readonly IProcessHost _processHost;

    public AppleContainerReader(
        [FromKeyedServices(ContainerEngine.AppleContainers)] IEngineProvisioner engineProvisioner,
        IProcessHost processHost)
    {
        _engineProvisioner = engineProvisioner;
        _processHost = processHost;
    }

    public async Task<Result<IReadOnlyList<Container>>> ListContainersAsync(bool includeAll = true, CancellationToken cancellationToken = default)
    {
        if (!await _engineProvisioner.IsEngineInstalled())
            return Result.WithFailure<IReadOnlyList<Container>>(
                DomainErrors.EngineNotAvailable("Apple Container is not available."));

        var output = new StringBuilder();
        var args = new List<string> { "ls", "--format", "json" };

        if (includeAll)
        {
            args.Insert(1, "--all");
        }

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<IReadOnlyList<Container>>(result.Failure);

        var outputStr = output.ToString();
        if (string.IsNullOrWhiteSpace(outputStr))
            return Result.WithSuccess<IReadOnlyList<Container>>([]);

        try
        {
            var containers = AppleContainerJsonParser.ParseContainers(outputStr);
            return Result.WithSuccess<IReadOnlyList<Container>>(containers);
        }
        catch (InvalidOperationException ex)
        {
            return Result.WithFailure<IReadOnlyList<Container>>(
                DomainErrors.ParseError(ex.Message));
        }
    }

    public async Task<Result<IReadOnlyList<ContainerImage>>> ListImagesAsync(bool includeAll = true, CancellationToken cancellationToken = default)
    {
        if (!await _engineProvisioner.IsEngineInstalled())
            return Result.WithFailure<IReadOnlyList<ContainerImage>>(
                DomainErrors.EngineNotAvailable("Apple Container is not available."));

        var output = new StringBuilder();
        var errors = new StringBuilder();
        var args = new List<string> { "image", "ls", "--format", "json" };

        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<IReadOnlyList<ContainerImage>>(result.Failure);

        var outputStr = output.ToString();
        if (string.IsNullOrWhiteSpace(outputStr))
            return Result.WithSuccess<IReadOnlyList<ContainerImage>>([]);

        try
        {
            var images = AppleContainerJsonParser.ParseImages(outputStr);
            return Result.WithSuccess<IReadOnlyList<ContainerImage>>(images);
        }
        catch (InvalidOperationException ex)
        {
            return Result.WithFailure<IReadOnlyList<ContainerImage>>(
                DomainErrors.ParseError(ex.Message));
        }
    }

}
