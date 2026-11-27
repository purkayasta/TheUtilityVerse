// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;
/// <summary>
/// Byte extensions
/// </summary>
public static class ByteExtension
{
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// This method will convert to an object from byte array (NOTE: Only Json is Supported)
    /// </summary>
    /// <param name="byteArr"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<object> ToObject(this byte[] byteArr)
    {
        if (byteArr is null || byteArr.Length < 1) return new UtilityVerseResult<object>("byte array is invalid");
        try
        {
            return new UtilityVerseResult<object>(System.Text.Json.JsonSerializer.Deserialize<object>(byteArr, UtilityVerse.Helpers.Utils.JsonOptions));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            return new UtilityVerseResult<object>($"Json Exception: {jsonException}");
        }
        catch (Exception unknownException)
        {
            return new UtilityVerseResult<object>($"Unknown exception: {unknownException}");
        }
    }
#endif

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// This method will convert byte array into desired model. (NOTE: Only Json is Supported)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="byteArr"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<T> To<T>(this byte[] byteArr)
    {
        if (byteArr is null || byteArr.Length < 1) return new UtilityVerseResult<T>("Invalid Byte Array");

        try
        {
            return new UtilityVerseResult<T>(System.Text.Json.JsonSerializer.Deserialize<T>(byteArr, UtilityVerse.Helpers.Utils.JsonOptions));
        }
        catch (System.Text.Json.JsonException jsonException)
        {
            return new UtilityVerseResult<T>($"Json Exception: {jsonException}");
        }
        catch (Exception unknownException)
        {
            return new UtilityVerseResult<T>($"Unknown Exception: {unknownException}");
        }
    }

#endif
}
