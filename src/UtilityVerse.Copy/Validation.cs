// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UtilityVerse.Copy.Generators;

namespace UtilityVerse.Copy;

internal static class Validation
{
    internal static bool HasUnsupportedShape(INamedTypeSymbol type, bool deep, out string reason, Compilation compilation)
    {
        reason = "";

        if (type.IsStatic || type.IsRefLikeType)
        {
            reason = "static and ref-like types cannot be copied";
        }

        for (var parent = type.ContainingType; parent != null; parent = parent.ContainingType)
        {
            if (!Helper.IsPartial(parent))
            {
                reason = "containing types must also be partial";
            }
        }

        var copyMethodName = deep ? "DeepCopy" : "ShallowCopy";

        if (HasMethodCollision(type, copyMethodName))
        {
            reason = $"a declaration conflicts with the generated method '{copyMethodName}()'";
            return true;
        }

        if (!deep)
        {
            return reason.Length != 0;
        }

        var contextType = compilation.GetTypeByMetadataName("UtilityVerse.Copy.CopyContext");

        if (contextType != null && HasMethodCollision(type, "__UtilityVerseDeepCopy", contextType))
        {
            reason = "a declaration conflicts with the generated helper '__UtilityVerseDeepCopy(CopyContext)'";
            return true;
        }

        if (HasExplicitCopyImplementation(type, compilation, "UtilityVerse.Copy.IDeepCopyable", "Copy"))
        {
            reason = "the generated implementation of 'IDeepCopyable.Copy' is already declared explicitly";
            return true;
        }

        if (type.IsValueType && DeepCopyGenerator.CanUnbox(compilation) &&
            HasExplicitCopyImplementation(type, compilation, "UtilityVerse.Copy.IBoxedDeepCopyable", "CopyBoxed"))
        {
            reason = "the generated implementation of 'IBoxedDeepCopyable.CopyBoxed' is already declared explicitly";
            return true;
        }

        if (type.GetMembers("__UtilityVerseSequenceTypes").Any(IsUserDeclared) ||
            type.GetMembers("__UtilityVerseRegisterSequenceTypes").Any(IsUserDeclared))
        {
            reason = "the generated sequence registration name is reserved";
        }

        var hasGeneratedBase = Helper.HasGeneratedDeepBase(type);
        var copiesThroughRecordChain = Helper.UsesSynthesizedRecordCopy(type);

        if (type.GetMembers().OfType<IEventSymbol>().Any(e => !e.IsStatic) ||
            type.GetMembers().OfType<IFieldSymbol>().Any(f => !f.IsStatic && f.IsImplicitlyDeclared && f.AssociatedSymbol == null))
        {
            reason = "event subscriptions and captured primary-constructor state require a custom copy implementation";
        }

        if (contextType != null && type.InstanceConstructors.Any(constructor =>
            MatchesGraphConstructor(constructor, type, contextType) && IsUserDeclared(constructor)))
        {
            reason = "the generated copy constructor signature is reserved";
        }

        if (type.IsRecord && !type.IsValueType)
        {
            for (var record = type; record is { IsRecord: true }; record = copiesThroughRecordChain ? record.BaseType : null)
            {
                if (record.DeclaringSyntaxReferences.Length == 0 || record.InstanceConstructors.Any(constructor =>
                    !constructor.IsImplicitlyDeclared && constructor.Parameters.Length == 1 &&
                    IsByValueParameter(constructor.Parameters[0], record)))
                {
                    reason = $"record '{record.Name}' may use a custom record copy constructor that could mutate the source";
                    return true;
                }
            }
        }

        if (!type.IsRecord || type.IsValueType)
        {
            var primary = Helper.PrimaryConstructor(type);

            if (primary != null)
            {
                if (hasGeneratedBase)
                {
                    reason = "a primary constructor cannot chain to the generated base copy constructor";
                }

                if (primary.Parameters.Any(p => p.RefKind != RefKind.None || !Helper.IsConstructorArgumentSafe(p.Type) ||
                    Helper.MatchParameter(type, p) is not { } member || !Helper.IsConstructorArgumentSafe(Helper.StateType(member)) ||
                    Helper.GetCopyBehavior(member) != CopyBehavior.Copy))
                {
                    reason = "primary constructor arguments must match stored immutable values with the default copy policy to avoid mutating the source";
                }
            }
            else if (!type.IsValueType && !hasGeneratedBase && type.BaseType is { } baseType &&
                !baseType.InstanceConstructors.Any(c => c.Parameters.Length == 0 && CanAccess(c, type)))
            {
                reason = "the base type needs an accessible parameterless constructor or generated deep-copy support";
            }
        }

        if (!type.IsValueType && hasGeneratedBase)
        {
            var parent = type.BaseType!;

            if (parent.DeclaringSyntaxReferences.Length == 0)
            {
                if (contextType == null || !parent.InstanceConstructors.Any(constructor =>
                    MatchesGraphConstructor(constructor, parent, contextType)))
                {
                    reason = "the base type must be rebuilt with graph-aware copy support";
                }
            }
            else if (!Helper.IsPartial(parent) || HasUnsupportedShape(parent, true, out _, compilation))
            {
                reason = "the opted-in base type must support generated deep copying";
            }
        }

        if (!type.IsValueType && (!hasGeneratedBase || copiesThroughRecordChain))
        {
            for (var parent = type.BaseType; parent != null && parent.SpecialType != SpecialType.System_Object; parent = parent.BaseType)
            {
                if (parent.DeclaringSyntaxReferences.Length == 0 || (!hasGeneratedBase && Helper.HasDeepCopyOptIn(parent)) ||
                    parent.GetMembers().OfType<IEventSymbol>().Any(e => !e.IsStatic) ||
                    parent.GetMembers().OfType<IFieldSymbol>().Any(f => !f.IsStatic && f.IsImplicitlyDeclared && f.AssociatedSymbol == null))
                {
                    reason = "the base hierarchy must opt into deep copying to preserve hidden state";
                }

                foreach (var inherited in Helper.GetOwnState(parent))
                {
                    if (StorageAccess.CanUse(inherited, compilation))
                    {
                        continue;
                    }

                    if (!CanAccess(inherited, type) || inherited is IFieldSymbol { IsReadOnly: true } ||
                        inherited is IPropertySymbol p && (p.GetMethod == null || p.SetMethod == null || !CanAccess(p.GetMethod, type) || !CanAccess(p.SetMethod, type)))
                    {
                        reason = "the base type must opt into deep copying to preserve inaccessible or readonly stored state";
                    }

                    if (type.GetMembers(inherited.Name).Any(m => m is IFieldSymbol || m is IPropertySymbol { IsOverride: false }))
                    {
                        reason = "the base type must opt into deep copying to preserve hidden state with duplicate names";
                    }
                }
            }
        }

        foreach (var member in Helper.GetCopiedState(type))
        {
            if (member is IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 } explicitProperty)
            {
                reason = $"explicit interface auto-property '{explicitProperty.Name}' requires an explicit backing field or a custom copy implementation";
                return true;
            }

            if (!SymbolEqualityComparer.Default.Equals(member.ContainingType, type) && !StorageAccess.CanUse(member, compilation))
            {
                if (member.DeclaredAccessibility == Accessibility.Private ||
                    member is IFieldSymbol { IsReadOnly: true } ||
                    member is IPropertySymbol { SetMethod: null } ||
                    member is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Private })
                {
                    reason = "the base type must opt into deep copying to preserve inaccessible or readonly stored state";
                }
            }

