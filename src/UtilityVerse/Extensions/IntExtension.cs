// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Text;

namespace UtilityVerse.Extensions;

public static class IntExtension
{
    /// <summary>
    /// This method will help to determine that source number is in between or not.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static bool IsInBetween(this int? source, int? min, int? max)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(min, nameof(min));
        ArgumentNullException.ThrowIfNull(max, nameof(max));

        return IsInBetween(source.Value, min.Value, max.Value);
    }

    /// <summary>
    /// This method will help to determine that source number is in between or not.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static bool IsInBetween(this int source, int min, int max)
    {
        if (min > max)
            throw new ArgumentOutOfRangeException(nameof(min) + " cannot be greater than " + nameof(max));

        return source >= min && source <= max;
    }

    /// <summary>
    /// This method will convert 1 or 0 to boolean
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int ToInt(this bool value) => value ? 1 : 0;


    /// <summary>
    /// This extension method will convert any int array into string with separator.
    /// </summary>
    /// <param name="strs"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string ToStr(this int[]? intArray, string? separator = null)
    {
        if (intArray is null || intArray.Length < 1) return string.Empty;

        var sb = new StringBuilder();

        bool isSeparatorExist = !string.IsNullOrEmpty(separator);

        for (int index = 0; index < intArray.Length; index++)
        {
            sb.Append(intArray[index]);

            if (isSeparatorExist && index < intArray.Length - 1)
                sb.Append(separator);
        }

        return sb.ToString();
    }
}
