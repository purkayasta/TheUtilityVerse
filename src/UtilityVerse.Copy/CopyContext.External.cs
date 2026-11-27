// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;

namespace UtilityVerse.Copy;

public sealed partial class CopyContext
{
    private static readonly ConditionalWeakTable<Type, IExternalCopier> externalCopiers = new();

    /// <summary>Registers a copier for one exact runtime type across copy contexts.</summary>
    /// <remarks>Reference copiers can register a destination before copying children to support cycles.</remarks>
    public static void RegisterExternalCopier<T>(Func<T, CopyContext, T> copy)
    {
        if (copy == null)
        {
            throw new ArgumentNullException(nameof(copy));
        }

        externalCopiers.Add(typeof(T), new ExternalCopier<T>(copy));
    }

    /// <summary>Copies a value using its registered external copier.</summary>
    public T CopyExternal<T>(T source)
    {
        using var operation = BeginCopy();

        var result = source;

        if (source is not null)
        {
            if (!externalCopiers.TryGetValue(source.GetType(), out var copier))
            {
                throw new NotSupportedException($"Type '{source.GetType()}' requires an external copier registration.");
            }

            result = (T)CopyExternalCore(source, copier, !typeof(T).IsValueType);
        }

        operation.Complete();

        return result;
    }

    private object CopyExternalCore(object source, IExternalCopier copier, bool trackIdentity)
    {
        if (trackIdentity && copies.TryGetValue(source, out var existing))
        {
            return existing;
        }

        if (trackIdentity && !constructing.Add(source))
        {
            throw new NotSupportedException($"An external copier must register its destination before a cycle through '{source.GetType()}'.");
        }

        try
        {
            var result = copier.Copy(source, this);

            if (result == null || result.GetType() != source.GetType() || ReferenceEquals(source, result))
            {
                throw new NotSupportedException($"The external copier for '{source.GetType()}' must return a new instance of the same runtime type.");
            }

            if (trackIdentity)
            {
                if (copies.TryGetValue(source, out var registered))
                {
                    if (!ReferenceEquals(result, registered))
                    {
                        throw new InvalidOperationException("The external copier returned a different instance from its registered destination.");
                    }
                }
                else
                {
                    Register(source, result);
                }
            }

            return result;
        }
        finally
        {
            if (trackIdentity)
            {
                constructing.Remove(source);
            }
        }
    }

    private interface IExternalCopier
    {
        object? Copy(object source, CopyContext context);
    }

    private sealed class ExternalCopier<T> : IExternalCopier
    {
        private readonly Func<T, CopyContext, T> copy;

        internal ExternalCopier(Func<T, CopyContext, T> copy)
        {
            this.copy = copy;
        }

        public object? Copy(object source, CopyContext context) => copy((T)source, context);
    }
}
