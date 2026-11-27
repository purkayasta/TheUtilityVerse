// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy;

internal static class StorageAccess
{
    internal const string Capability = "UtilityVerse.Copy.PhysicalState";

    private const string GeneratedNamespace = "UtilityVerse.Copy.Generated";

    internal static bool RequiresAccessor(ISymbol member) =>
        member is IPropertySymbol property && (property.IsVirtual || property.IsOverride);

    internal static bool SupportsOwner(INamedTypeSymbol owner, Compilation compilation)
    {
        var runtimeVersion = compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly.Identity.Version.Major;

        if (runtimeVersion < 8 || compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.UnsafeAccessorAttribute") == null)
        {
            return false;
        }

        return runtimeVersion >= 9 || !Hierarchy(owner).Any(type => type.TypeArguments.Length != 0);
    }

    internal static bool CanUse(ISymbol member, Compilation compilation)
    {
        if (!RequiresAccessor(member) || member is not IPropertySymbol property ||
            !SupportsOwner(property.ContainingType, compilation) || BackingField(property) == null)
        {
            return false;
        }

        var host = AccessorHost(property, compilation);

        if (host == null)
        {
            return true;
        }
        var parameters = Hierarchy(host).SelectMany(type => type.TypeParameters).ToArray();

        return Hierarchy(host).All(Helper.IsPartial) &&
            parameters.Select(parameter => parameter.Name).Distinct().Count() == parameters.Length;
    }

    private static INamedTypeSymbol? AccessorHost(IPropertySymbol property, Compilation compilation)
    {
        if (!IsTypeAccessible(property.Type, compilation))
        {
            return property.ContainingType;
        }

        return compilation.IsSymbolAccessibleWithin(property.ContainingType, compilation.Assembly)
            ? null
            : property.ContainingType.ContainingType;
    }

    private static bool IsTypeAccessible(ITypeSymbol type, Compilation compilation)
    {
        if (type is IArrayTypeSymbol array)
        {
            return IsTypeAccessible(array.ElementType, compilation);
        }

        return type is not INamedTypeSymbol named ||
            compilation.IsSymbolAccessibleWithin(named, compilation.Assembly) &&
            named.TypeArguments.All(argument => IsTypeAccessible(argument, compilation));
    }

    internal static IFieldSymbol? BackingField(IPropertySymbol property) =>
        property.ContainingType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(field =>
            SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, property));

