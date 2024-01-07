using System;

namespace UtilityVerse.Helpers;

internal class UtilityVerseException
{
    private static readonly string _value = "value";
    internal static void ThrowIfNullOrEmpty(string data, string paramName = null)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrWhiteSpace(data))
            throw new Exception($"{(paramName is not null ? paramName : _value)} is null or empty");
    }

    internal static void ThrowIfNull(object data, string paramName = null)
    {
        if (data is null) throw new Exception($"{(paramName is not null ? paramName : _value)} is null");
    }

    internal static void Throw(string message) { throw new Exception(message); }
}
