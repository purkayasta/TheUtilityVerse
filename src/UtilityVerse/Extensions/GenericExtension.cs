// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Helpers;

namespace UtilityVerse.Extensions;

/// <summary>
/// Generic Extension
/// </summary>
public static class GenericExtension
{
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// This method will convert poco model into byte array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance"></param>
    /// <returns>byte[]</returns>
    /// <exception cref="Exception"></exception>
    public static byte[] ToByteArray<T>(this T instance)
    {
        UtilityVerseException.ThrowIfNull(instance);
        return System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize<T>(instance));
    }
#endif
}
