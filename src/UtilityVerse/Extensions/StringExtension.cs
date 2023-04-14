using System.Text;

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

            bool isSeparatorExist = !separator.IsNullEmptyOrWhiteSpace();

            for (int index = 0; index < strs.Length; index++)
            {
                sb.Append(strs[index]);

                if (isSeparatorExist)
                    if (index < strs.Length - 1)
                        sb.Append(separator);
            }

            return sb.ToString();
        }
    }
}
