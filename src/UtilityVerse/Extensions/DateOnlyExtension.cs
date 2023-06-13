// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


namespace UtilityVerse.Extensions;

public static class DateOnlyExtension
{
    /// <summary>
    /// This method will help to determine is the date only instance is in between range or not.
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public static bool IsInBetween(this DateOnly? dt, DateOnly? startDate, DateOnly? endDate)
    {
        ArgumentNullException.ThrowIfNull(dt);
        ArgumentNullException.ThrowIfNull(startDate, nameof(startDate));
        ArgumentNullException.ThrowIfNull(endDate, nameof(endDate));

        return IsInBetween(dt.Value, startDate.Value, endDate.Value);
    }

    /// <summary>
    /// This method will help to determine is the date only instance is in between range or not.
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool IsInBetween(this DateOnly dateOnly, DateOnly startDate, DateOnly endDate)
    {
        if (startDate > endDate)
            throw new ArgumentOutOfRangeException(nameof(startDate) + " cannot be greater than " + nameof(endDate));

        return dateOnly >= startDate && dateOnly <= endDate;
    }


    /// <summary>
    /// This method will extract dateonly information from the datetime object.
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static int ToIntDate(this DateOnly? dateOnly, string format = "yyyyMMdd")
    {
        ArgumentNullException.ThrowIfNull(dateOnly);
        return ToIntDate(dateOnly.Value, format);
    }

    /// <summary>
    /// This method will extract dateonly information from the datetime object.
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static int ToIntDate(this DateOnly dateOnly, string format = "yyyyMMdd")
    {
        if (string.IsNullOrEmpty(format) || string.IsNullOrWhiteSpace(format))
            throw new InvalidDataException(nameof(format) + " is null or empty");

        return int.TryParse(dateOnly.ToString(format), out var result) ? result : -1;
    }

    /// <summary>
    /// this method convert dateonly to a valid local datetime
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <returns></returns>
    public static DateTime? ToDateTime(this DateOnly dateOnly)
    {
        if (dateOnly == default) return default;
        return new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Local);
    }

    /// <summary>
    /// this method will convert dateonly object into a valid utc datetime object
    /// </summary>
    /// <param name="dateOnly"></param>
    /// <returns></returns>
    public static DateTime? ToUtcDateTime(this DateOnly dateOnly)
    {
        if (dateOnly == default) return default;
        return new DateTime(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, DateTimeKind.Utc);
    }
}