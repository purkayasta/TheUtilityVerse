using System;

namespace UtilityVerse.Shared;

public sealed class UtilityVerseException
{
    private static readonly string _value = "value";
    /// <summary>Throws Exception when the string is null, empty, or whitespace, using the supplied parameter name in the message.</summary>
    public static void ThrowIfNullOrEmpty(string data, string paramName = null)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrWhiteSpace(data))
            throw new Exception($"{(paramName is not null ? paramName : _value)} is null or empty");
    }

    /// <summary>Throws Exception when the value is null, using the supplied parameter name in the message.</summary>
    public static void ThrowIfNull(object data, string paramName = null)
    {
        if (data is null) throw new Exception($"{(paramName is not null ? paramName : _value)} is null");
    }

    /// <summary>Throws Exception with the supplied message.</summary>
    public static void Throw(string message) { throw new Exception(message); }
}
