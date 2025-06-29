/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy;

internal static class Helper
{
    internal static bool IsPrimitive(ITypeSymbol symbol) =>
        symbol.IsValueType || symbol.SpecialType == SpecialType.System_String;

    internal static bool IsTrulyPrimitive(ITypeSymbol symbol)
    {
        return symbol.SpecialType switch
        {
            SpecialType.System_Boolean or
                SpecialType.System_Byte or
                SpecialType.System_SByte or
                SpecialType.System_Int16 or
                SpecialType.System_UInt16 or
                SpecialType.System_Int32 or
                SpecialType.System_UInt32 or
                SpecialType.System_Int64 or
                SpecialType.System_UInt64 or
                SpecialType.System_Single or
                SpecialType.System_Double or
                SpecialType.System_Char or
                SpecialType.System_String => true,
            _ => false
        };
    }

    internal static bool IsGenericCollection(string? originalDef, out string? kind)
    {
        kind = originalDef switch
        {
            // List-like collections
            "System.Collections.Generic.List<T>" => "List",
            "System.Collections.Generic.IList<T>" => "List",
            "System.Collections.ObjectModel.Collection<T>" => "List",
            "System.Collections.ObjectModel.ReadOnlyCollection<T>" => "List",
            "System.Collections.ObjectModel.ObservableCollection<T>" => "List",
            "System.ComponentModel.BindingList<T>" => "List",
            "System.Collections.Immutable.ImmutableList<T>" => "List",
            "System.Collections.Generic.ReadOnlyList<T>" => "List", // custom/unofficial

            // Enumerable-like collections
            "System.Collections.Generic.IEnumerable<T>" => "Enumerable",
            "System.Collections.Generic.ICollection<T>" => "Enumerable",
            "System.Collections.Generic.IReadOnlyCollection<T>" => "Enumerable",
            "System.Collections.Generic.IReadOnlyList<T>" => "Enumerable",

            // Hash-based collections
            "System.Collections.Generic.HashSet<T>" => "HashSet",

            // Dictionary-like collections
            "System.Collections.Generic.Dictionary<TKey, TValue>" => "Dictionary",
            "System.Collections.Frozen.FrozenDictionary<TKey, TValue>" => "Dictionary",
            "System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>" => "Dictionary",

            // Immutable
            "System.Collections.Immutable.ImmutableArray<T>" => "List",
            "System.Collections.Immutable.ImmutableSortedSet<T>" => "HashSet",
            "System.Collections.Immutable.ImmutableHashSet<T>" => "HashSet",
            "System.Collections.Immutable.ImmutableDictionary<TKey, TValue>" => "Dictionary",
            "System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>" => "Dictionary",

            // Queue / Stack
            "System.Collections.Generic.Queue<T>" => "Queue",
            "System.Collections.Generic.Stack<T>" => "Stack",

            _ => null
        };

        return kind != null;
    }

    internal static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol? type)
    {
        while (type != null && type.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (!member.IsReadOnly && member.SetMethod != null)
                    yield return member;
            }

            type = type.BaseType;
        }
    }

}