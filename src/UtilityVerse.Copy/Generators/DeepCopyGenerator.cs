// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Generators;

internal static class DeepCopyGenerator
{
    internal static string? Generate(
        INamedTypeSymbol type,
        Compilation compilation,
        IEnumerable<INamedTypeSymbol> models)
    {
        if (!Helper.IsPartial(type) || type.IsStatic || type.IsRefLikeType)
        {
            return null;
        }

        var name = Helper.LocalName(type);
        var context = "global::UtilityVerse.Copy.CopyContext";
        const string boxedInterface = "global::UtilityVerse.Copy.IBoxedDeepCopyable";
        var supportsBoxCycles = type.IsValueType && CanUnbox(compilation);
        var hasBase = Helper.HasGeneratedDeepBase(type);
        var sequenceTypes = SequenceRegistration.GetElementTypes(type, models, compilation);
        var states = Helper.GetCopiedState(type)
            .Select((member, index) => new StoredState(member, index,
                StorageAccess.CanUse(member, compilation) && member is IPropertySymbol property
                    ? StorageAccess.Reference(type, property, index, compilation)
                    : null))
            .ToArray();
        var indent = Helper.MemberIndentation(type);
        var bodyIndent = indent + "    ";
        var copyInterface = supportsBoxCycles ? boxedInterface : "global::UtilityVerse.Copy.IDeepCopyable";
        var sb = new StringBuilder(Helper.BeginType(type, copyInterface));

        sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"DeepCopyGenerator\", \"1.0\")]");
        sb.AppendLine($"{indent}public {(hasBase ? "new " : "")}{name} DeepCopy() => new {context}().Copy(this);");
        sb.AppendLine();
        sb.AppendLine($"{indent}object global::UtilityVerse.Copy.IDeepCopyable.Copy({context} context)");
        sb.AppendLine($"{indent}{{");

        if (sequenceTypes.Length != 0)
        {
            sb.AppendLine($"{bodyIndent}_ = __UtilityVerseSequenceTypes;");
            sb.AppendLine();
        }

