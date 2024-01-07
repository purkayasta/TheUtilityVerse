// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Contracts;
using UtilityVerse.Helpers;
using UtilityVerse.Shared;

namespace UtilityVerse.Extensions;

/// <summary>
/// Long Extension
/// </summary>
public static class LongExtension
{
    /// <summary>
    /// This method will convert any valid time stamp into a DateTime object
    /// </summary>
    /// <param name="timeStamp"></param>
    /// <param name="timerEnum"></param>
    /// <returns>UtilityVerseResult</returns>
    /// <exception cref="NotImplementedException"></exception>
    public static UtilityVerseResult<DateTime> ToDateTime(this long timeStamp, TimeEnum timerEnum)
    {
        if (timeStamp < 1)
            return new UtilityVerseResult<DateTime>($"{nameof(timeStamp)} invalid time stamp value");

        return timerEnum switch
        {
            TimeEnum.MilliSecond => new UtilityVerseResult<DateTime>(DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).DateTime),
            TimeEnum.Second => new UtilityVerseResult<DateTime>(DateTimeOffset.FromUnixTimeSeconds(timeStamp).DateTime),
            _ => throw new NotImplementedException()
        };
    }
}
