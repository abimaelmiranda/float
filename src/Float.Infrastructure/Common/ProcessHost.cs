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

    private static Task<ProcessResult> RunCoreAsync(
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
        ApplyEnvironment(startInfo, environment);
        return RunCoreAsync(startInfo, onOutput, onError, cancellationToken, captureResult);
    }

    private static Task<ProcessResult> RunCoreAsync(
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
            startInfo.ArgumentList.Add(argument);
        ApplyEnvironment(startInfo, environment);
        return RunCoreAsync(startInfo, onOutput, onError, cancellationToken, captureResult);
    }

    private static void ApplyEnvironment(ProcessStartInfo startInfo, IReadOnlyDictionary<string, string>? environment)
    {
        if (environment is null) return;
        foreach (var pair in environment)
            startInfo.Environment[pair.Key] = pair.Value;
    }

    private static async Task<ProcessResult> RunCoreAsync(
        ProcessStartInfo startInfo,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken,
        bool captureResult)
    {
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        if (!process.Start())
            throw new InvalidOperationException($"Unable to start process '{startInfo.FileName}'.");

        var stderrBuffer = new System.Text.StringBuilder();
        Action<string> stderrHandler = line => { stderrBuffer.AppendLine(line); onError(line); };

        var stdoutTask = PumpAsync(process.StandardOutput, onOutput, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, stderrHandler, cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        var result = new ProcessResult(process.ExitCode);
        if (!captureResult && !result.Succeeded)
        {
            var stderr = stderrBuffer.ToString().Trim();
            var detail = string.IsNullOrEmpty(stderr) ? "" : $"\n{stderr}";
            throw new InvalidOperationException(
                $"Process '{startInfo.FileName}' failed with exit code {process.ExitCode}.{detail}");
        }

        return result;
    }

    private static async Task PumpAsync(
        StreamReader reader,
        Action<string> onLine,
        CancellationToken cancellationToken)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            onLine(line);
        }
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
