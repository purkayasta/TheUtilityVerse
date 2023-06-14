// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Text;

namespace UtilityVerse.Extensions;

/// <summary>
/// Double Extension
/// </summary>
public static class DoubleExtension
{
	/// <summary>
	/// This extension method will convert any double array into string with separator.
	/// </summary>
	/// <param name="doubleArray"></param>
	/// <param name="separator"></param>
	/// <returns></returns>
	public static string ToStr(this double[]? doubleArray, string? separator = null)
	{
		if (doubleArray is null || doubleArray.Length < 1) return string.Empty;
		var sb = new StringBuilder();

		bool isSeparatorExist = !string.IsNullOrEmpty(separator);

		for (int index = 0; index < doubleArray.Length; index++)
		{
			sb.Append(doubleArray[index]);

			if (isSeparatorExist && index < doubleArray.Length - 1)
				sb.Append(separator);
		}

		return sb.ToString();
	}
}
