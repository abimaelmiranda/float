using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

namespace Float.Infrastructure.Engines.AppleContainers;

public class AppleContainersEngineProvisioner : IEngineProvisioner
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/apple/container/releases/latest";
    private const string UserAgent = "Float-App";
    private const string InstallerFileName = "apple-containers.pkg";
    private const string BinaryPath = "/usr/local/bin/container";
    private const string UninstallScriptPath = "/usr/local/bin/uninstall-container.sh";

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public ContainerEngine Engine => ContainerEngine.AppleContainers;

    private readonly IFileSystemService _fileSystemService;
    private readonly IProcessHost _processHost;

    public AppleContainersEngineProvisioner(IFileSystemService fileSystemService, IProcessHost processHost)
    {
        _fileSystemService = fileSystemService;
        _processHost = processHost;
    }

    public Task<bool> IsEngineInstalled()
    {
        if (!OperatingSystem.IsMacOS())
            return Task.FromResult(false);

        return Task.FromResult(File.Exists(BinaryPath));
    }

    public async Task<Result> InstallEngineAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsMacOS())
            return Result.WithFailure(DomainErrors.PlatformNotSupported("AppleContainers engine is only supported on macOS."));

        var tempDir = _fileSystemService.GetFloatTempDir();
        var installerPath = Path.Combine(tempDir, InstallerFileName);

        progress?.Report("Fetching latest release information...");
        var downloadUrlResult = await GetDownloadUrlAsync().ConfigureAwait(false);
        if (downloadUrlResult.IsFailure)
            return Result.WithFailure(downloadUrlResult.Failure);

        var downloadUrl = downloadUrlResult.GetValueOrThrow();

        progress?.Report($"Downloading {Path.GetFileName(downloadUrl)}...");
        var downloadResult = await DownloadFileAsync(downloadUrl, installerPath, cancellationToken).ConfigureAwait(false);
        if (downloadResult.IsFailure)
            return Result.WithFailure(downloadResult.Failure);

        progress?.Report("Requesting administrator privileges…");
        try
        {
            var escapedPath = installerPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var script = $"do shell script \"installer -pkg \\\"{escapedPath}\\\" -target /\" with administrator privileges";

            var errors = new StringBuilder();
            var result = await _processHost.RunWithResultAsync(
                "osascript",
                ["-e", script],
                workingDirectory: null,
                onOutput: line => progress?.Report(line),
                onError:  line =>
                {
                    errors.AppendLine(line);
                    progress?.Report(line);
                },
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (result.IsFailure)
                return Result.WithFailure(DomainErrors.CommandFailed(
                    $"Apple container engine command 'installer' failed. {result.Failure.Message}"));
        }
        finally
        {
            if (File.Exists(installerPath))
                File.Delete(installerPath);
        }

        progress?.Report("Installation complete.");
        return Result.WithSuccess();
    }

    public async Task<Result> UninstallEngineAsync()
    {
        if (!OperatingSystem.IsMacOS())
            return Result.WithFailure(DomainErrors.PlatformNotSupported("AppleContainers engine is only supported on macOS."));

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            UninstallScriptPath,
            ["-d"],
            workingDirectory: null,
            onOutput: _ => { },
            onError:  line => errors.AppendLine(line),
            cancellationToken: default).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Apple container engine command 'uninstall' failed. {result.Failure.Message}"));

        return Result.WithSuccess();
    }

    private static async Task<Result<string>> GetDownloadUrlAsync()
    {
        using var response = await HttpClient.GetAsync(LatestReleaseUrl).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return Result.WithFailure<string>(
                DomainErrors.CommandFailed($"Failed to fetch latest release: {(int)response.StatusCode} {response.ReasonPhrase}."));

        using var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var jsonDoc = await JsonDocument.ParseAsync(contentStream).ConfigureAwait(false);

        var assets = jsonDoc.RootElement.GetProperty("assets").EnumerateArray()
            .Select(asset => new
            {
                Name = asset.GetProperty("name").GetString(),
                Url  = asset.GetProperty("browser_download_url").GetString(),
            })
            .Where(a => !string.IsNullOrWhiteSpace(a.Name) && !string.IsNullOrWhiteSpace(a.Url))
            .ToArray();

        if (assets.Length == 0)
            return Result.WithFailure<string>(DomainErrors.NotFound("No release assets found."));

        var pkg = assets.FirstOrDefault(a => a.Name!.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase))
                  ?? assets[0];

        if (pkg.Url is null)
            return Result.WithFailure<string>(DomainErrors.NotFound("Download URL not found."));

        return Result.WithSuccess(pkg.Url);
    }

    private static async Task<Result> DownloadFileAsync(string downloadUrl, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Failed to download file: {(int)response.StatusCode} {response.ReasonPhrase}."));

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination   = File.Create(destinationPath);
        await contentStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);

        return Result.WithSuccess();
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    public async Task<bool> IsEngineRunningAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsEngineInstalled()) return false;

        var result = await _processHost.RunWithResultAsync(
            BinaryPath,
            ["system", "status"],
            workingDirectory: null,
            onOutput: _ => { },
            onError:  _ => { },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.IsSuccess;
    }

    public async Task<Result> StartEngineAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsEngineInstalled())
            return Result.WithFailure(DomainErrors.EngineNotAvailable("Apple Container engine is not installed."));

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            BinaryPath,
            ["system", "start", "--enable-kernel-install"],
            workingDirectory: null,
            onOutput: _ => { },
            onError:  line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Apple container engine command 'system start' failed. {result.Failure.Message}"));

        return Result.WithSuccess();
    }

    public async Task<Result> StopEngineAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsEngineInstalled())
            return Result.WithFailure(DomainErrors.EngineNotAvailable("Apple Container engine is not installed."));

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            BinaryPath,
            ["system", "stop"],
            workingDirectory: null,
            onOutput: _ => { },
            onError:  line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
            return Result.WithFailure(DomainErrors.CommandFailed(
                $"Apple container engine command 'system stop' failed. {result.Failure.Message}"));

        return Result.WithSuccess();
    }
}
