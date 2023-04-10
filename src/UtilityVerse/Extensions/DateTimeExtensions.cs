namespace UtilityVerse.Extensions
{
    public static class DateTimeExtensions
    {
        private static DateTime _defaultDateTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// This method will help you determine if the target date time is within the given range (start and end)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsInBetween(this DateTime? dt, DateTime? startDateTime, DateTime? endDateTime)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(startDateTime, nameof(startDateTime));
            ArgumentNullException.ThrowIfNull(endDateTime, nameof(endDateTime));

            return dt.Value >= startDateTime && dt.Value <= endDateTime;
        }

        /// <summary>
        /// This method will convert date time object into UNIX time stamp
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int ToUnixTimeStamp(this DateTime? dt)
        {
            ArgumentNullException.ThrowIfNull(dt);
            return (int)dt.Value.Subtract(_defaultDateTime).TotalSeconds;
        }

        /// <summary>
        /// This method will convert any valid time stamp into a DateTime object
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static DateTime ToDateTime(this int? timeStamp)
        {
            ArgumentNullException.ThrowIfNull(timeStamp);
            if (timeStamp < 1) throw new ArgumentOutOfRangeException(nameof(timeStamp), "invalid time stamp value");
            return _defaultDateTime.AddSeconds(timeStamp.Value);
        }
    }
}
