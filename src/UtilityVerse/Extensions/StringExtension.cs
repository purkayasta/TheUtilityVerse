// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Text;
using UtilityVerse.Contracts;

namespace UtilityVerse.Extensions
{
    public static class StringExtension
    {
        /// <summary>
        /// This extension method will help to validate if the string is null empty and whitespace or not.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool IsNullEmptyOrWhiteSpace(this string? val)
        {
            if (val is null) return true;
            return string.IsNullOrEmpty(val) || string.IsNullOrWhiteSpace(val.Trim());
        }


        #region Conversion

        /// <summary>
        /// This extension method will convert any string array into string with separator.
        /// </summary>
        /// <param name="strs"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string AsString(this string[]? strs, string? separator = null)
        {
            if (strs is null || strs.Length < 1) return string.Empty;

            var sb = new StringBuilder();

            bool isSeparatorExist = !string.IsNullOrEmpty(separator);

            for (int index = 0; index < strs.Length; index++)
            {
                sb.Append(strs[index]);

                if (isSeparatorExist)
                    if (index < strs.Length - 1)
                        sb.Append(separator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// This extension method will convert any int array into string with separator.
        /// </summary>
        /// <param name="strs"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string AsString(this int[]? intArray, string? separator = null)
        {
            if (intArray is null || intArray.Length < 1) return string.Empty;

            var sb = new StringBuilder();

            bool isSeparatorExist = !string.IsNullOrEmpty(separator);

            for (int index = 0; index < intArray.Length; index++)
            {
                sb.Append(intArray[index]);

                if (isSeparatorExist)
                    if (index < intArray.Length - 1)
                        sb.Append(separator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// This extension method will convert any double array into string with separator.
        /// </summary>
        /// <param name="strs"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string AsString(this double[]? doubleArray, string? separator = null)
        {
            if (doubleArray is null || doubleArray.Length < 1) return string.Empty;
            var sb = new StringBuilder();

            bool isSeparatorExist = !string.IsNullOrEmpty(separator);

            for (int index = 0; index < doubleArray.Length; index++)
            {
                sb.Append(doubleArray[index]);

                if (isSeparatorExist)
                    if (index < doubleArray.Length - 1)
                        sb.Append(separator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// This extension method will convert any double array into string with separator.
        /// </summary>
        /// <param name="strs"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string AsString(this decimal[]? decimalArray, string? separator = null)
        {
            if (decimalArray is null || decimalArray.Length < 1) return string.Empty;

            var sb = new StringBuilder();

            bool isSeparatorExist = !string.IsNullOrEmpty(separator);

            for (int index = 0; index < decimalArray.Length; index++)
            {
                sb.Append(decimalArray[index]);

                if (isSeparatorExist)
                    if (index < decimalArray.Length - 1)
                        sb.Append(separator);
            }

            return sb.ToString();
        }

        /// <summary>
        /// This extension method will convert any double array into string with separator.
        /// </summary>
        /// <param name="strs"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string AsString(this char[]? characterArray, string? separator = null)
        {
            if (characterArray is null || characterArray.Length < 1) return string.Empty;

            var sb = new StringBuilder();

            bool isSeparatorExist = !string.IsNullOrEmpty(separator);

            for (int index = 0; index < characterArray.Length; index++)
            {
                sb.Append(characterArray[index]);

                if (isSeparatorExist)
                    if (index < characterArray.Length - 1)
                        sb.Append(separator);
            }

            return sb.ToString();
        }
        #endregion

        public static string ConvertOsWisePath(this string? path)
        {
            if (path.IsNullEmptyOrWhiteSpace()) return string.Empty;

            if (Utility.IsWindows) return path!.Replace("/", "\\");
            return path!.Replace("\\", "/");
        }

        public static string EncodeToBase64(this string? value, EncodingEnum encoding = EncodingEnum.UTF8)
        {
            if (value.IsNullEmptyOrWhiteSpace()) return string.Empty;

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
            if (value.IsNullEmptyOrWhiteSpace()) return string.Empty;

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
}
