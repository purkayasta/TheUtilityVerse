namespace UtilityVerse.Helpers;

internal static class Utils
{
    internal static bool IsStruct(Type type) => type.IsValueType && !type.IsPrimitive && !type.IsEnum;
}
