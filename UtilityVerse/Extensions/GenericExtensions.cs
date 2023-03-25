using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace UtilityVerse.Extensions
{
    public static class GenericExtensions
    {
        public static T? To<T>(this byte[]? byteArr)
        {
            ArgumentNullException.ThrowIfNull(byteArr);
            return JsonSerializer.Deserialize<T>(byteArr);
        }

        public static byte[] ToByteArray<T>(this T? instance)
        {
            ArgumentNullException.ThrowIfNull(instance);
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize<T>(instance));
        }
    }
}
