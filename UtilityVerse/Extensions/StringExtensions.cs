using System.Text;

namespace UtilityVerse.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullEmptyOrWhiteSpace(this string? val)
        {
            if (val == null) return false;
            return string.IsNullOrEmpty(val) || string.IsNullOrWhiteSpace(val.Trim());
        }


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
