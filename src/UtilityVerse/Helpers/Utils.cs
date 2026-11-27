namespace UtilityVerse.Helpers;

internal static class Utils
{
#if NETCOREAPP3_1_OR_GREATER
    internal static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { IncludeFields = true };
#endif

    internal static bool IsStruct(Type type) => type.IsValueType && !type.IsPrimitive && !type.IsEnum;
}
