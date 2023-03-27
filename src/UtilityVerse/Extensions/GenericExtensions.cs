using System.Text;
using System.Text.Json;

namespace UtilityVerse.Extensions
{
    public static class GenericExtensions
    {
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
}
