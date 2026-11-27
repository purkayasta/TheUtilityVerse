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
    /// Joins a nonempty decimal array with an optional separator, returning an error result for null or empty input.
    /// </summary>
    /// <param name="decimalArray"></param>
    /// <param name="separator"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<string> ToStr(this decimal[] decimalArray, string separator = null)
    {
        if (decimalArray is null || decimalArray.Length < 1) return UtilityVerseResult<string>.Failure("Decimal Array is null or empty");

        var sb = new System.Text.StringBuilder();

        bool isSeparatorExist = !string.IsNullOrEmpty(separator);

        for (int index = 0; index < decimalArray.Length; index++)
        {
            sb.Append(decimalArray[index]);

            if (isSeparatorExist && index < decimalArray.Length - 1)
                sb.Append(separator);
        }

        return UtilityVerseResult<string>.Success(sb.ToString());
    }
}
