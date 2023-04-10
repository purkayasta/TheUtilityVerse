namespace UtilityVerse.Extensions
{
    public static class DateOnlyExtensions
    {
        /// <summary>
        /// This method will help to determine is the date only instance is in between range or not.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static bool IsInBetween(this DateOnly? dt, DateOnly? startDate, DateOnly? endDate)
        {
            ArgumentNullException.ThrowIfNull(dt);
            ArgumentNullException.ThrowIfNull(startDate, nameof(startDate));
            ArgumentNullException.ThrowIfNull(endDate, nameof(endDate));

            return dt.Value >= startDate && dt.Value <= endDate;
        }
    }
}
