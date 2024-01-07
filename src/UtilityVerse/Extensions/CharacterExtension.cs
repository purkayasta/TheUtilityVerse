// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;

/// <summary>
/// Character Extension
/// </summary>
public static class CharacterExtension
{
    /// <summary>
    /// This extension method will convert any double array into string with separator.
    /// </summary>
    /// <param name="characterArray"></param>
    /// <param name="separator"></param>
    /// <returns>string</returns>
    public static UtilityVerseResult<string> ToStr(this char[] characterArray, string separator = null)
    {
        if (characterArray is null || characterArray.Length < 1)
            return new UtilityVerseResult<string>("Invalid Character Array is given");

        try
        {
            var sb = new System.Text.StringBuilder();

            bool isSeparatorExist = !string.IsNullOrEmpty(separator);

            for (int index = 0; index < characterArray.Length; index++)
            {
                sb.Append(characterArray[index]);

                if (isSeparatorExist && index < characterArray.Length - 1)
                    sb.Append(separator);
            }

            return new UtilityVerseResult<string>(sb.ToString());
        }
        catch (System.Exception unknownException)
        {
            return new UtilityVerseResult<string>($"Unknown exception: {unknownException}");
        }
    }
}
