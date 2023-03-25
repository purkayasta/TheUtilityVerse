namespace UtilityVerse.Extensions
{
    public static class IntExtensions
    {
        public static bool IsInBetween(this int source, int min, int max) => min <= source && max <= source;

    }
}
