// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using System.Collections.Concurrent;

namespace UtilityVerse.Extensions;

public static class CollectionExtension
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? sources) => sources is null || !sources.Any();

    public static bool IsNullOrEmpty<T>(this IList<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this List<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this ICollection<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this IReadOnlyCollection<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this IReadOnlyList<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this IReadOnlySet<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this IOrderedEnumerable<T>? sources) => sources is null || !sources.Any();

    public static bool IsNullOrEmpty<T>(this IOrderedQueryable<T>? sources) => sources is null || !sources.Any();

    public static bool IsNullOrEmpty<T>(this ConcurrentBag<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this ConcurrentQueue<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this ConcurrentStack<T>? sources) => sources is null || sources.Count < 1;

    public static bool IsNullOrEmpty<T>(this IProducerConsumerCollection<T>? sources) => sources is null || sources.Count < 1;
}
