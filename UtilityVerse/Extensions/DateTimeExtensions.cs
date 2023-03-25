namespace UtilityVerse.Extensions
{
    public static class DateTimeExtensions
    {
        private static DateTime _defaultDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// This method will help you determine if the target datetime is within the given range (start and end)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsInBetween(this DateTime? dt, DateTime? startDateTime, DateTime? endDateTime)
        {
            if (dt is null) throw new ArgumentNullException(nameof(dt));
            if (startDateTime is null) throw new ArgumentNullException(nameof(startDateTime));
            if (endDateTime is null) throw new ArgumentNullException(nameof(endDateTime));

            return dt >= startDateTime && dt < endDateTime;
        }

        public static int ToUnixTimeStamp(this DateTime? dt)
        {
            if (dt is null) return 0;
            return (int)dt.Value.Subtract(_defaultDateTime).TotalSeconds;
        }

        public static DateTime ToDateTime(this int? timeStamp)
        {
            ArgumentNullException.ThrowIfNull(timeStamp);
            if (timeStamp < 1) throw new ArgumentOutOfRangeException(nameof(timeStamp), "invalid value");
            return _defaultDateTime.AddSeconds(timeStamp.Value);
        }
    }
}
