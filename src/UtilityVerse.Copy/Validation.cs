// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UtilityVerse.Copy.Generators;

namespace UtilityVerse.Copy;

internal static class Validation
{
    internal static INamedTypeSymbol? FindFileLocalDeclaration(INamedTypeSymbol type)
    {
        for (var current = type; current != null; current = current.ContainingType)
        {
            if (current.IsFileLocal)
            {
                return current;
            }
        }

        return null;
    }

    internal static string FileLocalReason(INamedTypeSymbol declaration) =>
        $"Remove the 'file' modifier from '{declaration.Name}'. Generated copies cannot access file-local types";

    internal static bool HasUnsupportedShape(
        INamedTypeSymbol type,
        bool deep,
        out string reason,
        Compilation compilation,
        Action<ISymbol, string>? reportMemberIssue = null,
        System.Threading.CancellationToken cancellationToken = default)
    {
        reason = "";

        if (FindFileLocalDeclaration(type) is { } fileLocal)
        {
            reason = FileLocalReason(fileLocal);
            return true;
        }

        if (deep && !CompilerDepth.IsSupported(type, cancellationToken))
        {
            reason = "Type traversal exceeds the compiler safety limit of 100. Simplify nested type state or use a custom copy policy";
            return true;
        }

        var copyMethodName = deep ? "DeepCopy" : "ShallowCopy";

        if (type.IsStatic || type.IsRefLikeType)
        {
            reason = "Generated copies do not support static or ref-like types. Remove the opt-in and use an explicit copy method if needed";
            return true;
        }

        for (var parent = type.ContainingType; parent != null; parent = parent.ContainingType)
        {
            if (!Helper.IsPartial(parent))
            {
                var name = parent.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                reason = $"Add 'partial' to the containing type '{name}' to generate {copyMethodName}()";
                return true;
            }
        }

        if (HasMethodCollision(type, copyMethodName))
        {
            reason = $"A declaration conflicts with the generated method '{copyMethodName}()'. Rename it or remove the opt-in if your own copy method is intentional";
            return true;
        }

        if (!deep)
        {
            return reason.Length != 0;
        }

        var contextType = compilation.GetTypeByMetadataName("UtilityVerse.Copy.CopyContext");

        if (contextType != null && HasMethodCollision(type, "__UtilityVerseDeepCopy", contextType))
        {
            reason = "Rename the declaration that conflicts with the generated helper '__UtilityVerseDeepCopy(CopyContext)'";
            return true;
        }

        if (HasExplicitCopyImplementation(type, compilation, "UtilityVerse.Copy.IDeepCopyable", "Copy"))
        {
            reason = "Remove the explicit implementation of 'IDeepCopyable.Copy' to use the generated implementation, or remove the deep-copy opt-in to keep your own";
            return true;
        }

        if (type.IsValueType && DeepCopyGenerator.CanUnbox(compilation) &&
            HasExplicitCopyImplementation(type, compilation, "UtilityVerse.Copy.IBoxedDeepCopyable", "CopyBoxed"))
        {
            reason = "Remove the explicit implementation of 'IBoxedDeepCopyable.CopyBoxed' to use the generated implementation, or remove the deep-copy opt-in to keep your own";
            return true;
        }

        if (type.GetMembers("__UtilityVerseSequenceTypes").Any(IsUserDeclared) ||
            type.GetMembers("__UtilityVerseRegisterSequenceTypes").Any(IsUserDeclared))
        {
            reason = "Rename your declaration of '__UtilityVerseSequenceTypes' or '__UtilityVerseRegisterSequenceTypes'. These names are reserved for generated registration";
        }

        var hasGeneratedBase = Helper.HasGeneratedDeepBase(type);
        var copiesThroughRecordChain = Helper.UsesSynthesizedRecordCopy(type);

        if (type.GetMembers().OfType<IEventSymbol>().Any(e => !e.IsStatic) ||
            type.GetMembers().OfType<IFieldSymbol>().Any(f => !f.IsStatic && f.IsImplicitlyDeclared && f.AssociatedSymbol == null))
        {
            reason = "Instance event subscriptions or captured primary-constructor state need an explicit copy implementation. " +
                "For captured parameters, consider storing their values in named instance fields or auto-properties";
        }

        if (contextType != null && type.InstanceConstructors.Any(constructor =>
            MatchesGraphConstructor(constructor, type, contextType) && IsUserDeclared(constructor)))
        {
            reason = $"The generated copy constructor signature ({type.Name}, CopyContext) is reserved. Change your constructor signature or keep an explicit copy implementation";
        }

        if (type.IsRecord && !type.IsValueType)
        {
            for (var record = type; record is { IsRecord: true }; record = copiesThroughRecordChain ? record.BaseType : null)
            {
                if (record.DeclaringSyntaxReferences.Length == 0 || record.InstanceConstructors.Any(constructor =>
                    !constructor.IsImplicitlyDeclared && constructor.Parameters.Length == 1 &&
                    IsByValueParameter(constructor.Parameters[0], record)))
                {
                    reason = $"Record '{record.Name}' may use a custom record copy constructor that could mutate the source. " +
                        "Use the compiler-generated record copy constructor or implement the deep copy explicitly";
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
                    reason = "A primary constructor cannot chain to the generated base copy constructor. Use an ordinary constructor or implement the deep copy explicitly";
                }

                if (primary.Parameters.Any(p => p.RefKind != RefKind.None || !Helper.IsConstructorArgumentSafe(p.Type) ||
                    Helper.MatchParameter(type, p) is not { } member || !Helper.IsConstructorArgumentSafe(Helper.StateType(member)) ||
                    Helper.GetCopyBehavior(member) != CopyBehavior.Copy))
                {
                    reason = "Use an ordinary constructor or make the primary constructor arguments match stored immutable values with the default Copy policy";
                }
            }
            else if (!type.IsValueType && !hasGeneratedBase && type.BaseType is { } baseType &&
                !baseType.InstanceConstructors.Any(c => c.Parameters.Length == 0 && CanAccess(c, type)))
            {
                reason = $"Give base type '{baseType.Name}' an accessible parameterless constructor or opt it into generated deep copying with [DeepCopy] or IDeepCopy";
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
                    reason = $"Rebuild base type '{parent.Name}' with the current UtilityVerse.Copy generator, or implement the deep copy explicitly";
                }
            }
            else if (!Helper.IsPartial(parent) || HasUnsupportedShape(parent, true, out _, compilation))
            {
                reason = $"Fix the deep-copy diagnostics for base type '{parent.Name}' before generating a copy for this type";
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
                    reason = $"Opt base type '{parent.Name}' into deep copying to preserve its hidden state. Use an explicit copy implementation if the base type cannot be changed";
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
                        reason = $"Opt base type '{parent.Name}' into deep copying to preserve inaccessible or readonly stored state such as '{inherited.Name}'";
                    }

                    if (type.GetMembers(inherited.Name).Any(m => m is IFieldSymbol || m is IPropertySymbol { IsOverride: false }))
                    {
                        reason = $"Opt base type '{parent.Name}' into deep copying to preserve its hidden member '{inherited.Name}', or rename the conflicting member";
                    }
                }
            }
        }

        var virtualStorageReason = UnsupportedVirtualStorage(type, compilation);

        if (virtualStorageReason != null)
        {
            reason = virtualStorageReason;
        }

        var hasMemberIssue = false;
        var stateIndex = 0;

        foreach (var member in Helper.GetCopiedState(type))
        {
            var memberReason = UnsupportedMember(type, member, stateIndex, compilation);
            stateIndex++;

            if (memberReason == null)
            {
                continue;
            }

            if (reportMemberIssue == null)
            {
                reason = memberReason;
                return true;
            }

            hasMemberIssue = true;
            reportMemberIssue(member, memberReason);
        }

        return hasMemberIssue || reason.Length != 0;
    }

    private static string? UnsupportedMember(INamedTypeSymbol type, ISymbol member, int stateIndex, Compilation compilation)
    {
        if (member is IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 } explicitProperty)
        {
            return $"Use an explicit backing field for explicit interface auto-property '{explicitProperty.Name}', or implement the deep copy explicitly";
        }

        var stateType = Helper.StateType(member);
        var behavior = Helper.GetCopyBehavior(member);

        if (behavior is < CopyBehavior.Copy or > CopyBehavior.Custom)
        {
            return $"Member '{member.Name}' has an invalid copy policy. Choose Copy, Share, Reset, or Custom to match the intended copy behavior";
        }

        if (behavior == CopyBehavior.Copy ||
            stateType.TypeKind is TypeKind.Pointer or TypeKind.FunctionPointer || stateType.IsRefLikeType)
        {
            var unsupported = UnsupportedType(stateType);

            if (unsupported != null)
            {
                return $"Member '{member.Name}' of type '{stateType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}'. {unsupported}";
            }
        }

        if (member is IPropertySymbol property && StorageAccess.CanUse(property, compilation) &&
            StorageAccess.HasNameCollision(type, property, stateIndex, compilation))
        {
            return $"Rename the declaration that conflicts with generated storage for '{member.Name}'. The generated helper name is reserved";
        }

        return null;
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

                        return $"Base property '{ancestor.ToDisplayString()}' needs its type rebuilt with accessible physical backing storage support. " +
                            "Rebuild the base with the current generator or use explicit backing fields";
                    }

                    return $"Override '{property.ToDisplayString()}' may bypass the backing storage of '{ancestor.ToDisplayString()}'. " +
                        "Use explicit backing fields or implement the deep copy explicitly";
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
            return "Implement the deep copy explicitly for pointer or ref-like state";
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
            return "This immutable or frozen collection type is unsupported. Use a supported collection or apply an explicit copy policy";
        }

        if (kind is "ImmutableArray" or "ImmutableSet" or "ImmutableMap" or "FrozenSet" or "FrozenMap" &&
            !Helper.IsImmutableScalar(named.TypeArguments[0]))
        {
            return kind == "ImmutableArray"
                ? "Immutable arrays require immutable scalar elements. Choose a supported element type or apply an explicit copy policy"
                : "Immutable and frozen collections require immutable scalar keys or set elements. Choose supported types or apply an explicit copy policy";
        }

        if (Helper.IsTrulyPrimitive(type))
        {
            return null;
        }

        if (kind == "ConcurrentDictionary" && !named.GetMembers("Comparer").Any())
        {
            return "The target framework does not expose ConcurrentDictionary.Comparer. Use a supported target or a custom copier that preserves the comparer";
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

    internal static IEnumerable<INamedTypeSymbol> MissingOptIns(ITypeSymbol type)
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
