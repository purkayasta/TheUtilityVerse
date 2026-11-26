// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Generators;

internal static class DeepCopyGenerator
{
    internal static string? Generate(INamedTypeSymbol type)
    {
        if (!Helper.IsPartial(type) || type.IsStatic || type.IsRefLikeType) return null;
        var name = Helper.LocalName(type);
        var context = "global::UtilityVerse.Copy.CopyContext";
        var hasBase = Helper.HasGeneratedDeepBase(type);
        var sb = new StringBuilder(Helper.BeginType(type, "global::UtilityVerse.Copy.IDeepCopyable"));
        sb.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"DeepCopyGenerator\", \"1.0\")]");
        sb.AppendLine($"public {(hasBase ? "new " : "")}{name} DeepCopy() => new {context}().Copy(this);");
        sb.AppendLine($"object global::UtilityVerse.Copy.IDeepCopyable.Copy({context} context) => __UtilityVerseDeepCopy(context);");
        var modifier = hasBase ? "protected override" : type.IsValueType || type.IsSealed ? "private" : "protected virtual";
        if (type.IsAbstract)
        {
            sb.AppendLine($"protected {(hasBase ? "abstract override" : "abstract")} object __UtilityVerseDeepCopy({context} context);");
        }
        else
        {
            sb.AppendLine($"{modifier} object __UtilityVerseDeepCopy({context} context) {{");
            if (!type.IsValueType && !type.IsSealed)
                sb.AppendLine($"if (GetType() != typeof({name})) throw new global::System.NotSupportedException(\"The runtime derived type must opt into deep copying.\");");
            sb.AppendLine($"return new {name}(this, context); }}");
        }

        var constructor = Helper.CopyConstructorTarget(type);
        var chain = type.IsRecord && !type.IsValueType ? "this(source)" :
            hasBase ? "base(source, context)" :
            type.IsValueType ? "this()" :
            constructor == null ? "base()" : "this(" + string.Join(", ", constructor.Parameters.Select(p => "source." + Helper.Identifier(Helper.MatchParameter(type, p)!.Name))) + ")";
        if (!type.IsValueType && constructor is { IsImplicitlyDeclared: true })
            sb.AppendLine($"{(type.IsAbstract ? "protected" : "public")} {Helper.Identifier(type.Name)}() {{ }}");
        if (Helper.HasRequiredState(type))
            sb.AppendLine("[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        sb.AppendLine($"{(type.IsValueType || type.IsSealed ? "private" : "protected")} {Helper.Identifier(type.Name)}({name} source, {context} context) : {chain} {{");
        if (!type.IsValueType && !hasBase) sb.AppendLine("context.Register(source, this);");
        foreach (var member in Helper.GetCopiedState(type))
        {
            sb.AppendLine($"this.{Helper.Identifier(member.Name)} = {CopyExpression(Helper.StateType(member), "source." + Helper.Identifier(member.Name), "context")};");
        }
        sb.AppendLine("}");
        sb.Append(Helper.EndType(type));
        return sb.ToString();
    }

    internal static string CopyExpression(ITypeSymbol type, string expression, string context, int depth = 0)
    {
        if (Helper.IsTrulyPrimitive(type)) return expression;
        var value = "value" + depth;
        var nestedContext = "context" + depth;
        if (type is IArrayTypeSymbol array)
            return $"{context}.CopyArray({expression}, ({value}, {nestedContext}) => {CopyExpression(array.ElementType, value, nestedContext, depth + 1)})!";
        if (type is INamedTypeSymbol named)
        {
            if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return $"({expression}.HasValue ? ({Helper.TypeName(type)})({CopyExpression(named.TypeArguments[0], expression + ".Value", context, depth + 1)}) : default)";
            if (named.IsTupleType)
                return "(" + string.Join(", ", named.TupleElements.Select(e => CopyExpression(e.Type, expression + "." + Helper.Identifier(e.Name), context, depth + 1))) + ")";
            var original = named.OriginalDefinition.ToDisplayString();
            if (Helper.IsGenericCollection(original, out var kind))
            {
                var element = named.TypeArguments[0];
                if (kind == "Sequence")
                    return $"({Helper.TypeName(type)}){context}.CopySequence({expression}, ({value}, {nestedContext}) => {CopyExpression(element, value, nestedContext, depth + 1)})!";
                var key = named.TypeArguments[0];
                var item = named.TypeArguments[1];
                var keyName = "key" + depth;
                var itemName = "item" + depth;
                var concrete = Helper.TypeName(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
                var insert = kind == "Dictionary" ? "copy.Add(key, item)" : "{ if (!copy.TryAdd(key, item)) throw new global::System.InvalidOperationException(\"Deep copying produced duplicate keys.\"); }";
                return $"{context}.CopyMap<{concrete}, {Helper.TypeName(key)}, {Helper.TypeName(item)}>({expression}, source => new {concrete}(source.Comparer), (copy, key, item) => {insert}, ({keyName}, {nestedContext}) => {CopyExpression(key, keyName, nestedContext, depth + 1)}, ({itemName}, {nestedContext}) => {CopyExpression(item, itemName, nestedContext, depth + 1)})!";
            }
            if (original.StartsWith("System.Tuple<", System.StringComparison.Ordinal))
            {
                var tupleName = Helper.TypeName(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
                var args = named.TypeArguments.Select((argument, i) => CopyExpression(argument, value + (i == 7 ? ".Rest" : ".Item" + (i + 1)), context, depth + 1));
                return $"{context}.CopyImmutable({expression}, {value} => new {tupleName}({string.Join(", ", args)}))!";
            }
            if (original.StartsWith("System.ValueTuple<", System.StringComparison.Ordinal))
                return $"new {Helper.TypeName(type)}(" + string.Join(", ", named.TypeArguments.Select((argument, i) => CopyExpression(argument, expression + (i == 7 ? ".Rest" : ".Item" + (i + 1)), context, depth + 1))) + ")";
        }
        return $"{context}.Copy({expression})";
    }
}
