using UtilityVerse.Contracts;

namespace UtilityVerse
{
    public sealed partial class UtilityVerse
    {
        /// <summary>
        /// Retry Operation. This utility method will help you do retry on any method. It will retry on exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="retryOption"></param>
        /// <returns></returns>
        public static T? AddRetry<T>(Func<T> handler, RetryOption? retryOption = null)
        {
            retryOption ??= EvaluateOption(retryOption);

            T? genericReturn = default;

            return ExecuteRetryOperation(handler, retryOption, genericReturn).GetAwaiter().GetResult();

        }

        /// <summary>
        /// Retry Operation Async Version. This utility method will help you do retry on any method. It will retry on exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        /// <param name="retryOption"></param>
        /// <returns></returns>
        public static Task<T?> AddRetryAsync<T>(Func<T> handler, RetryOption? retryOption = null)
        {
            retryOption ??= EvaluateOption(retryOption);

            T? genericReturn = default;

            return ExecuteRetryOperation(handler, retryOption, genericReturn);
        }

        #region Helper Private Methods

        private static Task<T?> ExecuteRetryOperation<T>(Func<T> handler, RetryOption? retryOption, T? genericReturn)
        {
            while (retryOption!.Count > 0)
            {
                try
                {
                    genericReturn = handler();
                }
                catch (Exception retryException)
                {
                    Console.WriteLine("Exception Occurred - {handlerException}", retryException);

                    retryOption.Count--;

                    Console.WriteLine("Next Execution in: {time} second", retryOption.Delay!.Value.TotalSeconds);

                    Task.Delay(retryOption.Delay!.Value);
                }
            }

            return Task.FromResult(genericReturn);
        }

        private static RetryOption EvaluateOption(RetryOption? retryOption = null)
        {
            retryOption ??= new RetryOption();

            ArgumentNullException.ThrowIfNull(retryOption, nameof(RetryOption));

            if (retryOption.Count is null || retryOption.Count < 1 || retryOption.Delay is null)
                throw new ArgumentException($"Invalid {nameof(RetryOption)}");

            return retryOption;
        }

        #endregion
    }
}
