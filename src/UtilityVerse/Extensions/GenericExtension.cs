// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Helpers;
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
            if (Utils.IsStruct(instance.GetType()))
            {
                return new UtilityVerseResult<byte[]>(ConvertStructIntoByteArray(instance));
            }
            return new UtilityVerseResult<byte[]>(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes<T>(instance));
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

    private static byte[] ConvertStructIntoByteArray(object obj)
    {
        int size = System.Runtime.InteropServices.Marshal.SizeOf(obj);
        byte[] arr = new byte[size];
        IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        System.Runtime.InteropServices.Marshal.StructureToPtr(obj, ptr, true);
        System.Runtime.InteropServices.Marshal.Copy(ptr, arr, 0, size);
        System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
        return arr;
    }
#endif
}
