// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


namespace UtilityVerse.Extensions
{
    public static class IntExtension
    {
        /// <summary>
        /// This method will help to determine that source number is in between or not.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsInBetween(this int? source, int? min, int? max)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(min, nameof(min));
            ArgumentNullException.ThrowIfNull(max, nameof(max));

            return IsInBetween(source.Value, min.Value, max.Value);
        }

        /// <summary>
        /// This method will help to determine that source number is in between or not.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static bool IsInBetween(this int source, int min, int max)
        {
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(min) + " cannot be greater than " + nameof(max));

            return source >= min && source <= max;
        }


        public static int ToInt(this bool value) => value ? 1 : 0;
    }
}