            var stateType = Helper.StateType(member);
            var behavior = Helper.GetCopyBehavior(member);

            if (behavior is < CopyBehavior.Copy or > CopyBehavior.Custom)
            {
                reason = member.Name + ": invalid copy policy";
                return true;
            }

            var unsupported = behavior == CopyBehavior.Copy ||
                stateType.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || stateType.IsRefLikeType
                ? UnsupportedType(stateType)
                : null;

            if (unsupported != null)
            {
                reason = member.Name + ": " + unsupported;
            }
        }

        var virtualStorageReason = UnsupportedVirtualStorage(type, compilation);

        if (virtualStorageReason != null)
        {
            reason = virtualStorageReason;
        }

        var stateIndex = 0;

        foreach (var member in Helper.GetCopiedState(type))
        {
            if (member is IPropertySymbol property && StorageAccess.CanUse(property, compilation) &&
                StorageAccess.HasNameCollision(type, property, stateIndex, compilation))
            {
                reason = "the generated physical storage helper name is reserved";
            }

            stateIndex++;
        }

        return reason.Length != 0;
    }

    private static bool HasMethodCollision(INamedTypeSymbol type, string name, ITypeSymbol? parameterType = null)
    {
        if (type.Name == name || type.TypeParameters.Any(parameter => parameter.Name == name))
        {
            return true;
        }

        foreach (var member in type.GetMembers(name))
        {
            if (!IsUserDeclared(member))
            {
                continue;
            }

            if (member is not IMethodSymbol method)
            {
                return true;
            }

            if (method.Arity == 0 && (parameterType == null
                ? method.Parameters.IsEmpty
                : method.Parameters.Length == 1 && IsByValueParameter(method.Parameters[0], parameterType)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasExplicitCopyImplementation(
        INamedTypeSymbol type,
        Compilation compilation,
        string interfaceName,
        string methodName)
    {
        var interfaceMethod = compilation.GetTypeByMetadataName(interfaceName)?.GetMembers(methodName).FirstOrDefault();

        return interfaceMethod != null &&
            type.FindImplementationForInterfaceMember(interfaceMethod) is IMethodSymbol
            {
                MethodKind: MethodKind.ExplicitInterfaceImplementation
            } implementation &&
            SymbolEqualityComparer.Default.Equals(implementation.ContainingType, type) &&
            IsUserDeclared(implementation);
    }

    private static bool MatchesGraphConstructor(IMethodSymbol constructor, INamedTypeSymbol owner, ITypeSymbol contextType) =>
        constructor.Parameters.Length == 2 &&
        IsByValueParameter(constructor.Parameters[0], owner) &&
        IsByValueParameter(constructor.Parameters[1], contextType);

    private static bool IsByValueParameter(IParameterSymbol parameter, ITypeSymbol type) =>
        parameter.RefKind == RefKind.None && SymbolEqualityComparer.Default.Equals(parameter.Type, type);

    private static string? UnsupportedVirtualStorage(INamedTypeSymbol type, Compilation compilation)
    {
        for (var current = type; current != null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is not IPropertySymbol { OverriddenProperty: not null } property)
                {
                    continue;
                }

                // An override can be computed while a more distant ancestor still
                // owns an independent auto-property slot. Inspect the entire chain.
                for (var ancestor = property.OverriddenProperty; ancestor != null; ancestor = ancestor.OverriddenProperty)
                {
                    if (!HasStoredProperty(ancestor))
                    {
                        continue;
                    }

                    if (StorageAccess.SupportsOwner(ancestor.ContainingType, compilation))
                    {
                        if (ancestor.ContainingType.DeclaringSyntaxReferences.Length != 0 &&
                            StorageAccess.CanUse(ancestor, compilation))
                        {
                            continue;
                        }

                        if (ancestor.ContainingType.DeclaringSyntaxReferences.Length == 0 &&
                            StorageAccess.HasCapability(ancestor.ContainingType))
                        {
                            continue;
                        }

                        return "the base type must be rebuilt with accessible physical backing storage support: " + ancestor.ToDisplayString();
                    }

                    return $"override '{property.ToDisplayString()}' may bypass the backing storage of '{ancestor.ToDisplayString()}'; " +
                        "use explicit backing fields or a custom copy implementation";
                }
            }
        }

        return null;
    }

    private static bool HasStoredProperty(IPropertySymbol property)
    {
        if (Helper.IsAutoProperty(property))
        {
            return true;
        }

        // Private backing fields are not necessarily imported from metadata.
        // Auto-property getter attributes remain visible on referenced assemblies.
        return property.ContainingType.DeclaringSyntaxReferences.Length == 0 &&
            property.DeclaringSyntaxReferences.Length == 0 &&
            property.GetMethod?.GetAttributes().Any(attribute => Helper.SymbolEquals(
                attribute.AttributeClass,
                "System.Runtime.CompilerServices.CompilerGeneratedAttribute")) == true;
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
        {
            return UnsupportedType(array.ElementType);
        }

        if (type.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || type.IsRefLikeType)
        {
            return "pointer and ref-like state cannot be deep copied";
        }

        if (type is not INamedTypeSymbol named)
        {
            return null;
        }

        var original = named.OriginalDefinition.ToDisplayString();
        var knownCollection = Helper.IsGenericCollection(original, out var kind);

        if (!knownCollection && (original.StartsWith("System.Collections.Immutable.", System.StringComparison.Ordinal) ||
            original.StartsWith("System.Collections.Frozen.", System.StringComparison.Ordinal)))
        {
            return "this immutable or frozen collection type is not supported; use a supported collection or a custom copy method";
        }

        if (kind is "ImmutableArray" or "ImmutableSet" or "ImmutableMap" or "FrozenSet" or "FrozenMap" &&
            !Helper.IsImmutableScalar(named.TypeArguments[0]))
        {
            return "immutable and frozen collections require immutable scalar keys or set elements; immutable arrays require immutable scalar elements";
        }

        if (Helper.IsTrulyPrimitive(type))
        {
            return null;
        }

        if (kind == "ConcurrentDictionary" && !named.GetMembers("Comparer").Any())
        {
            return "this target framework does not expose ConcurrentDictionary.Comparer";
        }

        if (knownCollection || named.IsTupleType ||
            original.StartsWith("System.Tuple<", System.StringComparison.Ordinal) ||
            original.StartsWith("System.ValueTuple<", System.StringComparison.Ordinal) ||
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            foreach (var argument in named.TypeArguments)
            {
                var unsupported = UnsupportedType(argument);

                if (unsupported != null)
                {
                    return unsupported;
                }
            }
        }

        return null;
    }

    internal static IEnumerable<ITypeSymbol> MissingOptIns(ITypeSymbol type)
    {
        if (Helper.IsTrulyPrimitive(type) || Helper.IsUri(type) || type is ITypeParameterSymbol ||
            type.SpecialType == SpecialType.System_Array)
        {
            yield break;
        }

        if (type is IArrayTypeSymbol array)
        {
            foreach (var missing in MissingOptIns(array.ElementType))
            {
                yield return missing;
            }

            yield break;
        }

        if (type is not INamedTypeSymbol named)
        {
            yield break;
        }

        var original = named.OriginalDefinition.ToDisplayString();

        if (Helper.IsGenericCollection(original, out _) || named.IsTupleType ||
            original.StartsWith("System.Tuple<", System.StringComparison.Ordinal) ||
            original.StartsWith("System.ValueTuple<", System.StringComparison.Ordinal) ||
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            foreach (var argument in named.TypeArguments)
            {
                foreach (var missing in MissingOptIns(argument))
                {
                    yield return missing;
                }
            }

            yield break;
        }

        if (!Helper.HasDeepCopyOptIn(named))
        {
            yield return named;
        }
    }
}
