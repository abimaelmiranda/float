namespace Float.Core.Models;

public sealed record AppSettings
{
    public string DefaultRegistry { get; init; } = "docker";
    public string DefaultArchitecture { get; init; } = "arm64";
    public string DefaultCpuCount { get; init; } = "";
    public string DefaultMemory { get; init; } = "";
    public bool DefaultKeepAlive { get; init; } = false;
    public bool StartImmediately { get; init; } = true;

    public bool ForceDeleteRunning { get; init; } = false;
    public int StopTimeoutSeconds { get; init; } = 5;

    public string Language { get; init; } = "en";
    public bool CloseToTray { get; init; } = true;
    public bool StopEngineOnQuit { get; init; } = true;
    public bool ShowInDock { get; init; } = false;
}
