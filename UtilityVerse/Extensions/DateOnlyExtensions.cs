namespace UtilityVerse.Extensions
{
#if NET6_0_OR_GREATER
    public static class DateOnlyExtensions
    {

        public static bool IsInBetween(this DateOnly dt)
        {
            return false;
        }
    }
#endif
}
