using System.Diagnostics;
using Float.Core.Abstractions.Services;

namespace Float.Infrastructure.Common;

public sealed class ProcessHost : IProcessHost
{
    public Task RunAsync(
        string executable,
        string arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null)
        => RunCoreAsync(
            executable,
            arguments,
            workingDirectory,
            onOutput,
            onError,
            cancellationToken,
            environment,
            captureResult: false);

    public Task RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null)
        => RunCoreAsync(
            executable,
            arguments,
            workingDirectory,
            onOutput,
            onError,
            cancellationToken,
            environment,
            captureResult: false);

    public Task<ProcessResult> RunWithResultAsync(
        string executable,
        string arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null)
        => RunCoreAsync(
            executable,
            arguments,
            workingDirectory,
            onOutput,
            onError,
            cancellationToken,
            environment,
            captureResult: true);

    public Task<ProcessResult> RunWithResultAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<string, string>? environment = null)
        => RunCoreAsync(
            executable,
            arguments,
            workingDirectory,
            onOutput,
            onError,
            cancellationToken,
            environment,
            captureResult: true);

    private static async Task<ProcessResult> RunCoreAsync(
        string executable,
        string? arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? environment,
        bool captureResult)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments ?? string.Empty,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Unable to start process '{executable}'.");
        }

        var stdoutTask = PumpAsync(process.StandardOutput, onOutput, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, onError, cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        var result = new ProcessResult(process.ExitCode);
        if (!captureResult && !result.Succeeded)
        {
            throw new InvalidOperationException($"Process '{executable}' failed with exit code {process.ExitCode}.");
        }

        return result;
    }

    private static async Task<ProcessResult> RunCoreAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? environment,
        bool captureResult)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var pair in environment)
            {
                startInfo.Environment[pair.Key] = pair.Value;
            }
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Unable to start process '{executable}'.");
        }

        var stdoutTask = PumpAsync(process.StandardOutput, onOutput, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, onError, cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        var result = new ProcessResult(process.ExitCode);
        if (!captureResult && !result.Succeeded)
        {
            throw new InvalidOperationException($"Process '{executable}' failed with exit code {process.ExitCode}.");
        }

        return result;
    }

    private static async Task PumpAsync(
        StreamReader reader,
        Action<string> onLine,
        CancellationToken cancellationToken)
    {
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is not null)
            {
                onLine(line);
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
