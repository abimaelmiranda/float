using System;

namespace Float.Core.Utils;

public static class Guard
{
    public static T NotNull<T>(T? value, string paramName)
        where T : class
        => value ?? throw new ArgumentNullException(paramName);

    public static string NotWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value;
    }
}