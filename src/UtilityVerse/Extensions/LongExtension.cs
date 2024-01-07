// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using UtilityVerse.Contracts;
using UtilityVerse.Helpers;

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
	/// <param name="timeEnum"></param>
	/// <returns></returns>
	public static DateTime ToDateTime(this long? timeStamp, TimeEnum timeEnum)
	{
		UtilityVerseException.ThrowIfNull(timeStamp);
		return ToDateTime(timeStamp.Value, timeEnum);
	}

	/// <summary>
	/// This method will convert any valid time stamp into a DateTime object
	/// </summary>
	/// <param name="timeStamp"></param>
	/// <param name="timeEnum"></param>
	/// <returns></returns>
	/// <exception cref="Exception"></exception>
	/// <exception cref="NotImplementedException"></exception>
	public static DateTime ToDateTime(this long timeStamp, TimeEnum timeEnum)
	{
		if (timeStamp < 1)
            UtilityVerseException.Throw($"{nameof(timeStamp)} invalid time stamp value");

		return timeEnum switch
		{
			TimeEnum.MilliSecond => DateTimeOffset.FromUnixTimeMilliseconds(timeStamp).DateTime,
			TimeEnum.Second => DateTimeOffset.FromUnixTimeSeconds(timeStamp).DateTime,
			_ => throw new NotImplementedException()
		};
	}
}