    internal static bool HasCapability(INamedTypeSymbol type) => type.InstanceConstructors.Any(constructor =>
        constructor.Parameters.Length == 2 &&
        SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, type) &&
        Helper.SymbolEquals(constructor.Parameters[1].Type, "UtilityVerse.Copy.CopyContext") &&
        constructor.GetAttributes().Any(attribute =>
            Helper.SymbolEquals(attribute.AttributeClass, "System.CodeDom.Compiler.GeneratedCodeAttribute") &&
            attribute.ConstructorArguments.Length == 2 &&
            attribute.ConstructorArguments[0].Value as string == Capability &&
            attribute.ConstructorArguments[1].Value as string == "1"));

    internal static string Name(INamedTypeSymbol copyingType, int index)
    {
        var identity = copyingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var encoded = string.Concat(identity.Select(character => char.IsLetterOrDigit(character)
            ? character.ToString()
            : "_" + ((int)character).ToString("X4")));

        return "__UtilityVerseStorage_" + encoded + "_" + index;
    }

    internal static string Reference(INamedTypeSymbol copyingType, IPropertySymbol property, int index, Compilation compilation)
    {
        var host = AccessorHost(property, compilation);
        var hostCount = host == null ? 0 : Hierarchy(host).Sum(type => type.Arity);
        var arguments = Hierarchy(property.ContainingType).SelectMany(type => type.TypeArguments)
            .Skip(hostCount).ToArray();
        var genericArguments = arguments.Length == 0
            ? ""
            : "<" + string.Join(", ", arguments.Select(Helper.TypeName)) + ">";

        var prefix = host == null ? "global::" + GeneratedNamespace : Helper.TypeName(host);

        return prefix + "." + Name(copyingType, index) + genericArguments + ".Field";
    }

    internal static bool HasNameCollision(INamedTypeSymbol copyingType, IPropertySymbol property, int index, Compilation compilation)
    {
        var host = AccessorHost(property, compilation);
        var arity = Hierarchy(property.ContainingType).Sum(type => type.Arity) -
            (host == null ? 0 : Hierarchy(host).Sum(type => type.Arity));
        var prefix = host == null ? GeneratedNamespace + "." : MetadataName(host) + "+";
        var metadataName = prefix + Name(copyingType, index) + (arity == 0 ? "" : "`" + arity);
        var existing = compilation.GetTypeByMetadataName(metadataName);

        return existing != null && SymbolEqualityComparer.Default.Equals(existing.ContainingAssembly, compilation.Assembly) &&
            existing.DeclaringSyntaxReferences.Any(reference =>
                !reference.SyntaxTree.FilePath.EndsWith(".g.cs", System.StringComparison.OrdinalIgnoreCase));
    }

    internal static void Emit(StringBuilder builder, INamedTypeSymbol copyingType, IPropertySymbol property, int index, Compilation compilation)
    {
        var original = property.OriginalDefinition;
        var owner = original.ContainingType;
        var parameters = Hierarchy(owner).SelectMany(type => type.TypeParameters).ToArray();
        var host = AccessorHost(property, compilation);
        var hostCount = host == null ? 0 : Hierarchy(host).Sum(type => type.Arity);
        var ownParameters = parameters.Skip(hostCount).ToArray();
        var names = new Dictionary<ITypeParameterSymbol, string>(SymbolEqualityComparer.Default);
        var usedNames = new HashSet<string>(parameters.Take(hostCount).Select(parameter => parameter.Name));

        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            var parameter = parameters[parameterIndex];
            var name = parameterIndex < hostCount ? Helper.Identifier(parameter.Name) : "TStorage" + parameterIndex;

            while (parameterIndex >= hostCount && !usedNames.Add(name))
            {
                name += "_";
            }

            names.Add(parameter, name);
        }

        var genericParameters = ownParameters.Length == 0
            ? ""
            : "<" + string.Join(", ", ownParameters.Select(parameter => names[parameter])) + ">";
        var field = BackingField(original)!;

        builder.AppendLine();
        string indent;
        string accessibility;

        if (host == null)
        {
            builder.AppendLine("namespace " + GeneratedNamespace);
            builder.AppendLine("{");
            indent = "    ";
            accessibility = "internal";
        }
        else
        {
            builder.Append(Helper.BeginType(host.OriginalDefinition, null, includeHeader: false));
            indent = Helper.MemberIndentation(host);
            accessibility = SymbolEqualityComparer.Default.Equals(host, property.ContainingType)
                ? (host.IsSealed ? "private" : "private protected")
                : property.ContainingType.DeclaredAccessibility switch
                {
                    Accessibility.Private => "private",
                    Accessibility.Protected or Accessibility.ProtectedAndInternal => "private protected",
                    _ => "internal"
                };
        }

        builder.AppendLine(indent + accessibility + " static class " + Name(copyingType, index) + genericParameters);

        foreach (var parameter in ownParameters)
        {
            var constraints = Constraints(parameter, names).ToArray();

            if (constraints.Length != 0)
            {
                builder.AppendLine(indent + "    where " + names[parameter] + " : " + string.Join(", ", constraints));
            }
        }

        builder.AppendLine(indent + "{");
        builder.AppendLine(indent + "    [global::System.Runtime.CompilerServices.UnsafeAccessor(");
        builder.AppendLine(indent + "        global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field,");
        builder.AppendLine(indent + "        Name = " + Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(field.Name, true) + ")]");
        builder.AppendLine(indent + "    internal static extern ref " + Helper.TypeName(original.Type, names) + " Field(");
        builder.AppendLine(indent + "        " + Helper.TypeName(owner, names) + " source);");
        builder.AppendLine(indent + "}");

        if (host == null)
        {
            builder.AppendLine("}");
        }
        else
        {
            builder.Append(Helper.EndType(host));
        }
    }

    private static string MetadataName(INamedTypeSymbol type)
    {
        var prefix = type.ContainingType == null
            ? (type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString() + ".")
            : MetadataName(type.ContainingType) + "+";

        return prefix + type.MetadataName;
    }

    private static IEnumerable<INamedTypeSymbol> Hierarchy(INamedTypeSymbol owner)
    {
        if (owner.ContainingType != null)
        {
            foreach (var parent in Hierarchy(owner.ContainingType))
            {
                yield return parent;
            }
        }

        yield return owner;
    }

    private static IEnumerable<string> Constraints(
        ITypeParameterSymbol parameter,
        IReadOnlyDictionary<ITypeParameterSymbol, string> names)
    {
        if (parameter.HasUnmanagedTypeConstraint)
        {
            yield return "unmanaged";
        }
        else if (parameter.HasValueTypeConstraint)
        {
            yield return "struct";
        }
        else if (parameter.HasReferenceTypeConstraint)
        {
            yield return parameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class";
        }
        else if (parameter.HasNotNullConstraint)
        {
            yield return "notnull";
        }

        foreach (var constraint in parameter.ConstraintTypes)
        {
            yield return Helper.TypeName(constraint, names);
        }

        if (parameter.HasConstructorConstraint)
        {
            yield return "new()";
        }
    }
}
