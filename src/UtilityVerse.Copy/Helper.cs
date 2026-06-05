using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UtilityVerse.Copy;

internal static class Helper
{
	// Must stay in sync with the default on the DeepCopy/ShallowCopy attributes (Depth = 5).
	private const int DefaultDepth = 5;

	internal static bool IsTrulyPrimitive(ITypeSymbol symbol)
	{
		// Every value type (numerics, bool, char, enums, decimal, user-defined structs) is copied
		// by value; string is the only reference type treated as primitive (it is immutable).
		return symbol.IsValueType || symbol.SpecialType == SpecialType.System_String;
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
		var seen = new HashSet<string>(StringComparer.Ordinal);
		while (type != null && type.SpecialType != SpecialType.System_Object)
		{
			foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
			{
				if (member.IsStatic) continue;
				if (!member.IsReadOnly && member.SetMethod != null && seen.Add(member.Name))
					yield return member;
			}

			type = type.BaseType;
		}
	}

	/// <summary>
	/// All readable instance properties (including get-only), walked up the inheritance chain.
	/// Used to match constructor parameters by name — unlike <see cref="GetAllProperties"/> this
	/// includes get-only properties, which are readable even though they cannot be assigned.
	/// </summary>
	internal static IEnumerable<IPropertySymbol> GetReadableProperties(INamedTypeSymbol? type)
	{
		var seen = new HashSet<string>(StringComparer.Ordinal);
		while (type != null && type.SpecialType != SpecialType.System_Object)
		{
			foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
			{
				if (member.IsStatic) continue;
				if (member.GetMethod != null && seen.Add(member.Name))
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
		return typeSymbol.GetAttributes().Any(a => SymbolEquals(a.AttributeClass, "UtilityVerse.Copy.DeepCopy"))
		|| typeSymbol.AllInterfaces.Any(i => SymbolEquals(i, "UtilityVerse.Copy.IDeepCopy"));
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

	internal static int ExtractDepth(INamedTypeSymbol typeSymbol, string attributeName = "DeepCopy")
	{
		int extractedDepth = DefaultDepth;

		var attribute = typeSymbol.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.Name == attributeName);

		if (attribute?.ConstructorArguments.Length > 0)
		{
			if (attribute.ConstructorArguments[0].Value is int attrDepth)
				extractedDepth = attrDepth;
		}

		var depthNamed = attribute?.NamedArguments
			.FirstOrDefault(na => na.Key == "Depth");

		if (depthNamed?.Value.Value is int namedDepth)
			extractedDepth = namedDepth;

		return extractedDepth;
	}

	// ── Code scaffolding helpers ─────────────────────────────────────────────────

	/// <summary>
	/// The simple type name including any type parameters (e.g. "Box&lt;T&gt;"), as it must appear
	/// in the generated partial declaration and in <c>new</c>/cast expressions.
	/// </summary>
	internal static string GetTypeNameWithTypeParameters(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.TypeParameters.Length == 0)
			return typeSymbol.Name;

		return $"{typeSymbol.Name}<{string.Join(", ", typeSymbol.TypeParameters.Select(tp => tp.Name))}>";
	}

	/// <summary>
	/// The chain of enclosing types from outermost to the immediate parent. Empty for a top-level type.
	/// </summary>
	internal static List<INamedTypeSymbol> GetContainingTypeChain(INamedTypeSymbol type)
	{
		var chain = new List<INamedTypeSymbol>();
		for (var enclosing = type.ContainingType; enclosing != null; enclosing = enclosing.ContainingType)
			chain.Add(enclosing);
		chain.Reverse();
		return chain;
	}

	/// <summary>
	/// A nested type can be generated only if every enclosing type is <c>partial</c> and has a supported
	/// type kind (so the generated partial can be nested in the same context). Always true for top-level
	/// types. <paramref name="chain"/> is the outermost-first enclosing chain.
	/// </summary>
	internal static bool EnclosingTypesAreGeneratable(INamedTypeSymbol type, out List<INamedTypeSymbol> chain)
	{
		chain = GetContainingTypeChain(type);
		return chain.All(enclosing => IsPartial(enclosing) && GetTypeKind(enclosing) != null);
	}

	internal static string? GetTypeKind(INamedTypeSymbol typeSymbol) => typeSymbol switch
	{
		{ TypeKind: TypeKind.Class, IsRecord: true } => "record",
		{ TypeKind: TypeKind.Struct, IsRecord: true } => "record struct",
		{ TypeKind: TypeKind.Class } => "class",
		{ TypeKind: TypeKind.Struct } => "struct",
		_ => null
	};

	internal static void AppendFileHeader(StringBuilder sb, string copyKind)
	{
		sb.AppendLine();
		sb.AppendLine("// <auto-generated>");
		sb.AppendLine("//     This code was generated by Copy.");
		sb.AppendLine("//     Author: Pritom Purkayasta");
		sb.AppendLine("//     DO NOT modify this file manually. Changes may be overwritten.");
		sb.AppendLine($"//     This file contains auto-generated {copyKind}Copy() implementations.");
		sb.AppendLine("// </auto-generated>");
		sb.AppendLine();
		sb.AppendLine("using System;");
		sb.AppendLine("using System.Linq;");
		sb.AppendLine("using System.Collections.Generic;");
		sb.AppendLine("using System.Collections.ObjectModel;");
		sb.AppendLine("using System.Collections.Immutable;");
		sb.AppendLine("using System.Collections.Concurrent;");
		sb.AppendLine("using System.Collections.Frozen;");
		sb.AppendLine();
	}

	internal static void AppendNamespaceOpen(StringBuilder sb, string? namespaceName)
	{
		if (!string.IsNullOrEmpty(namespaceName))
		{
			sb.AppendLine($"namespace {namespaceName}");
			sb.AppendLine("{");
		}
	}

	internal static void AppendNamespaceClose(StringBuilder sb, string? namespaceName)
	{
		if (!string.IsNullOrEmpty(namespaceName))
			sb.AppendLine("}");
	}

	internal static void AppendTypeDeclarationOpen(StringBuilder sb, string typeKind, string typeName)
	{
		sb.AppendLine($"    public partial {typeKind} {typeName}");
		sb.AppendLine("    {");
	}

	internal static void AppendTypeDeclarationClose(StringBuilder sb)
	{
		sb.AppendLine("    }");
	}

	/// <summary>
	/// Opens a <c>partial</c> declaration for each enclosing type (outermost first) so a nested type's
	/// generated members land in the correct nesting context. The access modifier is intentionally
	/// omitted — partial declarations need not repeat it, and omitting avoids accessibility mismatches.
	/// </summary>
	internal static void AppendEnclosingTypesOpen(StringBuilder sb, List<INamedTypeSymbol> chain)
	{
		foreach (var enclosing in chain)
		{
			sb.AppendLine($"    partial {GetTypeKind(enclosing)} {GetTypeNameWithTypeParameters(enclosing)}");
			sb.AppendLine("    {");
		}
	}

	internal static void AppendEnclosingTypesClose(StringBuilder sb, List<INamedTypeSymbol> chain)
	{
		for (var i = 0; i < chain.Count; i++)
			sb.AppendLine("    }");
	}

	/// <summary>
	/// Emits the circular-reference identity check and depth guard for class types.
	/// Must only be called for class (reference) types, not structs.
	/// </summary>
	internal static void AppendDeepCopyDepthGuard(StringBuilder sb, string typeName)
	{
		sb.AppendLine("            // Identity check first: if this object was already copied, return the same");
		sb.AppendLine("            // instance regardless of depth — this preserves circular references.");
		sb.AppendLine("            if (objectMap.TryGetValue(this, out var existing))");
		sb.AppendLine($"                return ({typeName})existing;");
		sb.AppendLine();
		sb.AppendLine("            // Otherwise, if at or beyond max depth, stop recursing (depth limit reached)");
		sb.AppendLine("            if (currentDepth >= maxDepth)");
		sb.AppendLine("                return null!;");
		sb.AppendLine();
	}

	// ── Deep copy assignment generation ──────────────────────────────────────────

	internal static string GenerateDeepCopyAssignment(ITypeSymbol propType, string propertyName)
	{
		if (IsTrulyPrimitive(propType) || IsBuiltinType(propType))
			return $"this.{propertyName}";

		// Open type parameters are opaque at generation time — we cannot know whether T has a
		// DeepCopy method, so we copy the value as-is (shared reference / value).
		if (propType is ITypeParameterSymbol)
			return $"this.{propertyName}";

		return propType switch
		{
			IArrayTypeSymbol arrayType => GenerateDeepCopyArrayAssignment(arrayType, propertyName),
			INamedTypeSymbol { IsGenericType: true } named => GenerateDeepCopyGenericAssignment(named, propertyName),
			_ => $"currentDepth + 1 <= maxDepth ? this.{propertyName}?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap) : null"
		};
	}

	private static string GenerateDeepCopyArrayAssignment(IArrayTypeSymbol arrayType, string propertyName)
	{
		var elementType = arrayType.ElementType;
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);

		if (elementIsPrimitive)
			return $"this.{propertyName}?.ToArray()";

		return $"currentDepth + 1 <= maxDepth ? this.{propertyName}?.Select(x => x?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap)).ToArray() : null";
	}

