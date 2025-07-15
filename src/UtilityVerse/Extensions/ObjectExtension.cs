// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Helpers;
using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;

/// <summary>
/// Object Extension
/// </summary>
public static class ObjectExtension
{
#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// This method will convert any object into a byte array. (NOTE: Only Json Is Supported)
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<byte[]> ToByteArray(this object obj)
    {
        if (obj is null) return new UtilityVerseResult<byte[]>("Object is null or invalid");
        try
        {
            if (Utils.IsStruct(obj.GetType()))
            {
                return new UtilityVerseResult<byte[]>(ConvertStructObjectIntoByteArray(obj));
            }
            return new UtilityVerseResult<byte[]>(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj));
        }
        catch (System.Text.Json.JsonException)
        {
            return new UtilityVerseResult<byte[]>("Json Parsing Exception");
        }
        catch (Exception unknownException)
        {
            return new UtilityVerseResult<byte[]>($"Unknown exception: {unknownException}");
        }
    }

    private static byte[] ConvertStructObjectIntoByteArray(object obj)
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