        sb.AppendLine($"{bodyIndent}return context.CopyGenerated(this, static (source, copyContext) => source.__UtilityVerseDeepCopy(copyContext));");
        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        if (supportsBoxCycles)
        {
            sb.AppendLine($"{indent}object {boxedInterface}.CopyBoxed(object source, {context} context)");
            sb.AppendLine($"{indent}{{");

            if (sequenceTypes.Length != 0)
            {
                sb.AppendLine($"{bodyIndent}_ = __UtilityVerseSequenceTypes;");
                sb.AppendLine();
            }

            sb.AppendLine($"{bodyIndent}return context.CopyGenerated(source, static (box, copyContext) =>");
            sb.AppendLine($"{bodyIndent}{{");
            sb.AppendLine($"{bodyIndent}    object destination = default({name});");
            sb.AppendLine($"{bodyIndent}    copyContext.Register(box, destination);");
            sb.AppendLine();
            sb.AppendLine($"{bodyIndent}    var value = new {name}(({name})box, copyContext);");
            sb.AppendLine($"{bodyIndent}    global::System.Runtime.CompilerServices.Unsafe.Unbox<{name}>(destination) = value;");
            sb.AppendLine();
            sb.AppendLine($"{bodyIndent}    return destination;");
            sb.AppendLine($"{bodyIndent}}});");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        if (sequenceTypes.Length != 0)
        {
            sb.AppendLine($"{indent}private static readonly bool __UtilityVerseSequenceTypes = __UtilityVerseRegisterSequenceTypes();");
            sb.AppendLine();
            sb.AppendLine($"{indent}private static bool __UtilityVerseRegisterSequenceTypes()");
            sb.AppendLine($"{indent}{{");

            foreach (var elementType in sequenceTypes)
            {
                sb.AppendLine($"{bodyIndent}{context}.RegisterSequenceType<{Helper.TypeName(elementType)}>();");
            }

            sb.AppendLine();
            sb.AppendLine($"{bodyIndent}return true;");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        var modifier = "protected virtual";

        if (hasBase)
        {
            modifier = "protected override";
        }
        else if (type.IsValueType || type.IsSealed)
        {
            modifier = "private";
        }

        if (type.IsAbstract)
        {
            sb.AppendLine($"{indent}protected {(hasBase ? "abstract override" : "abstract")} object __UtilityVerseDeepCopy({context} context);");
        }
        else
        {
            sb.AppendLine($"{indent}{modifier} object __UtilityVerseDeepCopy({context} context)");
            sb.AppendLine($"{indent}{{");

            if (!type.IsValueType && !type.IsSealed)
            {
                sb.AppendLine($"{bodyIndent}if (GetType() != typeof({name}))");
                sb.AppendLine($"{bodyIndent}{{");
                sb.AppendLine($"{bodyIndent}    throw new global::System.NotSupportedException(\"The runtime derived type must opt into deep copying.\");");
                sb.AppendLine($"{bodyIndent}}}");
                sb.AppendLine();
            }

            sb.AppendLine($"{bodyIndent}return new {name}(this, context);");
            sb.AppendLine($"{indent}}}");
        }

        sb.AppendLine();

        if (!type.IsValueType && type.InstanceConstructors.Any(c => c.IsImplicitlyDeclared && c.Parameters.Length == 0))
        {
            sb.AppendLine($"{indent}{(type.IsAbstract ? "protected" : "public")} {Helper.Identifier(type.Name)}()");
            sb.AppendLine($"{indent}{{");
            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        if (Helper.HasRequiredState(type))
        {
            sb.AppendLine($"{indent}[global::System.Diagnostics.CodeAnalysis.SetsRequiredMembers]");
        }

        if (states.Any(state => state.Accessor != null &&
            SymbolEqualityComparer.Default.Equals(state.Member.ContainingType, type)))
        {
            sb.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"{StorageAccess.Capability}\", \"1\")]");
        }

        var chain = ConstructorInitializer(type, hasBase, states);

        sb.AppendLine($"{indent}{(type.IsValueType || type.IsSealed ? "private" : "protected")} {Helper.Identifier(type.Name)}({name} source, {context} context)");
        sb.AppendLine($"{bodyIndent}: {chain}");
        sb.AppendLine($"{indent}{{");

        if (!type.IsValueType && !hasBase)
        {
            sb.AppendLine($"{bodyIndent}context.Register(source, this);");
            sb.AppendLine();
        }

        foreach (var state in states)
        {
            var expression = CopyExpression(Helper.StateType(state.Member), state.Access("source"), "context", indentation: bodyIndent);

            sb.AppendLine($"{bodyIndent}{state.Access("this")} = {expression};");
        }

        sb.AppendLine($"{indent}}}");
        sb.Append(Helper.EndType(type));

        foreach (var state in states)
        {
            if (state.Accessor != null && state.Member is IPropertySymbol property)
            {
                StorageAccess.Emit(sb, type, property, state.Index, compilation);
            }
        }

        return sb.ToString();
    }

    private static bool CanUnbox(Compilation compilation) =>
        compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.Unsafe")?.GetMembers("Unbox")
            .OfType<IMethodSymbol>().Any(method => method is
            {
                DeclaredAccessibility: Accessibility.Public,
                IsStatic: true,
                ReturnsByRef: true,
                Arity: 1,
                Parameters.Length: 1
            } && method.Parameters[0].Type.SpecialType == SpecialType.System_Object) == true;

    private static string ConstructorInitializer(INamedTypeSymbol type, bool hasBase, StoredState[] states)
    {
        if (type.IsRecord && !type.IsValueType)
        {
            return "this(source)";
        }

        var primaryConstructor = Helper.PrimaryConstructor(type);

        if (primaryConstructor != null)
        {
            var arguments = primaryConstructor.Parameters.Select(parameter =>
                states.First(state => SymbolEqualityComparer.Default.Equals(
                    state.Member, Helper.MatchParameter(type, parameter))).Access("source"));

            return "this(" + string.Join(", ", arguments) + ")";
        }

        if (hasBase)
        {
            return "base(source, context)";
        }

        return type.IsValueType ? "this()" : "base()";
    }

    private readonly struct StoredState
    {
        internal ISymbol Member { get; }

        internal int Index { get; }

        internal string? Accessor { get; }

        internal StoredState(ISymbol member, int index, string? accessor)
        {
            Member = member;
            Index = index;
            Accessor = accessor;
        }

        internal string Access(string receiver) => Accessor == null
            ? receiver + "." + Helper.Identifier(Member.Name)
            : Accessor + "(" + receiver + ")";
    }

    internal static string CopyExpression(
        ITypeSymbol type,
        string expression,
        string context,
        int depth = 0,
        string indentation = "")
    {
        if (Helper.IsTrulyPrimitive(type))
        {
            return expression;
        }

        var value = "value" + depth;
        var nestedContext = "context" + depth;
        var argumentIndent = indentation + "    ";

        if (type is IArrayTypeSymbol array)
        {
            var element = CopyExpression(array.ElementType, value, nestedContext, depth + 1, argumentIndent);

            return $"{context}.CopyArray(\n" +
                $"{argumentIndent}{expression},\n" +
                $"{argumentIndent}({value}, {nestedContext}) => {element})!";
        }

        if (type is INamedTypeSymbol named)
        {
            if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var underlying = CopyExpression(
                    named.TypeArguments[0], expression + ".Value", context, depth + 1, argumentIndent);

                return $"({expression}.HasValue\n" +
                    $"{argumentIndent}? ({Helper.TypeName(type)})({underlying})\n" +
                    $"{argumentIndent}: default)";
            }

            if (named.IsTupleType)
            {
                var elements = named.TupleElements.Select(element => CopyExpression(
                    element.Type,
                    expression + "." + Helper.Identifier(element.Name),
                    context,
                    depth + 1,
                    argumentIndent));

                return "(\n" + argumentIndent + string.Join(",\n" + argumentIndent, elements) + ")";
            }

            var original = named.OriginalDefinition.ToDisplayString();

            if (Helper.IsGenericCollection(original, out var kind))
            {
                if (kind == "Sequence")
                {
                    var element = CopyExpression(
                        named.TypeArguments[0], value, nestedContext, depth + 1, argumentIndent);

                    return $"({Helper.TypeName(type)}){context}.CopySequence(\n" +
                        $"{argumentIndent}{expression},\n" +
                        $"{argumentIndent}({value}, {nestedContext}) => {element})!";
                }

                var key = named.TypeArguments[0];
                var item = named.TypeArguments[1];
                var keyName = "key" + depth;
                var itemName = "item" + depth;
                var concrete = Helper.TypeName(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
                var copiedKey = CopyExpression(key, keyName, nestedContext, depth + 1, argumentIndent);
                var copiedItem = CopyExpression(item, itemName, nestedContext, depth + 1, argumentIndent);
                var insert = "copy.Add(key, item)";

                if (kind != "Dictionary")
                {
                    insert = "\n" + argumentIndent + "{\n" +
                        argumentIndent + "    if (!copy.TryAdd(key, item))\n" +
                        argumentIndent + "    {\n" +
                        argumentIndent + "        throw new global::System.InvalidOperationException(\"Deep copying produced duplicate keys.\");\n" +
                        argumentIndent + "    }\n" +
                        argumentIndent + "}";
                }

                return $"{context}.CopyMap<{concrete}, {Helper.TypeName(key)}, {Helper.TypeName(item)}>(\n" +
                    $"{argumentIndent}{expression},\n" +
                    $"{argumentIndent}source => new {concrete}(source.Comparer),\n" +
                    $"{argumentIndent}(copy, key, item) => {insert},\n" +
                    $"{argumentIndent}({keyName}, {nestedContext}) => {copiedKey},\n" +
                    $"{argumentIndent}({itemName}, {nestedContext}) => {copiedItem})!";
            }

            if (original.StartsWith("System.Tuple<", System.StringComparison.Ordinal))
            {
                var tupleName = Helper.TypeName(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
                var elements = named.TypeArguments.Select((argument, index) => CopyExpression(
                    argument,
                    value + (index == 7 ? ".Rest" : ".Item" + (index + 1)),
                    context,
                    depth + 1,
                    argumentIndent));

                return $"{context}.CopyImmutable(\n" +
                    $"{argumentIndent}{expression},\n" +
                    $"{argumentIndent}{value} => new {tupleName}(\n" +
                    argumentIndent + string.Join(",\n" + argumentIndent, elements) + "))!";
            }

            if (original.StartsWith("System.ValueTuple<", System.StringComparison.Ordinal))
            {
                var elements = named.TypeArguments.Select((argument, index) => CopyExpression(
                    argument,
                    expression + (index == 7 ? ".Rest" : ".Item" + (index + 1)),
                    context,
                    depth + 1,
                    argumentIndent));

                return $"new {Helper.TypeName(type)}(\n" +
                    argumentIndent + string.Join(",\n" + argumentIndent, elements) + ")";
            }
        }

        return $"{context}.Copy({expression})";
    }
}
