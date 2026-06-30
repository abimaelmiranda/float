using System.Globalization;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainerVolumeCreator : IContainerVolumeCreator
{
    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IProcessHost _processHost;

    public AppleContainerVolumeCreator(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public async Task<Result<string>> CreateAsync(
        ContainerVolumeCreateRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var trimmedName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
            return Result.WithFailure<string>(DomainErrors.Validation("Volume name is required."));

        var args = new List<string> { "volume", "create", trimmedName };
        if (request.SizeValue is not null)
        {
            if (request.SizeValue <= 0)
                return Result.WithFailure<string>(DomainErrors.Validation("Volume size must be greater than zero."));

            args.AddRange(["-s", FormatSize(request.SizeValue.Value, request.SizeUnit)]);
        }

        if (request.JournalMode != ContainerVolumeJournalMode.Default)
        {
            var journal = request.JournalMode switch
            {
                ContainerVolumeJournalMode.Ordered => "ordered",
                ContainerVolumeJournalMode.Writeback => "writeback",
                ContainerVolumeJournalMode.Journal => "journal",
                _ => "ordered",
            };

            if (request.JournalSizeValue is not null)
            {
                if (request.JournalSizeValue <= 0)
                    return Result.WithFailure<string>(DomainErrors.Validation("Journal size must be greater than zero."));

                journal += ":" + FormatSize(request.JournalSizeValue.Value, request.JournalSizeUnit);
            }

            args.AddRange(["--opt", "journal=" + journal]);
        }

        progress?.Report("$ /usr/local/bin/container " + string.Join(" ", args));

        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            args,
            workingDirectory: null,
            onOutput: line => progress?.Report(line),
            onError: line => progress?.Report(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<string>(result.Failure);

        return Result.WithSuccess(trimmedName);
    }

    private static string FormatSize(decimal value, ContainerVolumeSizeUnit unit)
        => value.ToString("0.########", CultureInfo.InvariantCulture) + unit switch
        {
            ContainerVolumeSizeUnit.B => "",
            ContainerVolumeSizeUnit.KB => "K",
            ContainerVolumeSizeUnit.MB => "M",
            ContainerVolumeSizeUnit.GB => "G",
            ContainerVolumeSizeUnit.TB => "T",
            _ => "",
        };
}
