// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UtilityVerse.Copy;

/// <summary>Tracks reference identity during a generated deep copy. Use one instance per operation.</summary>
public sealed partial class CopyContext
{
    private const int MaxCopyDepth = 512;

    private const string SharedBackingStoreMessage =
        "A collection wrapper shares its backing store with another copied collection; use a custom copy implementation.";

    private readonly Dictionary<object, object> copies = new(ReferenceComparer.Instance);

    private readonly HashSet<object> constructing = new(ReferenceComparer.Instance);

    private List<IPendingCollection>? pendingCollections;

    private HashSet<object>? wrapperRoots;

    private int copyDepth;

    private bool copyFailed;

    private int completedDepth;

    /// <summary>Registers a destination before copying its children, allowing cycles.</summary>
    public void Register(object source, object destination)
    {
        if (wrapperRoots?.Contains(source) == true)
        {
            throw new NotSupportedException(SharedBackingStoreMessage);
        }

        copies.Add(source, destination);
    }

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

        // A value-typed argument has no source box to track. A reference-typed
        // argument can hold an existing box whose identity matters to the graph.
        var trackIdentity = !typeof(T).IsValueType;

        if (trackIdentity && copies.TryGetValue(source, out var existing))
        {
            return (T)existing;
        }

        if (source is IDeepCopyable copyable)
        {
            if (trackIdentity && copyable is IBoxedDeepCopyable boxed)
            {
                return (T)boxed.CopyBoxed(source, this);
            }

            if (trackIdentity && source.GetType().IsValueType)
            {
                return (T)CopyBoxWithoutEarlyRegistration(copyable);
            }

            return (T)copyable.Copy(this);
        }

        var type = source.GetType();

        if (type.IsPrimitive || type.IsEnum || type == typeof(Uri) ||
            source is string or decimal or DateTime or DateTimeOffset or TimeSpan or Guid or Version)
        {
            return source;
        }

        if (type.FullName is "System.DateOnly" or "System.TimeOnly" or "System.Half" or "System.Int128" or "System.UInt128" or
            "System.Index" or "System.Range" or "System.Numerics.BigInteger" or "System.Numerics.Complex" or "System.Text.Rune")
        {
            return source;
        }

        if (externalCopiers.TryGetValue(type, out var copier))
        {
            return (T)CopyExternalCore(source, copier, trackIdentity);
        }

        if (TryGetSequenceCopier(type, out var sequence, registerDefaultElements: true))
        {
            return (T)sequence!.CopyRuntime(this, source);
        }

