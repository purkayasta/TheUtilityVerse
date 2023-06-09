﻿// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using UtilityVerse.Contracts;

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
	/// <param name="timeEnum"></param>
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
	/// Convert datetime object into custom int format. If this method cannot parse the format it will return -1;
	/// </summary>
	/// <param name="dateTime">new DateTime() like [6/10/2023 4:02:06 PM]</param>
	/// <param name="format">yyyyMMdd</param>
	/// <returns>integer number (20230610)</returns>
	public static int ToIntDate(this DateTime? dateTime, string format = "yyyyMMdd")
	{
		ArgumentNullException.ThrowIfNull(dateTime);
		return ToIntDate(dateTime.Value, format);
	}

	/// <summary>
	/// Convert datetime object into custom int format. If this method cannot parse the format it will return -1;
	/// </summary>
	/// <param name="dateTime">new DateTime() like [6/10/2023 4:02:06 PM]</param>
	/// <param name="format">yyyyMMdd</param>
	/// <returns>integer number (20230610)</returns>
	/// <exception cref="InvalidDataException"></exception>
	public static int ToIntDate(this DateTime dateTime, string format = "yyyyMMdd")
	{
		if (string.IsNullOrEmpty(format) || string.IsNullOrWhiteSpace(format))
			throw new InvalidDataException(nameof(format) + " is null or empty");

		return int.TryParse(dateTime.ToString(format), out var result) ? result : -1;
	}

	/// <summary>
	/// This method will extract datetime information from the datetime object.
	/// </summary>
	/// <param name="dateTime"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	public static (int year, int month, int day, int hour, int minute, int second, int milisecond) DestructFromDateTime(
		this DateTime? dateTime)
	{
		ArgumentNullException.ThrowIfNull(dateTime);
		return (dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day, dateTime.Value.Hour,
			dateTime.Value.Minute, dateTime.Value.Second, dateTime.Value.Millisecond);
	}

	/// <summary>
	/// this method will convert your current datetime into utc formatted datetime
	/// </summary>
	/// <param name="dateTime"></param>
	/// <param name="includeTime"></param>
	/// <returns></returns>
	public static DateTime? ToUtcDateTime(this DateTime dateTime, bool includeTime = false)
	{
		if (dateTime == default) return default;
		if (includeTime)
			return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, DateTimeKind.Utc);
		return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Utc);
	}
}