namespace Float.Core.Abstractions.Services;

public sealed record ProcessResult(int ExitCode)
{
    public bool Succeeded => ExitCode == 0;
}
