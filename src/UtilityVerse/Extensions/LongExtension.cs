// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Contracts;

namespace UtilityVerse.Extensions;

public static class LongExtension
{
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
}
