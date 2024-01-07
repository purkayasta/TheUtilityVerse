// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;

#if NETCOREAPP3_1_OR_GREATER
/// <summary>
/// DateOnly Extension
/// </summary>
public static class DateOnlyExtension
{

    /// <summary>
    /// This method will help to determine is the date only instance is in between range or not.
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns>bool</returns>
    /// <exception cref="Exception"></exception>
    public static UtilityVerseResult<bool> IsInBetween(this DateOnly dt, DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            return new(nameof(startDate) + " cannot be greater than " + nameof(endDate));

        return new(dt >= startDate && dt <= endDate);
    }

    /// <summary>
    /// This method will extract dateonly information from the datetime object.
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <param name="format"></param>
    /// <returns>UtilityVerseResult</returns>
    public static UtilityVerseResult<int> ToIntDate(this DateOnly dateOnly, string format = "yyyyMMdd")
    {
        if (int.TryParse(dateOnly.ToString(format), out var result)) return new(result);
        return new("int parsing failed");
    }

    /// <summary>
    /// this method convert dateonly to a valid local datetime
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <returns>DateTime</returns>
    /// <exception cref="Exception"></exception>
    public static DateTime ToDateTime(this DateOnly dateOnly)
    {
        return new DateTime(year: dateOnly.Year, month: dateOnly.Month, day: dateOnly.Day, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Local);
    }

    /// <summary>
    /// this method will convert dateonly object into a valid utc datetime object
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <returns>DateTime</returns>
    /// <exception cref="Exception"></exception>
    public static DateTime ToUtcDateTime(this DateOnly dateOnly)
    {
        return new DateTime(year: dateOnly.Year, month: dateOnly.Month, day: dateOnly.Day, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);
    }
}
#endif