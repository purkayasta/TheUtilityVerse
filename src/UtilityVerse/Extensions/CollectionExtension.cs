// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace UtilityVerse.Extensions;

/// <summary>
/// Collection Extension
/// </summary>
public static class CollectionExtension
{
    /// <summary>
    /// If the collection is null or empty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> sources) => sources is null || !sources.Any();

    /// <summary>
    /// If the IList is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IList<T> sources) => sources is null || sources.Count < 1;

    /// <summary>
    /// If the list is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this List<T> sources) => sources is null || sources.Count < 1;

    /// <summary>
    /// If the collection is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this ICollection<T> sources) => sources is null || sources.Count < 1;

    /// <summary>
    /// If the readonly collection is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T> sources) => sources is null || sources.Count < 1;

    /// <summary>
    /// If the readonly list is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IReadOnlyList<T> sources) => sources is null || sources.Count < 1;

#if NETCOREAPP3_1_OR_GREATER
    /// <summary>
    /// If the readonly set is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IReadOnlySet<T> sources) => sources is null || sources.Count < 1;

#endif

    /// <summary>
    /// If the IOrderedEnumerable is null or empty
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IOrderedEnumerable<T> sources) => sources is null || !sources.Any();

    /// <summary>
    /// If the IOrderedQueryable is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IOrderedQueryable<T> sources) => sources is null || !sources.Any();

    /// <summary>
    /// If the ConcurrentBag is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this ConcurrentBag<T> sources) => sources is null || sources.IsEmpty;

    /// <summary>
    /// If the concurrentQueue is empty or not.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this ConcurrentQueue<T> sources) => sources is null || sources.IsEmpty;

    /// <summary>
    /// If the ConcurrentStack is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this ConcurrentStack<T> sources) => sources is null || sources.IsEmpty;

    /// <summary>
    /// If the IProducerConsumerCollection is null or empty.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sources"></param>
    /// <returns>bool</returns>
    public static bool IsNullOrEmpty<T>(this IProducerConsumerCollection<T> sources) => sources is null || sources.Count < 1;
}
