// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System;
using System.Threading.Tasks;
using UtilityVerse.Contracts;
using UtilityVerse.Helpers;

namespace UtilityVerse;

public sealed partial class Utility
{
    /// <summary>
    /// The method will retry on exception.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    /// <param name="retryOption"></param>
    /// <returns></returns>
    public static T AddRetry<T>(Func<T> handler, RetryOption retryOption = null)
    {
        UtilityVerseException.ThrowIfNull(data: handler, paramName: "Function");

        retryOption ??= EvaluateOption(retryOption);

        T genericReturn = default;
        return ExecuteRetryOperation(handler, retryOption, genericReturn).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Retry Operation Async Version. 
    /// This utility method will help you do retry on any method. It will retry on exception.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="handler"></param>
    /// <param name="retryOption"></param>
    /// <returns></returns>
    public static Task<T> AddRetryAsync<T>(Func<T> handler, RetryOption retryOption = null)
    {
        UtilityVerseException.ThrowIfNull(handler, "Function");

        retryOption ??= EvaluateOption(retryOption);

        T genericReturn = default;
        return ExecuteRetryOperation(handler, retryOption, genericReturn);
    }

    #region Helper Private Methods

    private static Task<T> ExecuteRetryOperation<T>(Func<T> handler, RetryOption retryOption, T genericReturn)
    {
        while (retryOption!.Count > 0)
        {
            try
            {
                genericReturn = handler();
            }
            catch (Exception retryException)
            {
                Console.WriteLine($"Exception Occurred: - {retryException.Message}");

                retryOption.Count--;

                Console.WriteLine($"Next Execution in: {retryOption.Delay!.TotalSeconds} seconds");

                Task.Delay(retryOption.Delay!);
            }
        }

        return Task.FromResult(genericReturn);
    }

    private static RetryOption EvaluateOption(RetryOption retryOption = null)
    {
        retryOption ??= new RetryOption();

        UtilityVerseException.ThrowIfNull(data: retryOption, paramName: nameof(RetryOption));

        if (retryOption.Count < 1)
            UtilityVerseException.Throw($"Invalid {nameof(RetryOption)}");

        return retryOption;
    }

    #endregion
}
