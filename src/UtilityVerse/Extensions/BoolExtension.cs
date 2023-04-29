// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


namespace UtilityVerse.Extensions
{
    public static class BoolExtension
    {
        /// <summary>
        /// This method will convert any boolean string value to boolean
        /// </summary>
        /// <param name="value">"YES", "1", "true"</param>
        /// <returns></returns>
        public static bool ToBoolean(this string? value)
        {
            if (value.IsNullOrEmptyOrWhiteSpace()) return false;

            value = value!.ToLower();

            if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("1", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase))
                return true;

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("0", StringComparison.OrdinalIgnoreCase) || value.Equals("no", StringComparison.OrdinalIgnoreCase))
                return false;

            return Convert.ToBoolean(value);
        }
    }
}
