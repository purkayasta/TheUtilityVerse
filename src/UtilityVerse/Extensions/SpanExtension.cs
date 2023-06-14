// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Text.Json;

namespace UtilityVerse.Extensions;

/// <summary>
/// Span Extension
/// </summary>
public static class SpanExtension
{
    /// <summary>
    /// This method will convert an readonly span byte array into an object.
    /// </summary>
    /// <param name="spanByteArr"></param>
    /// <returns></returns>
    public static object? ToObject(this ReadOnlySpan<byte> spanByteArr) => JsonSerializer.Deserialize<object>(spanByteArr);
}
