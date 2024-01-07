// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System;

namespace UtilityVerse.Extensions;

#if NETCOREAPP3_1_OR_GREATER

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
    public static object ToObject(this ReadOnlySpan<byte> spanByteArr) => System.Text.Json.JsonSerializer.Deserialize<object>(spanByteArr);
}

#endif