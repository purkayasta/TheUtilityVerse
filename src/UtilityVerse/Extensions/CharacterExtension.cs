// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Text;

namespace UtilityVerse.Extensions;

public static class CharacterExtension
{

    /// <summary>
    /// This extension method will convert any double array into string with separator.
    /// </summary>
    /// <param name="characterArray"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string ToStr(this char[]? characterArray, string? separator = null)
    {
        if (characterArray is null || characterArray.Length < 1) return string.Empty;

        var sb = new StringBuilder();

        bool isSeparatorExist = !string.IsNullOrEmpty(separator);

        for (int index = 0; index < characterArray.Length; index++)
        {
            sb.Append(characterArray[index]);

            if (isSeparatorExist && index < characterArray.Length - 1)
                sb.Append(separator);
        }

        return sb.ToString();
    }
}