        throw new NotSupportedException($"Type '{type}' needs generated deep-copy support.");
    }

    private object CopyBoxWithoutEarlyRegistration(IDeepCopyable source)
    {
        if (!constructing.Add(source))
        {
            throw new NotSupportedException(
                $"Cycles through boxed struct '{source.GetType()}' require a copier that can register the destination box before copying its children.");
        }

        try
        {
            var result = source.Copy(this);
            Register(source, result);

            return result;
        }
        finally
        {
            constructing.Remove(source);
        }
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

    /// <summary>Runs a typed value copier within a graph operation without creating a source box.</summary>
    public T CopyValue<T>(T source, Func<T, CopyContext, T> create) where T : struct
    {
        using var operation = BeginCopy();

        var result = create(source, this);

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

    /// <summary>Copies an array container while sharing its element values.</summary>
    /// <remarks>Use this path for element types whose values can be shared safely.</remarks>
    public Array? CopyArrayContainer(Array? source)
    {
        using var operation = BeginCopy();

        Array? result = null;

        if (source != null)
        {
            if (copies.TryGetValue(source, out var existing))
            {
                result = (Array)existing;
            }
            else
            {
                result = (Array)source.Clone();
                Register(source, result);
            }
        }

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

    /// <summary>Copies an array while preserving its runtime array type, rank, and bounds.</summary>
    /// <exception cref="ArgumentException">The array's elements are incompatible with <typeparamref name="T"/>.</exception>
    public Array? CopyShapedArray<T>(Array? source, Func<T, CopyContext, T> copyElement)
    {
        using var operation = BeginCopy();

        var result = CopyShapedArrayCore<T, DelegateElementCopier<T>>(source, new DelegateElementCopier<T>(copyElement));

        operation.Complete();

        return result;
    }

    private Array? CopyShapedArrayCore<T, TCopier>(Array? source, TCopier copyElement)
        where TCopier : struct, IElementCopier<T>
    {
        if (source is null)
        {
            return null;
        }

        if (source is T[] vector)
        {
            return CopyArrayCore<T, TCopier>(vector, copyElement);
        }

        if (!typeof(T).IsAssignableFrom(source.GetType().GetElementType()))
        {
            throw new ArgumentException($"The source array elements cannot be copied as '{typeof(T)}'.", nameof(source));
        }

        if (copies.TryGetValue(source, out var existing))
        {
            return (Array)existing;
        }

        var result = (Array)source.Clone();
        Register(source, result);

        if (source.LongLength == 0)
        {
            return result;
        }

        var rank = source.Rank;
        var indices = new int[rank];
        var lowerBounds = new int[rank];
        var upperBounds = new int[rank];

        for (var dimension = 0; dimension < rank; dimension++)
        {
            indices[dimension] = lowerBounds[dimension] = source.GetLowerBound(dimension);
            upperBounds[dimension] = source.GetUpperBound(dimension);
        }

        while (true)
        {
            var value = (T)source.GetValue(indices)!;
            result.SetValue(copyElement.Copy(value, this), indices);

            // Advance in row-major order without incrementing past an upper bound.
            var dimension = rank - 1;

            while (dimension >= 0 && indices[dimension] == upperBounds[dimension])
            {
                indices[dimension] = lowerBounds[dimension];
                dimension--;
            }

            if (dimension < 0)
            {
                return result;
            }

            indices[dimension]++;
        }
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
        type == typeof(SortedSet<T>) || type == typeof(LinkedList<T>) ||
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
                return CopySet<T, TCopier, HashSet<T>, HashSet<T>.Enumerator>(
                    set, new HashSet<T>(set.Comparer), set.GetEnumerator(), copyElement);

            case SortedSet<T> sorted:
                return CopySet<T, TCopier, SortedSet<T>, SortedSet<T>.Enumerator>(
                    sorted, new SortedSet<T>(sorted.Comparer), sorted.GetEnumerator(), copyElement);

            case List<T> sourceList:
                var listCopy = new List<T>(sourceList.Count);
                Register(sourceList, listCopy);

                foreach (var item in sourceList)
                {
                    listCopy.Add(copyElement.Copy(item, this));
                }

                return listCopy;

            case LinkedList<T>:
                var linked = new LinkedList<T>();
                Register(source, linked);

                foreach (var item in source)
                {
                    linked.AddLast(copyElement.Copy(item, this));
                }

                return linked;

            case Stack<T> sourceStack:
                var stack = new Stack<T>(sourceStack.Count);
                Register(sourceStack, stack);
                var topFirst = sourceStack.ToArray();

                for (var index = topFirst.Length - 1; index >= 0; index--)
                {
                    stack.Push(copyElement.Copy(topFirst[index], this));
                }

                return stack;

            case Queue<T> sourceQueue:
                var queue = new Queue<T>(sourceQueue.Count);
                Register(sourceQueue, queue);

                foreach (var item in sourceQueue)
                {
                    queue.Enqueue(copyElement.Copy(item, this));
                }

                return queue;

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

            case BindingList<T> sourceBinding:
                var binding = new BindingList<T>
                {
                    RaiseListChangedEvents = false,
                    AllowEdit = sourceBinding.AllowEdit,
                    AllowRemove = sourceBinding.AllowRemove,
                    AllowNew = sourceBinding.AllowNew
                };
                binding.RaiseListChangedEvents = sourceBinding.RaiseListChangedEvents;
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

        if (source is Collection<T> or ReadOnlyCollection<T>)
        {
            RegisterWrapperBacking<T>((System.Collections.IList)source);
        }

        Register(source, result);

        foreach (var item in source)
        {
            add(copyElement.Copy(item, this));
        }

        return result;
    }

    private void RegisterWrapperBacking<T>(System.Collections.IList source)
    {
        // The public synchronization root can detect shared storage without
        // assuming it is the wrapper's immediate backing list.
        var root = source.SyncRoot;

        if (root == null || (root.GetType() != typeof(List<T>) && root is not T[]) ||
            (source is Collection<T> && (source.IsFixedSize || source.IsReadOnly)))
        {
            throw new NotSupportedException("The collection wrapper has unsupported or unidentifiable backing storage; use a custom copy implementation.");
        }

        wrapperRoots ??= new HashSet<object>(ReferenceComparer.Instance);

        if (copies.ContainsKey(root) || !wrapperRoots.Add(root))
        {
            throw new NotSupportedException(SharedBackingStoreMessage);
        }
    }

    /// <summary>Copies a mutable map with a generated factory that preserves its comparer.</summary>
    /// <remarks>The factory must preserve the source's runtime type and any custom map state.</remarks>
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

    /// <summary>Copies a dictionary interface view using the source's supported concrete type and comparer.</summary>
    /// <remarks>The optional factory can supply a destination for additional runtime types.</remarks>
    public TMap? CopyDictionaryView<TMap, TKey, TValue>(
        TMap? source,
        Func<TKey, CopyContext, TKey> copyKey,
        Func<TValue, CopyContext, TValue> copyValue,
        Func<TMap, TMap>? createOther = null)
        where TMap : class, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        using var operation = BeginCopy();

        var result = CopyMapCore(
            source,
            createOther,
            static (copy, key, value) => ((IDictionary<TKey, TValue>)copy).Add(key, value),
            copyKey,
            copyValue,
            useStandardFactory: true);

        operation.Complete();

        return result;
    }

    private TMap? CopyMapCore<TMap, TKey, TValue>(
        TMap? source,
        Func<TMap, TMap>? create,
        Action<TMap, TKey, TValue> add,
        Func<TKey, CopyContext, TKey> copyKey,
        Func<TValue, CopyContext, TValue> copyValue,
        bool useStandardFactory = false)
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

        var sourceType = source.GetType();
        var result = useStandardFactory
            ? CreateStandardMap<TMap, TKey, TValue>(source, create)
            : create!(source);

        if (result?.GetType() != sourceType)
        {
            throw new NotSupportedException(
                $"Map runtime type '{sourceType}' requires a copy factory that preserves its runtime type and state.");
        }

        Register(source, result);

        var capacity = 0;

        if (source is ICollection<KeyValuePair<TKey, TValue>> collection)
        {
            capacity = collection.Count;

            if (capacity == 0)
            {
                return result;
            }
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

    private static TMap CreateStandardMap<TMap, TKey, TValue>(TMap source, Func<TMap, TMap>? createOther)
        where TMap : class, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        object result = source switch
        {
            Dictionary<TKey, TValue> dictionary => new Dictionary<TKey, TValue>(dictionary.Comparer),
            SortedDictionary<TKey, TValue> sorted => new SortedDictionary<TKey, TValue>(sorted.Comparer),
            SortedList<TKey, TValue> sorted => new SortedList<TKey, TValue>(sorted.Comparer),
            _ when createOther != null => createOther(source),
            _ => throw new NotSupportedException($"Unsupported dictionary runtime type '{source.GetType()}'.")
        };

        return result is TMap typed && result is IDictionary<TKey, TValue>
            ? typed
            : throw new NotSupportedException("A dictionary view copy requires a destination that implements the mutable dictionary interface.");
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

    private TSet CopySet<T, TCopier, TSet, TEnumerator>(
        TSet source,
        TSet result,
        TEnumerator enumerator,
        TCopier copyElement)
        where TCopier : struct, IElementCopier<T>
        where TSet : class, ISet<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        Register(source, result);

        if (source.Count == 0)
        {
            return result;
        }

        var entries = new T[source.Count];
        var index = 0;

        try
        {
            while (enumerator.MoveNext())
            {
                entries[index++] = copyElement.Copy(enumerator.Current, this);
            }
        }
        finally
        {
            enumerator.Dispose();
        }

        Schedule(new PendingSet<T, TSet>(result, entries));

        return result;
    }

    private void Schedule(IPendingCollection collection)
    {
        pendingCollections ??= new List<IPendingCollection>();
        pendingCollections.Add(collection);
    }

    private CopyOperation BeginCopy()
    {
        if (copyDepth >= MaxCopyDepth)
        {
            copyFailed = true;
            throw new NotSupportedException($"Deep copying exceeded the maximum depth of {MaxCopyDepth} nested operations.");
        }

        if ((copyDepth & 15) == 0)
        {
            EnsureCopyStack();
        }

        copyDepth++;

        return new CopyOperation(this, copyDepth);
    }

    private void EnsureCopyStack()
    {
        try
        {
            RuntimeHelpers.EnsureSufficientExecutionStack();
        }
        catch (InsufficientExecutionStackException error)
        {
            copyFailed = copyDepth != 0;
            throw new NotSupportedException("Deep copying exceeded the available stack depth.", error);
        }
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
        wrapperRoots?.Clear();
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

    private sealed class PendingSet<T, TSet> : IPendingCollection where TSet : class, ISet<T>
    {
        private readonly TSet destination;

        private readonly T[] entries;

        internal PendingSet(TSet destination, T[] entries)
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
                    SortedDictionary<TKey, TValue> dictionary => dictionary.ContainsKey(entry.Key),
                    SortedList<TKey, TValue> dictionary => dictionary.ContainsKey(entry.Key),
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

                case SortedDictionary<TKey, TValue> dictionary:
                    dictionary.Clear();
                    break;

                case SortedList<TKey, TValue> dictionary:
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
