// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

namespace UtilityVerse.Extensions;

/// <summary>
/// Boolean Extension
/// </summary>
public static class BoolExtension
{

    /// <summary>
    /// This method will convert 1 or 0 to boolean
    /// </summary>
    /// <param name="value"></param>
    /// <returns>int</returns>
    public static int ToInt(this bool value) => value ? 1 : 0;


    /// <summary>
    /// this method will convert boolean value into string
    /// </summary>
    /// <param name="val"></param>
    /// <returns>string</returns>
    public static string ToStr(this bool val)
    {
        return val ? "True" : "False";
    }
}
