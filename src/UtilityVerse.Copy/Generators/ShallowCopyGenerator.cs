// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Text;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Generators;

internal static class ShallowCopyGenerator
{
    internal static string? Generate(INamedTypeSymbol type)
    {
        if (!Helper.IsPartial(type) || type.IsStatic || type.IsRefLikeType)
        {
            return null;
        }

        var name = Helper.LocalName(type);
        var hidesBase = type.BaseType != null && Helper.HasShallowCopyOptIn(type.BaseType);
        var indent = Helper.MemberIndentation(type);
        var expression = type.IsValueType ? "this" : $"({name})MemberwiseClone()";
        var sb = new StringBuilder(Helper.BeginType(type, null));

        sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"ShallowCopyGenerator\", \"1.0\")]");
        sb.AppendLine($"{indent}public {(hidesBase ? "new " : "")}{name} ShallowCopy() => {expression};");
        sb.Append(Helper.EndType(type));

        return sb.ToString();
    }
}
