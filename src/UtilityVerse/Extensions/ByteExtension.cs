// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Text.Json;

namespace UtilityVerse.Extensions;

/// <summary>
/// Byte extensions
/// </summary>
public static class ByteExtension
{
	/// <summary>
	/// This method will convert to an object from byte array
	/// </summary>
	/// <param name="byteArr"></param>
	/// <returns></returns>
	public static object? ToObject(this byte[]? byteArr)
	{
		ArgumentNullException.ThrowIfNull(byteArr);
		return JsonSerializer.Deserialize<object>(byteArr);
	}

	/// <summary>
	/// This method will convert byte array into desired model.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="byteArr"></param>
	/// <returns></returns>
	public static T? To<T>(this byte[]? byteArr)
	{
		ArgumentNullException.ThrowIfNull(byteArr);
		return JsonSerializer.Deserialize<T>(byteArr);
	}
}
