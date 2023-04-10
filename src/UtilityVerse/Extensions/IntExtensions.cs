namespace UtilityVerse.Extensions
{
    public static class IntExtensions
    {
        /// <summary>
        /// This method will help to determine that source number is in between or not.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsInBetween(this int? source, int? min, int? max)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(min, nameof(min));
            ArgumentNullException.ThrowIfNull(max, nameof(max));

            return source.Value >= min && source.Value <= max;
        }
    }
}
