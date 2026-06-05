/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Generators;

internal static class ShallowCopyGenerator
{
	internal static string? Generate(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.IsAbstract) return null;
		if (!Helper.IsPartial(typeSymbol)) return null;
		// Nested types are supported only when every enclosing type is partial (reported as UV009).
		if (!Helper.EnclosingTypesAreGeneratable(typeSymbol, out var enclosingChain)) return null;

		var depth = Helper.ExtractDepth(typeSymbol, "ShallowCopy");
		var typeKind = Helper.GetTypeKind(typeSymbol);
		if (typeKind == null) return null;

		var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
			? null
			: typeSymbol.ContainingNamespace.ToDisplayString();
		var typeName = Helper.GetTypeNameWithTypeParameters(typeSymbol);

		var sb = new StringBuilder();

		Helper.AppendFileHeader(sb, "Shallow");
		Helper.AppendNamespaceOpen(sb, namespaceName);
		Helper.AppendEnclosingTypesOpen(sb, enclosingChain);
		Helper.AppendTypeDeclarationOpen(sb, typeKind, typeName);

		// Public entry point
		sb.AppendLine("        [System.CodeDom.Compiler.GeneratedCode(\"ShallowCopyGenerator\", \"1.0\")]");
		sb.AppendLine($"        public {typeName} ShallowCopy()");
		sb.AppendLine("        {");
		sb.AppendLine($"            var objectMap = new Dictionary<object, object>(System.Collections.Generic.ReferenceEqualityComparer.Instance);");
		sb.AppendLine($"            return ShallowCopyInternal(0, {depth}, objectMap);");
		sb.AppendLine("        }");
		sb.AppendLine();

		// Internal implementation
		sb.AppendLine("        [System.CodeDom.Compiler.GeneratedCode(\"ShallowCopyGenerator\", \"1.0\")]");
		sb.AppendLine($"        internal {typeName} ShallowCopyInternal(int currentDepth, int maxDepth, Dictionary<object, object> objectMap)");
		sb.AppendLine("        {");

		if (typeSymbol.TypeKind == TypeKind.Class)
		{
			sb.AppendLine("            // Check if already copied (handles circular references)");
			sb.AppendLine("            if (objectMap.TryGetValue(this, out var existing))");
			sb.AppendLine($"                return ({typeName})existing;");
			sb.AppendLine();

			// MemberwiseClone copies all primitives and shared references in one shot;
			// complex properties are then selectively overridden based on depth.
			sb.AppendLine($"            var copy = ({typeName})this.MemberwiseClone();");
			sb.AppendLine("            objectMap[this] = copy;");
			sb.AppendLine();

			foreach (var member in Helper.GetAllProperties(typeSymbol))
			{
				var propType = member.Type;

				// Primitives and builtins: MemberwiseClone already handled them
				if (Helper.IsTrulyPrimitive(propType) || Helper.IsBuiltinType(propType))
					continue;

				// init-only properties cannot be reassigned after MemberwiseClone; the clone already
				// holds the (shared) reference, which is valid shallow-copy semantics.
				if (member.SetMethod is { IsInitOnly: true })
					continue;

				var assignment = Helper.GenerateShallowCopyAssignment(propType, member.Name);
				sb.AppendLine($"            copy.{member.Name} = {assignment};");
			}

			sb.AppendLine("            return copy;");
		}
		else
		{
			// Structs: returning by value IS a copy — no recursion needed
			sb.AppendLine("            return this;");
		}

		sb.AppendLine("        }");
		Helper.AppendTypeDeclarationClose(sb);
		Helper.AppendEnclosingTypesClose(sb, enclosingChain);
		Helper.AppendNamespaceClose(sb, namespaceName);

		return sb.ToString();
	}
}
