// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UtilityVerse.Copy;

/// <summary>Tracks reference identity during a generated deep copy. Use one instance per operation.</summary>
public sealed partial class CopyContext
{
    private readonly Dictionary<object, object> copies = new(ReferenceComparer.Instance);

    private readonly HashSet<object> constructing = new(ReferenceComparer.Instance);

    private List<IPendingCollection>? pendingCollections;

    private int copyDepth;

    private bool copyFailed;

    private int completedDepth;

    /// <summary>Registers a destination before copying its children, allowing cycles.</summary>
    public void Register(object source, object destination) => copies.Add(source, destination);

    /// <summary>Copies an opted-in object or an immutable scalar, preserving runtime type and identity.</summary>
    public T Copy<T>(T source)
    {
        using var operation = BeginCopy();

        var result = CopyCore(source);

        operation.Complete();

        return result;
    }

    private T CopyCore<T>(T source)
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

    /// <summary>Runs a generated copy factory within a graph operation, including deferred collection population.</summary>
    /// <remarks>The factory must register reference-type destinations before copying their children.</remarks>
    public object CopyGenerated<T>(T source, Func<T, CopyContext, object> create)
    {
        using var operation = BeginCopy();

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (create is null)
        {
            throw new ArgumentNullException(nameof(create));
        }

        var result = copies.TryGetValue(source, out var existing) ? existing : create(source, this);

        operation.Complete();

        return result;
    }

    /// <summary>Copies an array, registering its container before copying the elements.</summary>
    public T[]? CopyArray<T>(T[]? source, Func<T, CopyContext, T> copyElement)
    {
        using var operation = BeginCopy();

        var result = CopyArrayCore(source, copyElement);

        operation.Complete();

        return result;
    }

    private T[]? CopyArrayCore<T>(T[]? source, Func<T, CopyContext, T> copyElement) =>
        CopyArrayCore<T, DelegateElementCopier<T>>(source, new DelegateElementCopier<T>(copyElement));

