namespace Float.Core.Models.Results.Errors;

public static class DomainErrorCodes
{
    public const string NotFound = "ENTITY.NOT_FOUND";
    public const string Validation = "VALIDATION_ERROR";
    public const string InvalidOperation = "INVALID_OPERATION";
    public const string EngineNotAvailable = "ENGINE_NOT_AVAILABLE";
    public const string CommandFailed = "COMMAND_FAILED";
    public const string ParseError = "PARSE_ERROR";
    public const string PlatformNotSupported = "PLATFORM_NOT_SUPPORTED";
    public const string NotSupported = "NOT_SUPPORTED";
}
