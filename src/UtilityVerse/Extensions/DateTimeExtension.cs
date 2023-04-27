// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using UtilityVerse.Contracts;

namespace UtilityVerse.Extensions
{
    public static class DateTimeExtension
    {
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

            return IsInBetween(dt.Value, startDateTime.Value, endDateTime.Value);
        }

        /// <summary>
        /// This method will help you determine if the target date time is within the given range (start and end)
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="startDateTime"></param>
        /// <param name="endDateTime"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsInBetween(this DateTime dt, DateTime startDateTime, DateTime endDateTime)
        {
            if (endDateTime < startDateTime)
                throw new ArgumentOutOfRangeException(nameof(endDateTime) + " cannot be smaller than " + nameof(startDateTime));

            return dt >= startDateTime && dt <= endDateTime;
        }

        /// <summary>
        /// This method will convert date time object into UNIX time stamp
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long ToUnixTimeStamp(this DateTime? dt, TimeEnum timeEnum)
        {
            ArgumentNullException.ThrowIfNull(dt);
            return ToUnixTimeStamp(dt.Value, timeEnum);
        }

        /// <summary>
        /// This method will convert date time object into UNIX time stamp
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static long ToUnixTimeStamp(this DateTime dt, TimeEnum timeEnum)
        {
            var offset = new DateTimeOffset(dt);

            return timeEnum switch
            {
                TimeEnum.MilliSecond => offset.ToUnixTimeMilliseconds(),
                TimeEnum.Second => offset.ToUnixTimeSeconds(),
                _ => throw new NotImplementedException()
            };

        }

        /// <summary>
        /// This method will convert any valid time stamp into a DateTime object
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static DateTime ToDateTime(this long? timeStamp, TimeEnum timeEnum)
        {
            ArgumentNullException.ThrowIfNull(timeStamp);
            return ToDateTime(timeStamp.Value, timeEnum);
        }

        /// <summary>
        /// This method will convert any valid time stamp into a DateTime object
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static DateTime ToDateTime(this long timeStamp, TimeEnum timeEnum)
        {
            if (timeStamp < 1)
                throw new ArgumentOutOfRangeException(nameof(timeStamp), "invalid time stamp value");

            return timeEnum switch
            {
                TimeEnum.MilliSecond => DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).DateTime,
                TimeEnum.Second => DateTimeOffset.FromUnixTimeSeconds(timeStamp).DateTime,
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// This method will extract datetime information from the datetime object.
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static (int year, int month, int day, int hour, int minute, int second, int milisecond) FromDateTime(this DateTime? dateTime)
        {
            ArgumentNullException.ThrowIfNull(dateTime);
            return (dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day, dateTime.Value.Hour, dateTime.Value.Minute, dateTime.Value.Second, dateTime.Value.Millisecond);
        }
    }
}
