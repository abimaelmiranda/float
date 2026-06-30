using System.Collections.Generic;

namespace Float.Core.Models.Results.Errors;

public static class DomainErrors
{
    public static Error NotFound(string message)
        => new(DomainErrorCodes.NotFound, message);

    public static Error Validation(string message)
        => new(DomainErrorCodes.Validation, message);

    public static Error Validation(IEnumerable<string> messages)
        => new(DomainErrorCodes.Validation, string.Join("; ", messages));

    public static Error InvalidOperation(string? message = null)
        => new(DomainErrorCodes.InvalidOperation, message ?? "Invalid operation");

    public static Error EngineNotAvailable(string message)
        => new(DomainErrorCodes.EngineNotAvailable, message);

    public static Error CommandFailed(string message)
        => new(DomainErrorCodes.CommandFailed, message);

    public static Error ParseError(string message)
        => new(DomainErrorCodes.ParseError, message);

    public static Error PlatformNotSupported(string message)
        => new(DomainErrorCodes.PlatformNotSupported, message);

    public static Error NotSupported(string message)
        => new(DomainErrorCodes.NotSupported, message);
}
