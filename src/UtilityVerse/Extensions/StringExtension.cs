// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Text;
using System.Text.RegularExpressions;
using UtilityVerse.Contracts;
using UtilityVerse.Helpers;

namespace UtilityVerse.Extensions;

/// <summary>
/// All string extensions
/// </summary>
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

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return true;

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("no", StringComparison.OrdinalIgnoreCase))
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

    /// <summary>
    /// this method will convert your file path based on the runtime
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static string ConvertOsWisePath(this string? path)
    {
        if (path.IsNullOrEmptyOrWhiteSpace()) return string.Empty;

        if (Utility.IsItWindows) return path!.Replace("/", "\\");
        return path!.Replace("\\", "/");
    }

    /// <summary>
    /// this method will encode any string into base64 string depending on the encoding
    /// </summary>
    /// <param name="value"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
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

    /// <summary>
    /// this method will convert base64 into a normal string based into encoding format.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="encoding"></param>
    /// <returns></returns>
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

    /// <summary>
    /// this method will check if this is a valid email address or not.
    /// </summary>
    /// <param name="emailAddress"></param>
    /// <returns></returns>
    public static bool IsValidEmail(this string emailAddress)
        => Regex.IsMatch(emailAddress, Variables.EmailAddressRegex, RegexOptions.Compiled);

    /// <summary>
    /// this method will check is the ipaddress is valid or not.
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    public static bool IsIpAddress(this string ipAddress)
        => Regex.IsMatch(ipAddress, Variables.IpAddressRegex, RegexOptions.Compiled);

    /// <summary>
    /// this method will convert any url string to a slug url
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static string ToSlugUrl(this string url)
    {
        var urlString = Encoding.ASCII.GetString(Encoding.UTF8.GetBytes(url))?.ToLowerInvariant();
        if (string.IsNullOrEmpty(urlString)) return string.Empty;

        urlString = Regex.Replace(urlString, Variables.InvalidCharacterRegex, "", RegexOptions.Compiled);
        urlString = Regex.Replace(urlString, @"\s+", " ", RegexOptions.Compiled).Trim();
        urlString = urlString[..(urlString.Length <= 45 ? urlString.Length : 45)].Trim();
        urlString = Regex.Replace(urlString, @"\s", "-", RegexOptions.Compiled);

        return urlString;
    }
}