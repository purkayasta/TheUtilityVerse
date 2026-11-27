// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;

#if NETCOREAPP3_1_OR_GREATER

/// <summary>
/// Span Extension
/// </summary>
public static class SpanExtension
{
    /// <summary>
    /// Deserializes UTF-8 JSON from a byte span into an object result. JSON objects are represented as JsonElement values.
    /// </summary>
    /// <param name="spanByteArr"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<object> ToObject(this ReadOnlySpan<byte> spanByteArr)
    {
        if (spanByteArr.Length < 1) return new UtilityVerseResult<object>("Byte Array is invalid");

        try
        {
            return new UtilityVerseResult<object>(System.Text.Json.JsonSerializer.Deserialize<object>(spanByteArr));
        }
        catch (System.Text.Json.JsonException)
        {
            return new UtilityVerseResult<object>("Json parsing error");
        }
        catch (Exception unknownException)
        {
            return new UtilityVerseResult<object>($"Unknown exception: {unknownException}");
        }
    }
}

#endif