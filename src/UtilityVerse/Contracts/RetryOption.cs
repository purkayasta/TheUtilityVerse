namespace UtilityVerse.Contracts
{
    public class RetryOption
    {

        /// <summary>
        /// When it will retry again.
        /// </summary>
        public TimeSpan? Delay { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Retry Count. How many time it will try?
        /// </summary>
        public int? Count { get; set; } = 1;
    }
}