    private T[]? CopyArrayCore<T, TCopier>(T[]? source, TCopier copyElement)
        where TCopier : struct, IElementCopier<T>
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
            result[index] = copyElement.Copy(source[index], this);
        }

        return result;
    }

    /// <summary>Copies standard sequences while preserving their concrete collection and comparer.</summary>
    public IEnumerable<T>? CopySequence<T>(IEnumerable<T>? source, Func<T, CopyContext, T> copyElement)
    {
        using var operation = BeginCopy();

        var result = CopySequenceCore(source, copyElement);

        operation.Complete();

        return result;
    }

    private IEnumerable<T>? CopySequenceCore<T>(IEnumerable<T>? source, Func<T, CopyContext, T> copyElement)
    {
        if (source == null)
        {
            return null;
        }

        if (copies.TryGetValue(source, out var existing))
        {
            return (IEnumerable<T>)existing;
        }

        var runtimeType = source.GetType();

        if (source is not T[] && !IsExactSequenceType<T>(runtimeType))
        {
            if (TryGetSequenceCopier(runtimeType, out var copier))
            {
                return copier!.Copy(this, source, copyElement);
            }

            if (source is System.Collections.ICollection || source is ICollection<T> || source is IReadOnlyCollection<T>)
            {
                throw new NotSupportedException($"Collection runtime type '{runtimeType}' requires a custom copy implementation.");
            }
        }

        return CopySequenceCore<T, DelegateElementCopier<T>>(source, new DelegateElementCopier<T>(copyElement));
    }

    private static bool IsExactSequenceType<T>(Type type) =>
        type == typeof(List<T>) || type == typeof(HashSet<T>) ||
        type == typeof(Queue<T>) || type == typeof(Stack<T>) ||
        type == typeof(Collection<T>) || type == typeof(ReadOnlyCollection<T>) ||
        type == typeof(ObservableCollection<T>) || type == typeof(BindingList<T>);

    // The entry point has checked the identity cache and runtime shape already.
    // Covariant dispatch reaches this core with the actual closed element type.
    private IEnumerable<T> CopySequenceCore<T, TCopier>(IEnumerable<T> source, TCopier copyElement)
        where TCopier : struct, IElementCopier<T>
    {
        IEnumerable<T> result;
        Action<T> add;

        switch (source)
        {

            case T[] array:
                return CopyArrayCore<T, TCopier>(array, copyElement)!;

            case HashSet<T> set:
                return CopySet<T, TCopier>(set, copyElement);

            case Stack<T>:
                var stack = new Stack<T>();
                Register(source, stack);
                foreach (var item in source.Reverse())
                {
                    stack.Push(copyElement.Copy(item, this));
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
            add(copyElement.Copy(item, this));
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
        using var operation = BeginCopy();

        var result = CopyMapCore(source, create, add, copyKey, copyValue);

        operation.Complete();

        return result;
    }

    private TMap? CopyMapCore<TMap, TKey, TValue>(
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

        var capacity = source is ICollection<KeyValuePair<TKey, TValue>> collection ? collection.Count : 0;

        if (source is ICollection<KeyValuePair<TKey, TValue>> { Count: 0 })
        {
            return result;
        }

        var entries = new List<KeyValuePair<TKey, TValue>>(capacity);

        foreach (var pair in source)
        {
            var key = copyKey(pair.Key, this);
            var value = copyValue(pair.Value, this);

            entries.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        if (entries.Count != 0)
        {
            Schedule(new PendingMap<TMap, TKey, TValue>(result, entries, add));
        }

        return result;
    }

    /// <summary>Rebuilds an immutable container. Cycles through immutable containers are rejected.</summary>
    public T? CopyImmutable<T>(T? source, Func<T, T> create) where T : class
    {
        using var operation = BeginCopy();

        var result = CopyImmutableCore(source, create);

        operation.Complete();

        return result;
    }

    private T? CopyImmutableCore<T>(T? source, Func<T, T> create) where T : class
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

    private HashSet<T> CopySet<T, TCopier>(HashSet<T> source, TCopier copyElement)
        where TCopier : struct, IElementCopier<T>
    {
        var result = new HashSet<T>(source.Comparer);
        Register(source, result);

        if (source.Count == 0)
        {
            return result;
        }

        var entries = new T[source.Count];
        var index = 0;

        foreach (var item in source)
        {
            entries[index++] = copyElement.Copy(item, this);
        }

        Schedule(new PendingSet<T>(result, entries));

        return result;
    }

    private void Schedule(IPendingCollection collection)
    {
        pendingCollections ??= new List<IPendingCollection>();
        pendingCollections.Add(collection);
    }

    private CopyOperation BeginCopy()
    {
        copyDepth++;

        return new CopyOperation(this, copyDepth);
    }

    private void CompleteCopy()
    {
        if (copyFailed)
        {
            throw new InvalidOperationException("A nested copy failed; the graph cannot be completed.");
        }

        if (copyDepth != 1 || pendingCollections == null)
        {
            return;
        }

        // Keys may refer to ancestors whose constructors have not returned yet.
        // Only the outermost operation can insert them after all stored state is hydrated.
        for (var index = 0; index < pendingCollections.Count; index++)
        {
            pendingCollections[index].Populate();
        }

        // Another collection's final contents can affect a key's hash. Rebuild
        // invalid buckets in the same destination after every container is populated.
        var rebuilt = false;

        for (var index = 0; index < pendingCollections.Count; index++)
        {
            var collection = pendingCollections[index];

            if (!collection.IsValid())
            {
                collection.Rebuild();
                rebuilt = true;
            }
        }

        if (rebuilt)
        {
            for (var index = 0; index < pendingCollections.Count; index++)
            {
                if (!pendingCollections[index].IsValid())
                {
                    throw new NotSupportedException("Key or element hash/equality depends on unstable collection population; use a custom copy implementation.");
                }
            }
        }

        pendingCollections.Clear();
    }

    private void EndCopy(bool succeeded)
    {
        copyFailed |= !succeeded;
        completedDepth = 0;
        copyDepth--;

        if (copyDepth != 0 || !copyFailed)
        {
            return;
        }

        // Failed operations must not leave partially hydrated destinations cached.
        copies.Clear();
        constructing.Clear();
        pendingCollections?.Clear();
        copyFailed = false;
    }

    private readonly struct CopyOperation : IDisposable
    {
        private readonly CopyContext context;

        private readonly int depth;

        internal CopyOperation(CopyContext context, int depth)
        {
            this.context = context;
            this.depth = depth;
        }

        internal void Complete()
        {
            context.CompleteCopy();
            context.completedDepth = depth;
        }

        public void Dispose()
        {
            // Scopes are internal and strictly nested. A parent can complete only
            // after its children dispose, so one depth marker records success
            // without mutable struct state or a per-call heap allocation.
            context.EndCopy(context.completedDepth == depth);
        }
    }

    private interface IPendingCollection
    {
        void Populate();

        bool IsValid();

        void Rebuild();
    }

    private sealed class PendingSet<T> : IPendingCollection
    {
        private readonly HashSet<T> destination;

        private readonly T[] entries;

        internal PendingSet(HashSet<T> destination, T[] entries)
        {
            this.destination = destination;
            this.entries = entries;
        }

        public void Populate()
        {
            foreach (var entry in entries)
            {
                if (!destination.Add(entry))
                {
                    throw new InvalidOperationException("Deep copying produced duplicate set elements.");
                }
            }
        }

        public bool IsValid()
        {
            foreach (var entry in entries)
            {
                if (!destination.Contains(entry))
                {
                    return false;
                }
            }

            return true;
        }

        public void Rebuild()
        {
            destination.Clear();
            Populate();
        }
    }

    private sealed class PendingMap<TMap, TKey, TValue> : IPendingCollection
        where TMap : class, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly TMap destination;

        private readonly List<KeyValuePair<TKey, TValue>> entries;

        private readonly Action<TMap, TKey, TValue> add;

        internal PendingMap(TMap destination, List<KeyValuePair<TKey, TValue>> entries, Action<TMap, TKey, TValue> add)
        {
            this.destination = destination;
            this.entries = entries;
            this.add = add;
        }

        public void Populate()
        {
            foreach (var entry in entries)
            {
                add(destination, entry.Key, entry.Value);
            }
        }

        public bool IsValid()
        {
            foreach (var entry in entries)
            {
                var found = destination switch
                {
                    Dictionary<TKey, TValue> dictionary => dictionary.ContainsKey(entry.Key),
                    ConcurrentDictionary<TKey, TValue> dictionary => dictionary.ContainsKey(entry.Key),
                    _ => true
                };

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        public void Rebuild()
        {
            switch (destination)
            {
                case Dictionary<TKey, TValue> dictionary:
                    dictionary.Clear();
                    break;

                case ConcurrentDictionary<TKey, TValue> dictionary:
                    dictionary.Clear();
                    break;

                default:
                    throw new NotSupportedException("This map does not support rebuilding hash buckets.");
            }

            Populate();
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
