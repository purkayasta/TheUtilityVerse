// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Generators;

internal static class ShallowCopyGenerator
{
    internal static string? Generate(INamedTypeSymbol type)
    {
        if (!Helper.IsPartial(type) || type.IsStatic || type.IsRefLikeType) return null;
        var name = Helper.LocalName(type);
        var hidesBase = type.BaseType != null && Helper.HasShallowCopyOptIn(type.BaseType);
        return Helper.BeginType(type, null) +
            "[global::System.CodeDom.Compiler.GeneratedCode(\"ShallowCopyGenerator\", \"1.0\")]\n" +
            $"public {(hidesBase ? "new " : "")}{name} ShallowCopy() => " +
            (type.IsValueType ? "this;\n" : $"({name})MemberwiseClone();\n") + Helper.EndType(type);
    }
}
