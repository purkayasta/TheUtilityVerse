// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy;

internal static class CompilerDepth
{
    internal static bool IsSupported(INamedTypeSymbol model, System.Threading.CancellationToken cancellationToken)
    {
        var active = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        return Visit(model, 1, active, cancellationToken);
    }

    private static bool Visit(ITypeSymbol type, int depth, HashSet<ITypeSymbol> active, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (active.Contains(type))
        {
            return true;
        }

        if (depth > 100)
        {
            return false;
        }

        active.Add(type);

        try
        {
            if (type is IArrayTypeSymbol array)
            {
                return Visit(array.ElementType, depth + 1, active, cancellationToken);
            }

            if (type is not INamedTypeSymbol named)
            {
                return true;
            }

            foreach (var argument in named.TypeArguments)
            {
                if (!Visit(argument, depth + 1, active, cancellationToken))
                {
                    return false;
                }
            }

            if (named.IsValueType || Helper.HasDeepCopyOptIn(named))
            {
                foreach (var member in Helper.GetCopiedState(named))
                {
                    if (Helper.GetCopyBehavior(member) == CopyBehavior.Copy &&
                        !Visit(Helper.StateType(member), depth + 1, active, cancellationToken))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        finally
        {
            active.Remove(type);
        }
    }
}
