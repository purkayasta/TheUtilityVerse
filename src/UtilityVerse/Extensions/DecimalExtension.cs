// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;

/// <summary>
/// Decimal Extension
/// </summary>
public static class DecimalExtension
{
    /// <summary>
    /// This extension method will convert any double array into string with separator.
    /// </summary>
    /// <param name="decimalArray"></param>
    /// <param name="separator"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<string> ToStr(this decimal[] decimalArray, string separator = null)
    {
        if (decimalArray is null || decimalArray.Length < 1) UtilityVerseException.Throw("Decimal Array is null or empty");

        var sb = new System.Text.StringBuilder();

        bool isSeparatorExist = !string.IsNullOrEmpty(separator);

        for (int index = 0; index < decimalArray.Length; index++)
        {
            sb.Append(decimalArray[index]);

            if (isSeparatorExist && index < decimalArray.Length - 1)
                sb.Append(separator);
        }

        return new UtilityVerseResult<string>(sb.ToString());
    }
}