	private static string GenerateDeepCopyGenericAssignment(INamedTypeSymbol named, string propertyName)
	{
		var original = named.OriginalDefinition.ToDisplayString();
		var typeArgs = named.TypeArguments;

		if (IsGenericCollection(original, out var collectionKind))
			return GenerateDeepCopyCollectionAssignment(collectionKind!, typeArgs, propertyName);

		if (original.StartsWith("System.Tuple") || original.StartsWith("System.ValueTuple"))
			return GenerateDeepCopyTupleAssignment(typeArgs, propertyName, named);

		return $"currentDepth + 1 <= maxDepth ? this.{propertyName}?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap) : null";
	}

	private static string GenerateDeepCopyCollectionAssignment(string collectionKind, ImmutableArray<ITypeSymbol> typeArgs, string propertyName)
	{
		return collectionKind switch
		{
			"HashSet" => GenerateDeepCopyHashSetAssignment(typeArgs[0], propertyName),
			"Dictionary" => GenerateDeepCopyDictionaryAssignment(typeArgs, propertyName),
			"Queue" => GenerateDeepCopyQueueAssignment(typeArgs[0], propertyName),
			"Stack" => GenerateDeepCopyStackAssignment(typeArgs[0], propertyName),
			_ => GenerateDeepCopyListAssignment(typeArgs[0], propertyName)
		};
	}

	private static string GenerateDeepCopyHashSetAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);
		var elementTypeName = elementType.ToDisplayString();

		return elementIsPrimitive
			? $"this.{propertyName} != null ? new HashSet<{elementTypeName}>(this.{propertyName}) : null"
			: $"currentDepth + 1 <= maxDepth && this.{propertyName} != null ? new HashSet<{elementTypeName}>(this.{propertyName}.Select(x => x?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap)).ToList()) : null";
	}

	private static string GenerateDeepCopyDictionaryAssignment(ImmutableArray<ITypeSymbol> typeArgs, string propertyName)
	{
		var keyType = typeArgs[0];
		var valueType = typeArgs[1];
		bool keyIsPrimitive = IsTrulyPrimitive(keyType) || IsBuiltinType(keyType);
		bool valueIsPrimitive = IsTrulyPrimitive(valueType) || IsBuiltinType(valueType);

		var keyCopy = keyIsPrimitive ? "x.Key" : "x.Key?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap)";
		var valueCopy = valueIsPrimitive ? "x.Value" : "x.Value?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap)";

		return (!keyIsPrimitive || !valueIsPrimitive)
			? $"currentDepth + 1 <= maxDepth ? this.{propertyName}?.ToDictionary(x => {keyCopy}, x => {valueCopy}) : null"
			: $"this.{propertyName}?.ToDictionary(x => {keyCopy}, x => {valueCopy})";
	}

	private static string GenerateDeepCopyQueueAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);
		var elementTypeName = elementType.ToDisplayString();

		return elementIsPrimitive
			? $"this.{propertyName} != null ? new Queue<{elementTypeName}>(this.{propertyName}) : null"
			: $"currentDepth + 1 <= maxDepth && this.{propertyName} != null ? new Queue<{elementTypeName}>(this.{propertyName}.Select(x => x?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap))) : null";
	}

	private static string GenerateDeepCopyStackAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);
		var elementTypeName = elementType.ToDisplayString();

		// new Stack<T>(IEnumerable) pushes in enumeration order, which reverses a stack; Reverse() first
		// so the copy preserves the original top-to-bottom order.
		return elementIsPrimitive
			? $"this.{propertyName} != null ? new Stack<{elementTypeName}>(this.{propertyName}.Reverse()) : null"
			: $"currentDepth + 1 <= maxDepth && this.{propertyName} != null ? new Stack<{elementTypeName}>(this.{propertyName}.Select(x => x?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap)).Reverse()) : null";
	}

	private static string GenerateDeepCopyListAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);

		return elementIsPrimitive
			? $"this.{propertyName}?.ToList()"
			: $"currentDepth + 1 <= maxDepth ? this.{propertyName}?.Select(x => x?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap)).ToList() : null";
	}

	private static string GenerateDeepCopyTupleAssignment(ImmutableArray<ITypeSymbol> typeArgs, string propertyName, INamedTypeSymbol tupleType)
	{
		bool tupleHasComplex = typeArgs.Any(arg => !IsTrulyPrimitive(arg) && !IsBuiltinType(arg));
		var tupleArgs = typeArgs.Select((arg, i) =>
			(IsTrulyPrimitive(arg) || IsBuiltinType(arg))
				? $"x.Item{i + 1}"
				: $"x.Item{i + 1}?.DeepCopyInternal(currentDepth + 1, maxDepth, objectMap)");

		return tupleHasComplex
			? $"currentDepth + 1 <= maxDepth && this.{propertyName} is {{ }} x ? new {tupleType.ToDisplayString()}({string.Join(", ", tupleArgs)}) : default"
			: $"this.{propertyName} is {{ }} x ? new {tupleType.ToDisplayString()}({string.Join(", ", tupleArgs)}) : default";
	}

	// ── Shallow copy assignment generation ───────────────────────────────────────

	internal static string GenerateShallowCopyAssignment(ITypeSymbol propType, string propertyName)
	{
		if (IsTrulyPrimitive(propType) || IsBuiltinType(propType))
			return $"this.{propertyName}";

		// Open type parameters are opaque at generation time — copy the value as-is.
		if (propType is ITypeParameterSymbol)
			return $"this.{propertyName}";

		return propType switch
		{
			IArrayTypeSymbol arrayType => GenerateShallowCopyArrayAssignment(arrayType, propertyName),
			INamedTypeSymbol { IsGenericType: true } named => GenerateShallowCopyGenericAssignment(named, propertyName),
			_ => $"currentDepth < maxDepth ? this.{propertyName}?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap) : this.{propertyName}"
		};
	}

	private static string GenerateShallowCopyArrayAssignment(IArrayTypeSymbol arrayType, string propertyName)
	{
		var elementType = arrayType.ElementType;
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);

		if (elementIsPrimitive)
			return $"this.{propertyName} != null ? ({arrayType.ToDisplayString()})this.{propertyName}.Clone() : null";

		return $"currentDepth + 1 <= maxDepth ? this.{propertyName}?.Select(x => x?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap)).ToArray() : this.{propertyName}?.ToArray()";
	}

	private static string GenerateShallowCopyGenericAssignment(INamedTypeSymbol named, string propertyName)
	{
		var original = named.OriginalDefinition.ToDisplayString();
		var typeArgs = named.TypeArguments;

		if (IsGenericCollection(original, out var collectionKind))
			return GenerateShallowCopyCollectionAssignment(collectionKind!, typeArgs, propertyName);

		if (original.StartsWith("System.Tuple") || original.StartsWith("System.ValueTuple"))
			return $"this.{propertyName}"; // Tuples: share the reference (shallow copy semantics)

		return $"currentDepth < maxDepth ? this.{propertyName}?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap) : this.{propertyName}";
	}

	private static string GenerateShallowCopyCollectionAssignment(string collectionKind, ImmutableArray<ITypeSymbol> typeArgs, string propertyName)
	{
		return collectionKind switch
		{
			"HashSet" => GenerateShallowCopyHashSetAssignment(typeArgs[0], propertyName),
			"Dictionary" => GenerateShallowCopyDictionaryAssignment(typeArgs, propertyName),
			"Queue" => GenerateShallowCopyQueueAssignment(typeArgs[0], propertyName),
			"Stack" => GenerateShallowCopyStackAssignment(typeArgs[0], propertyName),
			_ => GenerateShallowCopyListAssignment(typeArgs[0], propertyName)
		};
	}

	private static string GenerateShallowCopyHashSetAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);
		var elementTypeName = elementType.ToDisplayString();
		var cloneExpr = "x?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap)";

		return elementIsPrimitive
			? $"this.{propertyName} != null ? new HashSet<{elementTypeName}>(this.{propertyName}) : null"
			: $"this.{propertyName} != null ? new HashSet<{elementTypeName}>(currentDepth < maxDepth ? this.{propertyName}.Select(x => {cloneExpr}).ToList() : this.{propertyName}) : null";
	}

	private static string GenerateShallowCopyDictionaryAssignment(ImmutableArray<ITypeSymbol> typeArgs, string propertyName)
	{
		var keyType = typeArgs[0];
		var valueType = typeArgs[1];
		bool keyIsPrimitive = IsTrulyPrimitive(keyType) || IsBuiltinType(keyType);
		bool valueIsPrimitive = IsTrulyPrimitive(valueType) || IsBuiltinType(valueType);

		var keyCopy = keyIsPrimitive ? "x.Key" : "x.Key?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap)";
		var valueCopy = valueIsPrimitive ? "x.Value" : "x.Value?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap)";

		return (!keyIsPrimitive || !valueIsPrimitive)
			? $"currentDepth < maxDepth ? this.{propertyName}?.ToDictionary(x => {keyCopy}, x => {valueCopy}) : this.{propertyName}?.ToDictionary(x => x.Key, x => x.Value)"
			: $"this.{propertyName}?.ToDictionary(x => {keyCopy}, x => {valueCopy})";
	}

	private static string GenerateShallowCopyQueueAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);
		var elementTypeName = elementType.ToDisplayString();
		var cloneExpr = "x?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap)";

		return elementIsPrimitive
			? $"this.{propertyName} != null ? new Queue<{elementTypeName}>(this.{propertyName}) : null"
			: $"this.{propertyName} != null ? new Queue<{elementTypeName}>(currentDepth < maxDepth ? this.{propertyName}.Select(x => {cloneExpr}) : this.{propertyName}) : null";
	}

	private static string GenerateShallowCopyStackAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);
		var elementTypeName = elementType.ToDisplayString();
		var cloneExpr = "x?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap)";

		// new Stack<T>(IEnumerable) reverses order; Reverse() first so the copy preserves the original order.
		return elementIsPrimitive
			? $"this.{propertyName} != null ? new Stack<{elementTypeName}>(this.{propertyName}.Reverse()) : null"
			: $"this.{propertyName} != null ? new Stack<{elementTypeName}>((currentDepth < maxDepth ? this.{propertyName}.Select(x => {cloneExpr}) : (IEnumerable<{elementTypeName}>)this.{propertyName}).Reverse()) : null";
	}

	private static string GenerateShallowCopyListAssignment(ITypeSymbol elementType, string propertyName)
	{
		bool elementIsPrimitive = IsTrulyPrimitive(elementType) || IsBuiltinType(elementType);
		var cloneExpr = "x?.ShallowCopyInternal(currentDepth + 1, maxDepth, objectMap)";

		return elementIsPrimitive
			? $"this.{propertyName}?.ToList()"
			: $"currentDepth < maxDepth ? this.{propertyName}?.Select(x => {cloneExpr}).ToList() : this.{propertyName}?.ToList()";
	}
}
