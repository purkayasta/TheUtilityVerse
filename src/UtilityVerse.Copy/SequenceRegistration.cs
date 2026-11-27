// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy;

internal static class SequenceRegistration
{
    internal static ITypeSymbol[] GetElementTypes(
        INamedTypeSymbol owner,
        IEnumerable<INamedTypeSymbol> models,
        Compilation compilation)
    {
        var elements = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        void Visit(ITypeSymbol type)
        {
            if (!visited.Add(type))
            {
                return;
            }

            if (type is IArrayTypeSymbol array)
            {
                Visit(array.ElementType);
            }
            else if (type is INamedTypeSymbol named)
            {
                if (Helper.IsGenericCollection(named.OriginalDefinition.ToDisplayString(), out var kind) &&
                    kind == "Sequence")
                {
                    elements.Add(named.TypeArguments[0].WithNullableAnnotation(NullableAnnotation.NotAnnotated));
                }

                foreach (var argument in named.TypeArguments)
                {
                    Visit(argument);
                }
            }
        }

        // Base constructors hydrate before derived state. Register the complete
        // hierarchy before entering any constructor, regardless of member order.
        for (var current = owner; current != null; current = current.BaseType)
        {
            foreach (var member in Helper.GetCopiedState(current))
            {
                Visit(Helper.StateType(member));
            }
        }

        if (elements.Count == 0)
        {
            return System.Array.Empty<ITypeSymbol>();
        }

        var views = elements.ToArray();

        foreach (var model in models)
        {
            if (model.IsStatic || HasTypeParameters(model) || !model.IsReferenceType ||
                !compilation.IsSymbolAccessibleWithin(model, owner))
            {
                continue;
            }

            if (views.Any(view => view is ITypeParameterSymbol ||
                compilation.ClassifyCommonConversion(model, view).IsImplicit))
            {
                elements.Add(model);
            }
        }

        return elements.OrderBy(Helper.TypeName).ToArray();
    }

    private static bool HasTypeParameters(INamedTypeSymbol type) =>
        type.TypeParameters.Length != 0 ||
        (type.ContainingType != null && HasTypeParameters(type.ContainingType));
}
