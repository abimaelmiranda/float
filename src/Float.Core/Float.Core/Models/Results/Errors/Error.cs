namespace Float.Core.Models.Results.Errors;

public record Error(string Code, string? Message = null)
{
    public static readonly Error Empty = new(string.Empty, string.Empty);
}