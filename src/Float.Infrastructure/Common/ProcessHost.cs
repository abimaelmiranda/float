using System.Diagnostics;
using Float.Core.Abstractions.Services;
using Float.Core.Models.Results;
using Float.Core.Models.Results.Errors;

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
            throwOnFailure: true);

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
            throwOnFailure: true);

    public Task<Result> RunWithResultAsync(
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
            throwOnFailure: false);

    public Task<Result> RunWithResultAsync(
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
            throwOnFailure: false);

    public Task<Result> RunWithResultAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        string? stdinInput,
        CancellationToken cancellationToken = default)
        => RunCoreAsync(
            executable,
            arguments,
            workingDirectory,
            onOutput,
            onError,
            cancellationToken,
            environment: null,
            throwOnFailure: false,
            stdinInput: stdinInput);

    private static Task<Result> RunCoreAsync(
        string executable,
        string? arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? environment,
        bool throwOnFailure)
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
        return RunCoreAsync(startInfo, onOutput, onError, cancellationToken, throwOnFailure);
    }

    private static Task<Result> RunCoreAsync(
        string executable,
        IReadOnlyList<string> arguments,
        string? workingDirectory,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string>? environment,
        bool throwOnFailure,
        string? stdinInput = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdinInput is not null,
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);
        ApplyEnvironment(startInfo, environment);
        return RunCoreAsync(startInfo, onOutput, onError, cancellationToken, throwOnFailure, stdinInput);
    }

    private static void ApplyEnvironment(ProcessStartInfo startInfo, IReadOnlyDictionary<string, string>? environment)
    {
        if (environment is null) return;
        foreach (var pair in environment)
            startInfo.Environment[pair.Key] = pair.Value;
    }

    private static async Task<Result> RunCoreAsync(
        ProcessStartInfo startInfo,
        Action<string> onOutput,
        Action<string> onError,
        CancellationToken cancellationToken,
        bool throwOnFailure,
        string? stdinInput = null)
    {
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        if (!process.Start())
        {
            var error = DomainErrors.CommandFailed($"Unable to start process '{startInfo.FileName}'.");
            if (throwOnFailure)
                throw new InvalidOperationException(error.Message);
            return Result.WithFailure(error);
        }

        if (stdinInput is not null)
        {
            await process.StandardInput.WriteLineAsync(stdinInput).ConfigureAwait(false);
            process.StandardInput.Close();
        }

        var stderrBuffer = new System.Text.StringBuilder();
        Action<string> stderrHandler = line => { stderrBuffer.AppendLine(line); onError(line); };

        var stdoutTask = PumpAsync(process.StandardOutput, onOutput, cancellationToken);
        var stderrTask = PumpAsync(process.StandardError, stderrHandler, cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = stderrBuffer.ToString().Trim();
            var detail = string.IsNullOrEmpty(stderr) ? "" : $"\n{stderr}";
            var error = DomainErrors.CommandFailed(
                $"Process '{startInfo.FileName}' failed with exit code {process.ExitCode}.{detail}");
            if (throwOnFailure)
                throw new InvalidOperationException(error.Message);
            return Result.WithFailure(error);
        }

        return Result.WithSuccess();
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
