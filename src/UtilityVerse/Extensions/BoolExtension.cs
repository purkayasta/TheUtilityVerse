// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

namespace UtilityVerse.Extensions;

/// <summary>
/// Boolean Extension
/// </summary>
public static class BoolExtension
{
	/// <summary>
	/// This will convert boolean to int
	/// </summary>
	/// <param name="val"></param>
	/// <returns></returns>
	public static int ToInt(this bool? val)
	{
		if (!val.HasValue) return 0;

		return val.Value ? 1 : 0;
	}

	/// <summary>
	/// this method will convert boolean value into string
	/// </summary>
	/// <param name="val"></param>
	/// <returns></returns>
	public static string ToStr(this bool? val)
	{
		if (!val.HasValue) return string.Empty;
		return val.Value ? "True" : "False";
	}
}
