namespace UtilityVerse.Helpers;

internal static class Variables
{
    internal const string IpAddressRegex = @"\b(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";
    internal const string EmailAddressRegex = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
    internal const string InvalidCharacterRegex = @"[^a-z0-9\s-]";
}



internal class StaticMessages
{
    internal const string ContactDeveloperError = "Error Occurred, Contact Developer!";
    internal const string NullOrEmptyError = "Field value is null or empty";
    internal const string NullOrEmptyErrorWithParam = "{} value is null or empty";
}