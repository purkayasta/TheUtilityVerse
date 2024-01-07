// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Helpers;

namespace UtilityVerse.Extensions;

/// <summary>
/// Object Extension
/// </summary>
public static class ObjectExtension
{
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// This method will convert any object into a byte array.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>byte[]</returns>
    /// <exception cref="Exception"></exception>
    public static byte[] ToByteArray(this object obj)
    {
        UtilityVerseException.ThrowIfNull(obj);
        return System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(obj));
    }
#endif
}
