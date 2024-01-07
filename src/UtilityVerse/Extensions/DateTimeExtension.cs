// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System;
using UtilityVerse.Contracts;
using UtilityVerse.Helpers;

namespace UtilityVerse.Extensions;

/// <summary>
/// DateTime Extension
/// </summary>
public static class DateTimeExtension
{
    /// <summary>
    /// This method will help you determine if the target date time is within the given range (start and end)
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="startDateTime"></param>
    /// <param name="endDateTime"></param>
    /// <returns>bool</returns>
    /// <exception cref="Exception"></exception>
    public static bool IsInBetween(this DateTime dt, DateTime startDateTime, DateTime endDateTime)
    {
        UtilityVerseException.ThrowIfNull(dt);
        UtilityVerseException.ThrowIfNull(startDateTime);
        UtilityVerseException.ThrowIfNull(endDateTime);

        if (endDateTime < startDateTime)
            UtilityVerseException.Throw($"{nameof(endDateTime)} cannot be smaller than {nameof(startDateTime)}");

        return dt >= startDateTime && dt <= endDateTime;
    }

    /// <summary>
    /// This method will convert date time object into UNIX time stamp
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="timeEnum"></param>
    /// <returns>long</returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="Exception"></exception>
    public static long ToUnixTimeStamp(this DateTime dt, TimeEnum timeEnum)
    {
        UtilityVerseException.ThrowIfNull(dt);

        return timeEnum switch
        {
            TimeEnum.MilliSecond => new DateTimeOffset(dt).ToUnixTimeMilliseconds(),
            TimeEnum.Second => new DateTimeOffset(dt).ToUnixTimeSeconds(),
            _ => throw new NotImplementedException()
        };
    }

    /// <summary>
    /// Convert datetime object into custom int format. If this method cannot parse the format it will return -1;
    /// </summary>
    /// <param name="dateTime">new DateTime() like [6/10/2023 4:02:06 PM]</param>
    /// <param name="format">yyyyMMdd</param>
    /// <returns>int</returns>
    /// <exception cref="Exception"></exception>
    public static int ToIntDate(this DateTime dateTime, string format = "yyyyMMdd")
    {
        UtilityVerseException.ThrowIfNull(dateTime, nameof(dateTime));
        UtilityVerseException.ThrowIfNullOrEmpty(format, nameof(format));
        return int.TryParse(dateTime.ToString(format), out var result) ? result : -1;
    }

    /// <summary>
    /// This method will extract datetime information from the datetime object.
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns>(int, int, int, int, int, int, int)</returns>
    /// <exception cref="Exception"></exception>
    public static (int year, int month, int day, int hour, int minute, int second, int milisecond) DestructFromDateTime(
        this DateTime dateTime)
    {
        UtilityVerseException.ThrowIfNull(dateTime);
        return (dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Millisecond);
    }

    /// <summary>
    /// this method will convert your current datetime into utc formatted datetime
    /// </summary>
    /// <param name="dateTime"></param>
    /// <param name="includeTime"></param>
    /// <returns>DateTime</returns>
    /// <exception cref="Exception"></exception>
    public static DateTime ToUtcDateTime(this DateTime dateTime, bool includeTime = false)
    {
        UtilityVerseException.ThrowIfNull(dateTime);
        if (includeTime) return new DateTime(year: dateTime.Year, month: dateTime.Month, day: dateTime.Day, hour: dateTime.Hour, minute: dateTime.Minute, second: dateTime.Second, kind: DateTimeKind.Utc);
        return new DateTime(year: dateTime.Year, month: dateTime.Month, day: dateTime.Day, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);
    }
}