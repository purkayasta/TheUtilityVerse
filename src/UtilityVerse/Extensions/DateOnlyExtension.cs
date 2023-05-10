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
}
