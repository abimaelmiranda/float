using System.Text;
using System.Text.Json;
using Float.Core.Abstractions.Services;
using Float.Core.Models;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;
using Float.Infrastructure.Engines.AppleContainers.Json.Context;

namespace Float.Infrastructure.Engines.AppleContainers;

public sealed class AppleRegistryService : IRegistryService
{
    private readonly IProcessHost _processHost;

    public AppleRegistryService(IProcessHost processHost)
    {
        _processHost = processHost;
    }

    public async Task<Result<IReadOnlyList<RegistryInfo>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var output = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            ["registry", "list", "--format", "json"],
            workingDirectory: null,
            onOutput: line => output.AppendLine(line),
            onError: _ => { },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure<IReadOnlyList<RegistryInfo>>(result.Failure);

        try
        {
            var json = output.ToString().Trim();
            if (string.IsNullOrEmpty(json) || json == "null")
                return Result.WithSuccess<IReadOnlyList<RegistryInfo>>([]);

            var items = JsonSerializer.Deserialize(json, AppleRegistryJsonContext.Default.AppleRegistryListItemArray);
            var registries = (items ?? [])
                .Select(i => new RegistryInfo(i.Hostname, i.Username))
                .ToList();

            return Result.WithSuccess<IReadOnlyList<RegistryInfo>>(registries);
        }
        catch (JsonException ex)
        {
            return Result.WithFailure<IReadOnlyList<RegistryInfo>>(
                DomainErrors.CommandFailed($"Failed to parse registry list: {ex.Message}"));
        }
    }

    public async Task<Result> LoginAsync(
        string server,
        string username,
        string password,
        string scheme = "auto",
        CancellationToken cancellationToken = default)
    {
        return await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            ["registry", "login", "--scheme", scheme, "--username", username, "--password-stdin", server],
            workingDirectory: null,
            onOutput: _ => { },
            onError: _ => { },
            stdinInput: password,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task<Result> LogoutAsync(string server, CancellationToken cancellationToken = default)
    {
        return await _processHost.RunWithResultAsync(
            "/usr/local/bin/container",
            ["registry", "logout", server],
            workingDirectory: null,
            onOutput: _ => { },
            onError: _ => { },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
