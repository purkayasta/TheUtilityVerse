namespace UtilityVerse.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string val)
            => string.IsNullOrEmpty(val) || string.IsNullOrWhiteSpace(val.Trim());
    }
}
