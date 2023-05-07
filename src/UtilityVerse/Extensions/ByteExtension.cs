// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Text.Json;

namespace UtilityVerse.Extensions;

public static class ByteExtension
{
    /// <summary>
    /// This method will convert to an object from byte array
    /// </summary>
    /// <param name="byteArr"></param>
    /// <returns></returns>
    public static object? ToObject(this byte[]? byteArr)
    {
        ArgumentNullException.ThrowIfNull(byteArr);
        return JsonSerializer.Deserialize<object>(byteArr);
    }

}
