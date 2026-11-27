// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace UtilityVerse.Copy;

internal static class Helper
{
    private static readonly SymbolDisplayFormat TypeDisplay = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
        SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    internal static string TypeName(ITypeSymbol type) => type.ToDisplayString(TypeDisplay);

    internal static string TypeName(ITypeSymbol type, IReadOnlyDictionary<ITypeParameterSymbol, string> names) =>
        string.Concat(type.ToDisplayParts(TypeDisplay).Select(part =>
            part.Symbol is ITypeParameterSymbol parameter && names.TryGetValue(parameter, out var name)
                ? name
                : part.ToString()));

    internal static string Identifier(string name) => SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    internal static string LocalName(INamedTypeSymbol type) => Identifier(type.Name) +
        (type.TypeParameters.Length == 0 ? "" : "<" + string.Join(", ", type.TypeParameters.Select(p => Identifier(p.Name))) + ">");

    internal static bool IsTrulyPrimitive(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum || type.SpecialType is >= SpecialType.System_Boolean and <= SpecialType.System_Decimal ||
            type.SpecialType == SpecialType.System_String)
        {
            return true;
        }

        if (IsBuiltinType(type))
        {
            return true;
        }

        if (type is not INamedTypeSymbol { IsValueType: true } named)
        {
            return false;
        }

