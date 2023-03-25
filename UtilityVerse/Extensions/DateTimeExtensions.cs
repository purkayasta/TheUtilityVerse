namespace UtilityVerse.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsInBetween(this DateTime? dt, DateTime? startDateTime, DateTime? endDateTime)
        {
            if (dt is null) throw new ArgumentNullException(nameof(dt));
            if (startDateTime is null) throw new ArgumentNullException(nameof(startDateTime));
            if (endDateTime is null) throw new ArgumentNullException(nameof(endDateTime));

            return dt >= startDateTime && dt < endDateTime;
        }

    }
}
