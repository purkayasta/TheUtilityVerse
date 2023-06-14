// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Text;
using System.Text.Json;

namespace UtilityVerse.Extensions;

/// <summary>
/// Generic Extension
/// </summary>
public static class GenericExtension
{
	/// <summary>
	/// This method will convert poco model into byte array.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="instance"></param>
	/// <returns></returns>
	public static byte[] ToByteArray<T>(this T? instance)
	{
		ArgumentNullException.ThrowIfNull(instance);
		return Encoding.UTF8.GetBytes(JsonSerializer.Serialize<T>(instance));
	}
}