        return named.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic).All(f => IsTrulyPrimitive(f.Type));
    }

    // Uri is extensible and must go through runtime dispatch, even inside structs.
    internal static bool IsUri(ITypeSymbol type) =>
        type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString() == "System.Uri";

    private static bool IsBuiltinType(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString() is
        "System.DateTime" or "System.DateOnly" or "System.DateTimeOffset" or "System.Guid" or "System.TimeSpan" or
        "decimal" or "System.Decimal" or "System.Version" or "System.Numerics.BigInteger" or
        "System.Numerics.Complex" or "System.Index" or "System.Range" or "System.Half" or "System.Int128" or "System.UInt128";

    internal static bool IsGenericCollection(string? originalDef, out string? kind)
    {
        kind = originalDef switch
        {
            "System.Collections.Generic.List<T>" or "System.Collections.Generic.IList<T>" or
            "System.Collections.Generic.IEnumerable<T>" or "System.Collections.Generic.ICollection<T>" or
            "System.Collections.Generic.IReadOnlyList<T>" or "System.Collections.Generic.IReadOnlyCollection<T>" or
            "System.Collections.ObjectModel.Collection<T>" or "System.Collections.ObjectModel.ReadOnlyCollection<T>" or
            "System.Collections.ObjectModel.ObservableCollection<T>" or "System.ComponentModel.BindingList<T>" or
            "System.Collections.Generic.HashSet<T>" or "System.Collections.Generic.Queue<T>" or "System.Collections.Generic.Stack<T>" => "Sequence",
            "System.Collections.Generic.Dictionary<TKey, TValue>" => "Dictionary",
            "System.Collections.Concurrent.ConcurrentDictionary<TKey, TValue>" => "ConcurrentDictionary",
            _ => null
        };

        return kind != null;
    }

    internal static IEnumerable<ISymbol> GetOwnState(INamedTypeSymbol type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.IsStatic)
            {
                continue;
            }

            if (member is IFieldSymbol { IsImplicitlyDeclared: false, IsConst: false })
            {
                yield return member;
            }

            if (member is IPropertySymbol { IsIndexer: false } property && IsAutoProperty(property))
            {
                yield return member;
            }
        }
    }

    internal static bool IsAutoProperty(IPropertySymbol property) => property.ContainingType.GetMembers().OfType<IFieldSymbol>()
        .Any(f => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, property));

    internal static ITypeSymbol StateType(ISymbol member) => member switch
    {
        IPropertySymbol p => p.Type,
        IFieldSymbol f => f.Type,
        _ => throw new System.InvalidOperationException("Expected stored state.")
    };

    internal static IEnumerable<IPropertySymbol> GetAllProperties(INamedTypeSymbol? type)
    {
        var names = new HashSet<string>();

        while (type != null && type.SpecialType != SpecialType.System_Object)
        {
            foreach (var property in type.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic && !property.IsIndexer && names.Add(property.Name))
                {
                    yield return property;
                }
            }

            type = type.BaseType;
        }
    }

    internal static bool IsPartial(INamedTypeSymbol type) => type.DeclaringSyntaxReferences.Any(r =>
        r.GetSyntax() is TypeDeclarationSyntax syntax && syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

    internal static bool HasDeepCopyOptIn(INamedTypeSymbol type) => type.GetAttributes().Any(a => SymbolEquals(a.AttributeClass, "UtilityVerse.Copy.DeepCopy")) ||
        type.AllInterfaces.Any(i => SymbolEquals(i, "UtilityVerse.Copy.IDeepCopy"));

    internal static bool HasShallowCopyOptIn(INamedTypeSymbol type) => type.GetAttributes().Any(a => SymbolEquals(a.AttributeClass, "UtilityVerse.Copy.ShallowCopy")) ||
        type.AllInterfaces.Any(i => SymbolEquals(i, "UtilityVerse.Copy.IShallowCopy"));

    internal static bool SymbolEquals(ISymbol? symbol, string name) => symbol?.ToDisplayString() == name;

    internal static bool HasRequiredState(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (current.GetMembers().Any(m => m is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true }))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasGeneratedDeepBase(INamedTypeSymbol type) => type.BaseType != null && HasDeepCopyOptIn(type.BaseType);

    internal static IEnumerable<ISymbol> GetCopiedState(INamedTypeSymbol type)
    {
        var names = new HashSet<string>();
        var current = type;

        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in GetOwnState(current))
            {
                var firstWithName = names.Add(member.Name);

                if (firstWithName || StorageAccess.RequiresAccessor(member))
                {
                    yield return member;
                }
            }

            if (HasGeneratedDeepBase(current))
            {
                yield break;
            }

            current = current.BaseType;
        }
    }

    internal static IMethodSymbol? PrimaryConstructor(INamedTypeSymbol type) => type.InstanceConstructors
        .FirstOrDefault(c => c.DeclaringSyntaxReferences.Any(r =>
            r.GetSyntax() is TypeDeclarationSyntax { ParameterList: not null }));

    // A primary constructor runs before the destination can be registered. Never
    // pass it a mutable source reference, including one nested inside a struct.

    internal static bool IsConstructorArgumentSafe(ITypeSymbol type) =>
        type.SpecialType is >= SpecialType.System_Boolean and <= SpecialType.System_Decimal ||
        type.SpecialType == SpecialType.System_String || type.TypeKind == TypeKind.Enum ||
        type is INamedTypeSymbol { IsValueType: true } named &&
        named.GetMembers().OfType<IFieldSymbol>().Where(f => !f.IsStatic).All(f => IsConstructorArgumentSafe(f.Type));

    internal static ISymbol? MatchParameter(INamedTypeSymbol type, IParameterSymbol parameter) =>
        GetCopiedState(type)
            .FirstOrDefault(m => m.Name.TrimStart('_').Equals(parameter.Name, System.StringComparison.OrdinalIgnoreCase) &&
                (m is not IPropertySymbol property || property.GetMethod != null) && IsAssignable(StateType(m), parameter.Type));

    private static bool IsAssignable(ITypeSymbol source, ITypeSymbol target)
    {
        if (SymbolEqualityComparer.Default.Equals(source, target) || target.SpecialType == SpecialType.System_Object)
        {
            return true;
        }

        if (source.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target)))
        {
            return true;
        }

        for (var current = source.BaseType; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, target))
            {
                return true;
            }
        }

        return false;
    }

    internal static string MemberIndentation(INamedTypeSymbol type)
    {
        var depth = type.ContainingNamespace.IsGlobalNamespace ? 1 : 2;

        for (var parent = type.ContainingType; parent != null; parent = parent.ContainingType)
        {
            depth++;
        }

        return new string(' ', depth * 4);
    }

    internal static string BeginType(INamedTypeSymbol type, string? interfaces, bool includeHeader = true)
    {
        var sb = new StringBuilder(includeHeader
            ? "// <auto-generated/>\n#nullable enable\nusing System;\nusing System.Linq;\nusing System.Collections.Generic;\n\n"
            : "");
        var indent = "";

        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            sb.AppendLine("namespace " + type.ContainingNamespace.ToDisplayString());
            sb.AppendLine("{");
            indent = "    ";
        }

        var parents = new Stack<INamedTypeSymbol>();

        for (var parent = type.ContainingType; parent != null; parent = parent.ContainingType)
        {
            parents.Push(parent);
        }

        foreach (var parent in parents)
        {
            sb.AppendLine(indent + Declaration(parent));
            sb.AppendLine(indent + "{");
            indent += "    ";
        }

        sb.AppendLine(indent + Declaration(type) + (interfaces == null ? "" : " : " + interfaces));
        sb.AppendLine(indent + "{");

        return sb.ToString();
    }

    internal static string EndType(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();
        var indent = MemberIndentation(type);

        for (var current = type; current != null; current = current.ContainingType)
        {
            indent = indent.Substring(0, indent.Length - 4);
            sb.AppendLine(indent + "}");
        }

        if (!type.ContainingNamespace.IsGlobalNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static string Declaration(INamedTypeSymbol type)
    {
        var access = type.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Private => "private",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "internal"
        };
        string kind;

        if (type.IsRecord)
        {
            kind = type.IsValueType ? "record struct" : "record";
        }
        else
        {
            kind = type.TypeKind switch
            {
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
                _ => "class"
            };
        }

        return access + (type.IsStatic ? " static" : "") + (type.IsReadOnly ? " readonly" : "") + " partial " + kind + " " + LocalName(type);
    }
}
