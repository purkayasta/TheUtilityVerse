// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Shared;

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
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<byte[]> ToByteArray<T>(this T instance)
    {
        if (instance is null) return new UtilityVerseResult<byte[]>("instance is null or invalid");

        try
        {
            return new UtilityVerseResult<byte[]>(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(instance, UtilityVerse.Helpers.Utils.JsonOptions));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            return new UtilityVerseResult<byte[]>($"Json Exception: {jsonException.Message}");
        }
        catch (Exception unknownException)
        {
            return new UtilityVerseResult<byte[]>($"unknown exception: {unknownException}");
        }
    }

#endif
}
