// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Linq;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy;

internal static class DepthConfiguration
{
    internal static bool IsConfiguration(AttributeData attribute) =>
        attribute.AttributeClass?.ToDisplayString() is "UtilityVerse.Copy.DeepCopy" or "UtilityVerse.Copy.CopyDepthAttribute";

    internal static int GetDepth(INamedTypeSymbol type)
    {
        var attributes = type.GetAttributes();
        var attribute = attributes.FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "UtilityVerse.Copy.DeepCopy") ??
            attributes.FirstOrDefault(item => item.AttributeClass?.ToDisplayString() == "UtilityVerse.Copy.CopyDepthAttribute");
        var depth = attribute?.ConstructorArguments.FirstOrDefault().Value as int? ?? 5;
        return depth is >= 1 and <= 100 ? depth : 5;
    }
}
