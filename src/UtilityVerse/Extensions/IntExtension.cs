// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Helpers;
using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;

/// <summary>
/// Int Extension
/// </summary>
public static class IntExtension
{
    /// <summary>
    /// This method will help to determine that source number is in between or not.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<bool> IsInBetween(this int source, int min, int max)
    {
        if (min > max)
            return new UtilityVerseResult<bool>(nameof(min) + " cannot be greater than " + nameof(max));

        return new UtilityVerseResult<bool>(source >= min && source <= max);
    }


    /// <summary>
    /// This extension method will convert any int array into string with separator.
    /// </summary>
    /// <param name="intArray"></param>
    /// <param name="separator"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<string> ToStr(this int[] intArray, string separator = null)
    {
        if (intArray is null || intArray.Length < 1) return new UtilityVerseResult<string>("int Array is empty");

        var sb = new System.Text.StringBuilder();

        bool isSeparatorExist = !string.IsNullOrEmpty(separator);

        for (int index = 0; index < intArray.Length; index++)
        {
            sb.Append(intArray[index]);

            if (isSeparatorExist && index < intArray.Length - 1)
                sb.Append(separator);
        }

        return new UtilityVerseResult<string>(sb.ToString());
    }
}