// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Text;
using UtilityVerse.Contracts;

namespace UtilityVerse.Extensions;

public static class StringExtension
{

    /// <summary>
    /// This method will convert any boolean string value to boolean
    /// </summary>
    /// <param name="value">"YES", "1", "true"</param>
    /// <returns></returns>
    public static bool ToBoolean(this string? value)
    {
        if (value.IsNullOrEmptyOrWhiteSpace()) return false;

        value = value!.ToLower();

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("0", StringComparison.OrdinalIgnoreCase) || value.Equals("no", StringComparison.OrdinalIgnoreCase))
            return false;

        return Convert.ToBoolean(value);
    }

    /// <summary>
    /// This extension method will help to validate if the string is null empty and whitespace or not.
    /// </summary>
    /// <param name="val"></param>
    /// <returns></returns>
    public static bool IsNullOrEmptyOrWhiteSpace(this string? val)
    {
        if (val is null) return true;
        return string.IsNullOrEmpty(val) || string.IsNullOrWhiteSpace(val.Trim());
    }


    /// <summary>
    /// This extension method will convert any string array into string with separator.
    /// </summary>
    /// <param name="strs"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static string ToStr(this string[]? strs, string? separator = null)
    {
        if (strs is null || strs.Length < 1) return string.Empty;

        var sb = new StringBuilder();

        bool isSeparatorExist = !string.IsNullOrEmpty(separator);

        for (int index = 0; index < strs.Length; index++)
        {
            sb.Append(strs[index]);

            if (isSeparatorExist && index < strs.Length - 1)
                sb.Append(separator);
        }

        return sb.ToString();
    }

    public static string ConvertOsWisePath(this string? path)
    {
        if (path.IsNullOrEmptyOrWhiteSpace()) return string.Empty;

        if (Utility.IsWindows) return path!.Replace("/", "\\");
        return path!.Replace("\\", "/");
    }

    public static string EncodeToBase64(this string? value, EncodingEnum encoding = EncodingEnum.UTF8)
    {
        if (value.IsNullOrEmptyOrWhiteSpace()) return string.Empty;

        return encoding switch
        {
            EncodingEnum.UTF8 => Convert.ToBase64String(Encoding.UTF8.GetBytes(value!)),
            EncodingEnum.UTF32 => Convert.ToBase64String(Encoding.UTF32.GetBytes(value!)),
            EncodingEnum.Unicode => Convert.ToBase64String(Encoding.Unicode.GetBytes(value!)),
            EncodingEnum.Ascii => Convert.ToBase64String(Encoding.ASCII.GetBytes(value!)),
            EncodingEnum.Latin => Convert.ToBase64String(Encoding.Latin1.GetBytes(value!)),
            _ => string.Empty
        };
    }

    public static string DecodeFromBase64(this string? value, EncodingEnum encoding = EncodingEnum.UTF8)
    {
        if (value.IsNullOrEmptyOrWhiteSpace()) return string.Empty;

        return encoding switch
        {
            EncodingEnum.UTF8 => Encoding.UTF8.GetString(Convert.FromBase64String(value!)),
            EncodingEnum.UTF32 => Encoding.UTF32.GetString(Convert.FromBase64String(value!)),
            EncodingEnum.Unicode => Encoding.Unicode.GetString(Convert.FromBase64String(value!)),
            EncodingEnum.Ascii => Encoding.ASCII.GetString(Convert.FromBase64String(value!)),
            EncodingEnum.Latin => Encoding.Latin1.GetString(Convert.FromBase64String(value!)),
            _ => string.Empty
        };
    }
}
