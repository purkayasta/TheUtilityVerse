// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System;
using System.Threading;
using System.Threading.Tasks;
using UtilityVerse.Contracts;

namespace UtilityVerse;

public sealed partial class Utility
{
    /// <summary>
    /// Executes a handler up to Count total attempts, returning immediately on success.
    /// The final exception is propagated and the supplied options are not modified.
    /// </summary>
    public static T AddRetry<T>(Func<T> handler, RetryOption retryOption = null)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        var (count, delay) = EvaluateOption(retryOption);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return handler();
            }
            catch when (attempt < count)
            {
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// Retries a synchronous handler with asynchronous waits between failed attempts.
    /// </summary>
    public static Task<T> AddRetryAsync<T>(Func<T> handler, RetryOption retryOption = null)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        var (count, delay) = EvaluateOption(retryOption);
        return ExecuteRetryOperation(() => Task.FromResult(handler()), count, delay);
    }

    /// <summary>
    /// Awaits and retries an asynchronous handler, including failures after an await.
    /// </summary>
    public static Task<T> AddRetryTaskAsync<T>(Func<Task<T>> handler, RetryOption retryOption = null)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        var (count, delay) = EvaluateOption(retryOption);
        return ExecuteRetryOperation(handler, count, delay);
    }

    private static async Task<T> ExecuteRetryOperation<T>(Func<Task<T>> handler, int count, TimeSpan delay)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await handler().ConfigureAwait(false);
            }
            catch when (attempt < count)
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }

    private static (int Count, TimeSpan Delay) EvaluateOption(RetryOption retryOption)
    {
        retryOption ??= new RetryOption();
        if (retryOption.Count < 1) throw new ArgumentOutOfRangeException(nameof(retryOption), "Count must be at least one.");
        if (retryOption.Delay < TimeSpan.Zero || retryOption.Delay.TotalMilliseconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(retryOption), "Delay must be between zero and Int32.MaxValue milliseconds.");
        return (retryOption.Count, retryOption.Delay);
    }
}
