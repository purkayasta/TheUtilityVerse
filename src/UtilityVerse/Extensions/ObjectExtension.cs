using System.Text;
using System.Text.Json;

namespace UtilityVerse.Extensions
{
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
        /// This method will convert an readonly span byte array into an object.
        /// </summary>
        /// <param name="byteArr"></param>
        /// <returns></returns>
        public static object? ToObject(this ReadOnlySpan<byte> byteArr) 
            => JsonSerializer.Deserialize<object>(byteArr);
    }
}
