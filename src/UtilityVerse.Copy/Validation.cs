// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy;

internal static class Validation
{
    internal static bool HasUnsupportedShape(INamedTypeSymbol type, bool deep, out string reason)
    {
        reason = "";
        if (type.IsStatic || type.IsRefLikeType)
            reason = "static and ref-like types cannot be copied";
        for (var parent = type.ContainingType; parent != null; parent = parent.ContainingType)
            if (!Helper.IsPartial(parent)) reason = "containing types must also be partial";
        if (type.GetMembers(deep ? "DeepCopy" : "ShallowCopy").OfType<IMethodSymbol>().Any(m => m.Parameters.Length == 0 && IsUserDeclared(m)))
            reason = "a copy method with the generated signature already exists";
        if (type.GetMembers("__UtilityVerseDeepCopy").Any(IsUserDeclared)) reason = "the generated helper name is reserved";
        if (!deep) return reason.Length != 0;
        if (type.IsRecord && !type.IsValueType && type.BaseType is { SpecialType: not SpecialType.System_Object })
            reason = "record inheritance requires a custom copy implementation";
        if (type.GetMembers().OfType<IEventSymbol>().Any(e => !e.IsStatic) ||
            type.GetMembers().OfType<IFieldSymbol>().Any(f => !f.IsStatic && f.IsImplicitlyDeclared && f.AssociatedSymbol == null))
            reason = "event subscriptions and captured primary-constructor state require a custom copy implementation";
        if (type.InstanceConstructors.Any(c => c.Parameters.Length == 2 &&
            SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, type) &&
            c.Parameters[1].Type.ToDisplayString() == "UtilityVerse.Copy.CopyContext" && IsUserDeclared(c)))
            reason = "the generated copy constructor signature is reserved";
        if (!type.IsValueType && !type.IsRecord && !Helper.HasGeneratedDeepBase(type) && Helper.CopyConstructorTarget(type) == null)
            reason = "a constructor must have parameters matching readable state members";
        if (!type.IsValueType && Helper.HasGeneratedDeepBase(type))
        {
            var parent = type.BaseType!;
            if (parent.DeclaringSyntaxReferences.Length == 0)
            {
                if (!parent.InstanceConstructors.Any(c => c.Parameters.Length == 2 &&
                    SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, parent) &&
                    c.Parameters[1].Type.ToDisplayString() == "UtilityVerse.Copy.CopyContext"))
                    reason = "the base type must be rebuilt with graph-aware copy support";
            }
            else if (!Helper.IsPartial(parent) || HasUnsupportedShape(parent, true, out _))
                reason = "the opted-in base type must support generated deep copying";
        }
        if (!type.IsValueType && !Helper.HasGeneratedDeepBase(type))
        {
            for (var parent = type.BaseType; parent != null && parent.SpecialType != SpecialType.System_Object; parent = parent.BaseType)
            {
                if (parent.DeclaringSyntaxReferences.Length == 0 || Helper.HasDeepCopyOptIn(parent) ||
                    parent.GetMembers().OfType<IEventSymbol>().Any(e => !e.IsStatic) ||
                    parent.GetMembers().OfType<IFieldSymbol>().Any(f => !f.IsStatic && f.IsImplicitlyDeclared && f.AssociatedSymbol == null))
                    reason = "the base hierarchy must opt into deep copying to preserve hidden state";
                foreach (var inherited in Helper.GetOwnState(parent))
                {
                    if (!CanAccess(inherited, type) || inherited is IFieldSymbol { IsReadOnly: true } ||
                        inherited is IPropertySymbol p && (p.GetMethod == null || p.SetMethod == null || !CanAccess(p.GetMethod, type) || !CanAccess(p.SetMethod, type)))
                        reason = "the base type must opt into deep copying to preserve inaccessible or readonly stored state";
                    if (type.GetMembers(inherited.Name).Any(m => m is IFieldSymbol || m is IPropertySymbol { IsOverride: false }))
                        reason = "the base type must opt into deep copying to preserve hidden state with duplicate names";
                }
            }
        }
        foreach (var member in Helper.GetCopiedState(type))
        {
            if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, type))
            {
                if (member.DeclaredAccessibility == Accessibility.Private ||
                    member is IFieldSymbol { IsReadOnly: true } ||
                    member is IPropertySymbol { SetMethod: null } ||
                    member is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Private })
                    reason = "the base type must opt into deep copying to preserve inaccessible or readonly stored state";
            }
            var unsupported = UnsupportedType(Helper.StateType(member));
            if (unsupported != null) reason = member.Name + ": " + unsupported;
        }
        return reason.Length != 0;
    }

    private static bool CanAccess(ISymbol member, INamedTypeSymbol type) => member.DeclaredAccessibility switch
    {
        Accessibility.Private => false,
        Accessibility.Internal or Accessibility.ProtectedAndInternal => SymbolEqualityComparer.Default.Equals(member.ContainingAssembly, type.ContainingAssembly),
        _ => true
    };

    private static bool IsUserDeclared(ISymbol symbol) => symbol.DeclaringSyntaxReferences.Any(r =>
        !r.SyntaxTree.FilePath.EndsWith(".g.cs", System.StringComparison.OrdinalIgnoreCase));

    private static string? UnsupportedType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
            return array.Rank != 1 ? "only one-dimensional arrays are supported" : UnsupportedType(array.ElementType);
        if (type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || type.IsRefLikeType)
            return "pointer and ref-like state cannot be deep copied";
        if (type is not INamedTypeSymbol named || Helper.IsTrulyPrimitive(type)) return null;
        var original = named.OriginalDefinition.ToDisplayString();
        if (original.StartsWith("System.Collections.Immutable.", System.StringComparison.Ordinal) ||
            original.StartsWith("System.Collections.Frozen.", System.StringComparison.Ordinal))
            return "immutable and frozen collection types are not supported; use a supported collection or a custom copy method";
        if (Helper.IsGenericCollection(original, out var kind) && kind == "ConcurrentDictionary" && !named.GetMembers("Comparer").Any())
            return "this target framework does not expose ConcurrentDictionary.Comparer";
        if (Helper.IsGenericCollection(original, out _) || named.IsTupleType ||
            original.StartsWith("System.Tuple<", System.StringComparison.Ordinal) ||
            original.StartsWith("System.ValueTuple<", System.StringComparison.Ordinal) ||
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            foreach (var argument in named.TypeArguments)
            {
                var unsupported = UnsupportedType(argument);
                if (unsupported != null) return unsupported;
            }
        }
        return null;
    }

    internal static IEnumerable<ITypeSymbol> MissingOptIns(ITypeSymbol type)
    {
        if (Helper.IsTrulyPrimitive(type) || type is ITypeParameterSymbol) yield break;
        if (type is IArrayTypeSymbol array)
        {
            foreach (var missing in MissingOptIns(array.ElementType)) yield return missing;
            yield break;
        }
        if (type is not INamedTypeSymbol named) yield break;
        var original = named.OriginalDefinition.ToDisplayString();
        if (Helper.IsGenericCollection(original, out _) || named.IsTupleType ||
            original.StartsWith("System.Tuple<", System.StringComparison.Ordinal) ||
            original.StartsWith("System.ValueTuple<", System.StringComparison.Ordinal) ||
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            foreach (var argument in named.TypeArguments)
                foreach (var missing in MissingOptIns(argument)) yield return missing;
            yield break;
        }
        if (!Helper.HasDeepCopyOptIn(named)) yield return named;
    }
}
