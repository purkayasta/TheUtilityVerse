// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Text;
using System.Text.Json;

namespace UtilityVerse.Extensions;

/// <summary>
/// Object Extension
/// </summary>
public static class ObjectExtension
{
	/// <summary>
	/// This method will convert any object into a byte array.
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public static byte[] ToByteArray(this object? obj)
	{
		ArgumentNullException.ThrowIfNull(obj);
		return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj));
	}
}
