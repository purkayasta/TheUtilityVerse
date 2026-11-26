// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UtilityVerse.Copy;

/// <summary>Tracks reference identity during a generated deep copy. Use one instance per operation.</summary>
public sealed class CopyContext
{
    private readonly Dictionary<object, object> copies = new(ReferenceComparer.Instance);

    private readonly HashSet<object> constructing = new(ReferenceComparer.Instance);

    /// <summary>Registers a destination before copying its children, allowing cycles.</summary>
    public void Register(object source, object destination) => copies.Add(source, destination);

    /// <summary>Copies an opted-in object or an immutable scalar, preserving runtime type and identity.</summary>
    public T Copy<T>(T source)
    {
        if (source is null)
        {
            return source;
        }

        if (copies.TryGetValue(source, out var existing))
        {
            return (T)existing;
        }

        if (source is IDeepCopyable copyable)
        {
            return (T)copyable.Copy(this);
        }

        var type = source.GetType();

        if (type.IsPrimitive || type.IsEnum || source is string or decimal or DateTime or DateTimeOffset or TimeSpan or Guid or Uri or Version)
        {
            return source;
        }

        if (type.FullName is "System.DateOnly" or "System.TimeOnly" or "System.Half" or "System.Int128" or "System.UInt128" or
            "System.Index" or "System.Range" or "System.Numerics.BigInteger" or "System.Numerics.Complex")
        {
            return source;
        }

        throw new NotSupportedException($"Type '{type}' needs generated deep-copy support.");
    }

    /// <summary>Copies an array, registering its container before copying the elements.</summary>
    public T[]? CopyArray<T>(T[]? source, Func<T, CopyContext, T> copyElement)
    {
        if (source is null)
        {
            return null;
        }

        if (copies.TryGetValue(source, out var existing))
        {
            return (T[])existing;
        }

        var result = (T[])source.Clone();
        Register(source, result);

        for (var index = 0; index < source.Length; index++)
        {
            result[index] = copyElement(source[index], this);
        }

        return result;
    }

    /// <summary>Copies standard sequences while preserving their concrete collection and comparer.</summary>
    public IEnumerable<T>? CopySequence<T>(IEnumerable<T>? source, Func<T, CopyContext, T> copyElement)
    {
        if (source is null)
        {
            return null;
        }

        if (copies.TryGetValue(source, out var existing))
        {
            return (IEnumerable<T>)existing;
        }

        var type = source.GetType();

        if ((source is ICollection<T> || source is Queue<T> || source is Stack<T>) &&
            type != typeof(List<T>) && type != typeof(HashSet<T>) && type != typeof(Queue<T>) && type != typeof(Stack<T>) &&
            type != typeof(ReadOnlyCollection<T>) && type != typeof(ObservableCollection<T>) && type != typeof(BindingList<T>) &&
            type != typeof(Collection<T>) && source is not T[])
        {
            throw new NotSupportedException($"Collection runtime type '{type}' requires a custom copy implementation.");
        }

        IEnumerable<T> result;
        Action<T> add;

        switch (source)
        {

            case T[] array:
                return CopyArray(array, copyElement);

            case HashSet<T> set:
                var setCopy = new HashSet<T>(set.Comparer);
                result = setCopy;
                add = item =>
                {
                    if (!setCopy.Add(item))
                    {
                        throw new InvalidOperationException("Deep copying produced duplicate set elements.");
                    }
                };
                break;

            case Stack<T>:
                var stack = new Stack<T>();
                Register(source, stack);
                foreach (var item in source.Reverse())
                {
                    stack.Push(copyElement(item, this));
                }

                return stack;

            case Queue<T>:
                var queue = new Queue<T>();
                result = queue;
                add = queue.Enqueue;
                break;

            case ReadOnlyCollection<T>:
                var backing = new List<T>();
                result = new ReadOnlyCollection<T>(backing);
                add = backing.Add;
                break;

            case ObservableCollection<T>:
                var observable = new ObservableCollection<T>();
                result = observable;
                add = observable.Add;
                break;

            case BindingList<T>:
                var binding = new BindingList<T>();
                result = binding;
                add = binding.Add;
                break;

            case Collection<T>:
                var collection = new Collection<T>();
                result = collection;
                add = collection.Add;
                break;

            default:
                var list = new List<T>();
                result = list;
                add = list.Add;
                break;
        }

        Register(source, result);

        foreach (var item in source)
        {
            add(copyElement(item, this));
        }

        return result;
    }

    /// <summary>Copies a mutable map with a generated factory that preserves its comparer.</summary>
    public TMap? CopyMap<TMap, TKey, TValue>(
        TMap? source,
        Func<TMap, TMap> create,
        Action<TMap, TKey, TValue> add,
        Func<TKey, CopyContext, TKey> copyKey,
        Func<TValue, CopyContext, TValue> copyValue)
        where TMap : class, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        if (source is null)
        {
            return null;
        }

        if (copies.TryGetValue(source, out var existing))
        {
            return (TMap)existing;
        }

        var result = create(source);
        Register(source, result);

        foreach (var pair in source)
        {
            add(result, copyKey(pair.Key, this), copyValue(pair.Value, this));
        }

        return result;
    }

    /// <summary>Rebuilds an immutable container. Cycles through immutable containers are rejected.</summary>
    public T? CopyImmutable<T>(T? source, Func<T, T> create) where T : class
    {
        if (source is null)
        {
            return null;
        }

        if (copies.TryGetValue(source, out var existing))
        {
            return (T)existing;
        }

        if (!constructing.Add(source))
        {
            throw new NotSupportedException("Cycles through immutable containers cannot be reconstructed.");
        }

        try
        {
            var result = create(source);
            Register(source, result);

            return result;
        }
        finally
        {
            constructing.Remove(source);
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
