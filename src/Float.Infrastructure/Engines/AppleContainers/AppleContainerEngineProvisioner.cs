using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Float.Core.Abstractions.Services;
using Float.Core.Enums;

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

    public async Task<bool> IsEngineInstalled()
    {
        EnsureSupportedPlatform();
        return File.Exists(BinaryPath);
    }

    public async Task InstallEngineAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        EnsureSupportedPlatform();

        var tempDir = _fileSystemService.GetFloatTempDir();
        var installerPath = Path.Combine(tempDir, InstallerFileName);

        progress?.Report("Fetching latest release information...");
        var downloadUrl = await GetDownloadUrlAsync().ConfigureAwait(false);

        progress?.Report($"Downloading {Path.GetFileName(downloadUrl)}...");
        await DownloadFileAsync(downloadUrl, installerPath, cancellationToken).ConfigureAwait(false);

        progress?.Report("Requesting administrator privileges…");
        try
        {
            // osascript prompts the native macOS auth dialog and runs installer as root.
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

            if (!result.Succeeded)
                throw EngineCommandFailed("installer", result.ExitCode, errors);
        }
        finally
        {
            if (File.Exists(installerPath))
                File.Delete(installerPath);
        }

        progress?.Report("Installation complete.");
    }

    public async Task UninstallEngineAsync()
    {
        EnsureSupportedPlatform();

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            UninstallScriptPath,
            ["-d"],
            workingDirectory: null,
            onOutput: _ => { },
            onError:  line => errors.AppendLine(line),
            cancellationToken: default).ConfigureAwait(false);

        if (!result.Succeeded)
            throw EngineCommandFailed("uninstall", result.ExitCode, errors);
    }

    private static async Task<string> GetDownloadUrlAsync()
    {
        using var response = await HttpClient.GetAsync(LatestReleaseUrl).ConfigureAwait(false);
        // TODO: replace flow-control exception with result pattern.
        response.EnsureSuccessStatusCode();

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
            // TODO: replace flow-control exception with result pattern.
            throw new InvalidOperationException("No release assets found.");

        var pkg = assets.FirstOrDefault(a => a.Name!.EndsWith(".pkg", StringComparison.OrdinalIgnoreCase))
                  ?? assets[0];

        // TODO: replace flow-control exception with result pattern.
        return pkg.Url ?? throw new InvalidOperationException("Download URL not found.");
    }

    private static async Task DownloadFileAsync(string downloadUrl, string destinationPath, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        // TODO: replace flow-control exception with result pattern.
        response.EnsureSuccessStatusCode();

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination   = File.Create(destinationPath);
        await contentStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    private static void EnsureSupportedPlatform()
    {
        if (!OperatingSystem.IsMacOS())
            // TODO: replace flow-control exception with result pattern.
            throw new PlatformNotSupportedException("AppleContainers engine is only supported on macOS.");
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

        return result.Succeeded;
    }

    public async Task StartEngineAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsEngineInstalled()) return;

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            BinaryPath,
            ["system", "start", "--enable-kernel-install"],
            workingDirectory: null,
            onOutput: _ => { },
            onError:  line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
            throw EngineCommandFailed("system start", result.ExitCode, errors);
    }

    public async Task StopEngineAsync(CancellationToken cancellationToken = default)
    {
        if (! await IsEngineInstalled()) return;

        var errors = new StringBuilder();
        var result = await _processHost.RunWithResultAsync(
            BinaryPath,
            ["system", "stop"],
            workingDirectory: null,
            onOutput: _ => { },
            onError:  line => errors.AppendLine(line),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!result.Succeeded)
            throw EngineCommandFailed("system stop", result.ExitCode, errors);
    }

    private static InvalidOperationException EngineCommandFailed(
        string command,
        int exitCode,
        StringBuilder errors)
    {
        var message = $"Apple container engine command '{command}' failed with exit code {exitCode}.";
        var detail = errors.ToString().Trim();
        return string.IsNullOrWhiteSpace(detail)
            ? new InvalidOperationException(message)
            : new InvalidOperationException(message + Environment.NewLine + detail);
    }
}
