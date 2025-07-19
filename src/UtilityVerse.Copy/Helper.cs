using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UtilityVerse.Copy;

internal static class Helper
{
	internal static bool IsTrulyPrimitive(ITypeSymbol symbol)
	{
		return symbol.IsValueType || symbol.TypeKind is TypeKind.Enum || symbol.SpecialType switch
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
			SpecialType.System_Enum or
			SpecialType.System_String => true,
			_ => false
		};
	}

	internal static bool IsBuiltinType(ITypeSymbol symbol)
	{
		return symbol.ToDisplayString() switch
		{
			"System.DateTime" or
			"System.DateOnly" or
			"System.DateTimeOffset" or
			"System.Guid" or
			"System.TimeSpan" or
			"System.Decimal" or
			"System.Uri" or
			"System.Version" or
			"System.Numerics.BigInteger" or
			"System.Numerics.Complex" or
			"System.Index" or
			"System.Range" or
			"System.Half" or
			"System.Int128" or
			"System.UInt128" => true,
			_ => false
		};
	}
	internal static bool IsCollectionType(ITypeSymbol symbol)
	{
		return symbol.AllInterfaces.Any(i =>
			i.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>");
	}

	internal static bool IsGenericCollection(string? originalDef, out string? kind)
	{
		kind = originalDef switch
		{
			"System.Collections.Generic.List<T>" => "List",
			"System.Collections.Generic.IList<T>" => "List",
			"System.Collections.ObjectModel.Collection<T>" => "List",
			"System.Collections.ObjectModel.ReadOnlyCollection<T>" => "List",
			"System.Collections.ObjectModel.ObservableCollection<T>" => "List",
			"System.ComponentModel.BindingList<T>" => "List",
			"System.Collections.Immutable.ImmutableList<T>" => "List",
			"System.Collections.Generic.ReadOnlyList<T>" => "List",

			"System.Collections.Generic.IEnumerable<T>" => "Enumerable",
			"System.Collections.Generic.ICollection<T>" => "Enumerable",
			"System.Collections.Generic.IReadOnlyCollection<T>" => "Enumerable",
			"System.Collections.Generic.IReadOnlyList<T>" => "Enumerable",

			"System.Collections.Generic.HashSet<T>" => "HashSet",

			"System.Collections.Generic.Dictionary<TKey, TValue>" => "Dictionary",
			"System.Collections.Frozen.FrozenDictionary<TKey, TValue>" => "Dictionary",
			"System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>" => "Dictionary",

			"System.Collections.Immutable.ImmutableArray<T>" => "List",
			"System.Collections.Immutable.ImmutableSortedSet<T>" => "HashSet",
			"System.Collections.Immutable.ImmutableHashSet<T>" => "HashSet",
			"System.Collections.Immutable.ImmutableDictionary<TKey, TValue>" => "Dictionary",
			"System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>" => "Dictionary",

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

	internal static bool IsPartial(INamedTypeSymbol typeSymbol)
	{
		return typeSymbol.DeclaringSyntaxReferences.Any(syntaxRef =>
			syntaxRef.GetSyntax() is TypeDeclarationSyntax syntax &&
			syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));
	}

	internal static bool HasDeepCopyOptIn(INamedTypeSymbol typeSymbol)
	{
		return typeSymbol.GetAttributes().Any(a =>
				   SymbolEquals(a.AttributeClass, "UtilityVerse.Copy.DeepCopy")) ||
			   typeSymbol.AllInterfaces.Any(i =>
				   SymbolEquals(i, "UtilityVerse.Copy.IDeepCopy"));
	}

	internal static bool HasShallowCopyOptIn(INamedTypeSymbol typeSymbol)
	{
		return typeSymbol.GetAttributes().Any(a =>
				   SymbolEquals(a.AttributeClass, "UtilityVerse.Copy.ShallowCopy")) ||
			   typeSymbol.AllInterfaces.Any(i =>
				   SymbolEquals(i, "UtilityVerse.Copy.IShallowCopy"));
	}

	internal static bool SymbolEquals(ISymbol? symbol, string fullyQualifiedMetadataName)
	{
		return symbol?.ToDisplayString() == fullyQualifiedMetadataName;
	}
	internal static INamedTypeSymbol UnwrapNullable(ITypeSymbol type)
	{
		if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
			type is INamedTypeSymbol namedType &&
			namedType.TypeArguments.FirstOrDefault() is INamedTypeSymbol underlying)
		{
			return underlying;
		}

		return type as INamedTypeSymbol ?? throw new System.InvalidOperationException("Expected named type.");
	}
}
