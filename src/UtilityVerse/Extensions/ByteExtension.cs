// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Helpers;

namespace UtilityVerse.Extensions;

/// <summary>
/// Byte extensions
/// </summary>
public static class ByteExtension
{
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// This method will convert to an object from byte array
    /// </summary>
    /// <param name="byteArr"></param>
    /// <returns>object</returns>
    public static object ToObject(this byte[] byteArr)
    {
        UtilityVerseException.ThrowIfNull(byteArr);
        return System.Text.Json.JsonSerializer.Deserialize<object>(byteArr);
    }
#endif

#if NETCOREAPP3_1_OR_GREATER

    /// <summary>
    /// This method will convert byte array into desired model.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="byteArr"></param>
    /// <returns>T</returns>
    public static T To<T>(this byte[] byteArr)
    {
        UtilityVerseException.ThrowIfNull(byteArr);
        return System.Text.Json.JsonSerializer.Deserialize<T>(byteArr);
    }
#endif
}
