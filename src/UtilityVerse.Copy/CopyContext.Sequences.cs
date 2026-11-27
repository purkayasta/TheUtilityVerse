// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UtilityVerse.Copy;

public sealed partial class CopyContext
{
    // Ephemeron caches allow collection types from collectible assemblies to unload.
    private static readonly ConditionalWeakTable<Type, ISequenceCopier> sequenceElementCopiers = new();

    private static readonly ConditionalWeakTable<Type, SequenceRuntimeType> sequenceRuntimeTypes = new();

    static CopyContext()
    {
        // These are the supported immutable reference scalars. Their element
        // copiers also cover broad object/interface views without user registration.
        RegisterSequenceType<string>();
        RegisterSequenceType<Uri>();
        RegisterSequenceType<Version>();
    }

    /// <summary>Registers typed copiers for supported runtime sequences whose element type is T.</summary>
    /// <remarks>Generated code registers visible model and declared sequence element types before copying a graph.</remarks>
    public static void RegisterSequenceType<T>() => _ = SequenceCopier<T>.Instance;

    private static bool TryGetSequenceCopier(Type runtimeType, out ISequenceCopier? copier)
    {
        var shape = sequenceRuntimeTypes.GetValue(runtimeType, static type => new SequenceRuntimeType(type));

        if (shape.ElementType != null)
        {
            if (sequenceElementCopiers.TryGetValue(shape.ElementType, out copier))
            {
                return true;
            }

            throw new NotSupportedException(
                $"Runtime sequence '{runtimeType}' requires CopyContext.RegisterSequenceType<{shape.ElementType}>() before copying.");
        }

        copier = null;

        return false;
    }

    private interface IElementCopier<T>
    {
        T Copy(T value, CopyContext context);
    }

    private readonly struct DelegateElementCopier<T> : IElementCopier<T>
    {
        private readonly Func<T, CopyContext, T> copy;

        internal DelegateElementCopier(Func<T, CopyContext, T> copy)
        {
            this.copy = copy;
        }

        public T Copy(T value, CopyContext context) => copy(value, context);
    }

    private readonly struct CovariantElementCopier<TActual, TView> : IElementCopier<TActual>
    {
        private readonly Func<TView, CopyContext, TView> copy;

        internal CovariantElementCopier(Func<TView, CopyContext, TView> copy)
        {
            this.copy = copy;
        }

        public TActual Copy(TActual value, CopyContext context) =>
            (TActual)(object?)copy((TView)(object?)value!, context)!;
    }

    private interface ISequenceCopier
    {
        IEnumerable<TView> Copy<TView>(
            CopyContext context,
            IEnumerable<TView> source,
            Func<TView, CopyContext, TView> copyElement);
    }

    private sealed class SequenceCopier<TActual> : ISequenceCopier
    {
        internal static readonly SequenceCopier<TActual> Instance = new();

        static SequenceCopier()
        {
            sequenceElementCopiers.Add(typeof(TActual), Instance);
        }

        public IEnumerable<TView> Copy<TView>(
            CopyContext context,
            IEnumerable<TView> source,
            Func<TView, CopyContext, TView> copyElement)
        {
            // This path is reached only when generic sequence covariance changes
            // the reference element type. Value-type and exact-type paths stay typed.
            var result = context.CopySequenceCore<TActual, CovariantElementCopier<TActual, TView>>(
                (IEnumerable<TActual>)source,
                new CovariantElementCopier<TActual, TView>(copyElement));

            return (IEnumerable<TView>)result;
        }
    }

    private sealed class SequenceRuntimeType
    {
        internal Type? ElementType { get; }

        internal SequenceRuntimeType(Type type)
        {
            if (!type.IsGenericType)
            {
                return;
            }

            // Inspect collection type identity once, never model members. All
            // executable copiers are closed generic methods referenced by registration.
            var definition = type.GetGenericTypeDefinition();

            if (definition == typeof(List<>) || definition == typeof(HashSet<>) ||
                definition == typeof(Queue<>) || definition == typeof(Stack<>) ||
                definition == typeof(Collection<>) || definition == typeof(ReadOnlyCollection<>) ||
                definition == typeof(ObservableCollection<>) || definition == typeof(BindingList<>))
            {
                ElementType = type.GetGenericArguments()[0];
            }
        }
    }
}
