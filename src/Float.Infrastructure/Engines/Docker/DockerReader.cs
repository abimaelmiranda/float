using System.Text;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;
using Float.Infrastructure.Engines.Docker.Json.Dtos.Container;
using Microsoft.Extensions.DependencyInjection;

namespace Float.Infrastructure.Engines.Docker;

public class DockerReader : IContainerReader
{
    public ContainerEngine Engine => ContainerEngine.Docker;

    private readonly IProcessHost _processHost;
    private readonly IEngineProvisioner _engineProvisioner;

    public DockerReader(
        IProcessHost processHost,
        [FromKeyedServices(ContainerEngine.Docker)] IEngineProvisioner engineProvisioner)
    {
        _processHost = processHost;
        _engineProvisioner = engineProvisioner;
    }

    public async Task<Result<IReadOnlyList<Container>>> ListContainersAsync(
        bool includeAll = true,
        CancellationToken cancellationToken = default)
    {
        if (!await _engineProvisioner.IsEngineInstalled())
            return Result.WithFailure<IReadOnlyList<Container>>(
                DomainErrors.EngineNotAvailable("Docker is not available."));

        var idsResult = await ListContainerIdsAsync(includeAll, cancellationToken).ConfigureAwait(false);
        if (idsResult.IsFailure)
            return Result.WithFailure<IReadOnlyList<Container>>(idsResult.Failure);

        var ids = idsResult.GetValueOrThrow();
        if (ids.Length == 0)
            return Result.WithSuccess<IReadOnlyList<Container>>([]);

        var inspectedResult = await InspectAsync(ids, cancellationToken).ConfigureAwait(false);
        if (inspectedResult.IsFailure)
            return Result.WithFailure<IReadOnlyList<Container>>(inspectedResult.Failure);

        var inspected = inspectedResult.GetValueOrThrow();
        var imageArchitectures = await ResolveMissingImageArchitecturesAsync(inspected, cancellationToken)
            .ConfigureAwait(false);

        var containers = inspected
            .Select(container => DockerContainerMapper.ToContainer(container, imageArchitectures))
            .ToArray();
        return Result.WithSuccess<IReadOnlyList<Container>>(containers);
    }

    private async Task<Result<string[]>> ListContainerIdsAsync(bool includeAll, CancellationToken cancellationToken)
    {
        var args = new List<string> { "ps", "--quiet" };
        if (includeAll)
            args.Add("--all");

        var output = new StringBuilder();
        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "docker",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<string[]>(result.Failure);

        var ids = output
            .ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return Result.WithSuccess(ids);
    }

    private async Task<Result<DockerInspectContainer[]>> InspectAsync(
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var args = new List<string> { "inspect", };
        args.AddRange(ids);

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "docker",
            args,
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<DockerInspectContainer[]>(result.Failure);

        if (output.Length == 0)
            return Result.WithFailure<DockerInspectContainer[]>(
                DomainErrors.CommandFailed("Docker command 'inspect' returned no output."));

        try
        {
            return Result.WithSuccess(DockerJsonParser.ParseContainers(output.ToString()));
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
        {
            return Result.WithFailure<DockerInspectContainer[]>(
                DomainErrors.ParseError($"Docker command 'inspect' returned invalid JSON. {ex.Message}"));
        }
    }

    private async Task<Dictionary<string, ContainerArchitecture>> ResolveMissingImageArchitecturesAsync(
        IReadOnlyList<DockerInspectContainer> containers,
        CancellationToken cancellationToken)
    {
        var refs = containers
            .Where(container => ContainerArchitectureExtensions.ParseContainerArchitectureOrNull(DockerContainerMapper.GetRawArchitecture(container)) is null)
            .SelectMany(container => new[] { container.Config?.Image, container.Image })
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .Select(image => image!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (refs.Length == 0)
            return [];

        var architectures = new Dictionary<string, ContainerArchitecture>(StringComparer.Ordinal);
        foreach (var imageRef in refs)
        {
            var result = await InspectImageArchitectureAsync(imageRef, cancellationToken)
                .ConfigureAwait(false);
            if (result.IsSuccess && result.GetValueOrThrow() is { } architecture)
                architectures[imageRef] = architecture;
        }

        return architectures;
    }

    private async Task<Result<ContainerArchitecture?>> InspectImageArchitectureAsync(
        string imageRef,
        CancellationToken cancellationToken)
    {
        var output = new StringBuilder();
        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "docker",
            ["image", "inspect", imageRef],
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<ContainerArchitecture?>(result.Failure);

        if (output.Length == 0)
            return Result.WithFailure<ContainerArchitecture?>(
                DomainErrors.CommandFailed($"Docker command 'image inspect' returned no output for image '{imageRef}'."));

        try
        {
            var images = DockerJsonParser.ParseImages(output.ToString());
            if (images.Length == 0)
                return Result.WithFailure<ContainerArchitecture?>(
                    DomainErrors.ParseError($"Docker command 'image inspect' returned no image entries for '{imageRef}'."));

            return Result.WithSuccess(ContainerArchitectureExtensions.ParseContainerArchitectureOrNull(images[0].Architecture));
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or InvalidOperationException)
        {
            return Result.WithFailure<ContainerArchitecture?>(
                DomainErrors.ParseError($"Docker command 'image inspect' returned invalid JSON for image '{imageRef}'. {ex.Message}"));
        }
    }

    public Task<Result<IReadOnlyList<ContainerImage>>> ListImagesAsync(bool includeAll = true, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Image listing is not yet supported for docker");
    }
}
