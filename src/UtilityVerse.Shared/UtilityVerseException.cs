using System;

namespace UtilityVerse.Shared;

public sealed class UtilityVerseException
{
    private static readonly string _value = "value";
    public static void ThrowIfNullOrEmpty(string data, string paramName = null)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrWhiteSpace(data))
            throw new Exception($"{(paramName is not null ? paramName : _value)} is null or empty");
    }

    public static void ThrowIfNull(object data, string paramName = null)
    {
        if (data is null) throw new Exception($"{(paramName is not null ? paramName : _value)} is null");
    }

    public static void Throw(string message) { throw new Exception(message); }
}
