/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UtilityVerse.Copy.Generators;

internal static class DeepCopyGenerator
{
	internal static string? Generate(INamedTypeSymbol typeSymbol)
	{
		if (typeSymbol.IsAbstract) return null;
		if (!Helper.IsPartial(typeSymbol)) return null;
		// Nested types are supported only when every enclosing type is partial (reported as UV009).
		if (!Helper.EnclosingTypesAreGeneratable(typeSymbol, out var enclosingChain)) return null;

		var depth = Helper.ExtractDepth(typeSymbol, "DeepCopy");
		var typeKind = Helper.GetTypeKind(typeSymbol);
		if (typeKind == null) return null;

		var namespaceName = typeSymbol.ContainingNamespace.IsGlobalNamespace
			? null
			: typeSymbol.ContainingNamespace.ToDisplayString();
		var typeName = Helper.GetTypeNameWithTypeParameters(typeSymbol);

		var hasParameterlessCtor = typeSymbol.InstanceConstructors.Any(c => c.Parameters.Length == 0);
		IMethodSymbol? ctor = null;
		if (!hasParameterlessCtor)
		{
			ctor = typeSymbol.InstanceConstructors
				.OrderByDescending(c => c.Parameters.Length)
				.FirstOrDefault();

			if (ctor == null)
				return null;
		}

		var sb = new StringBuilder();

		Helper.AppendFileHeader(sb, "Deep");
		Helper.AppendNamespaceOpen(sb, namespaceName);
		Helper.AppendEnclosingTypesOpen(sb, enclosingChain);
		Helper.AppendTypeDeclarationOpen(sb, typeKind, typeName);

		// Public entry point
		sb.AppendLine("        [System.CodeDom.Compiler.GeneratedCode(\"DeepCopyGenerator\", \"1.0\")]");
		sb.AppendLine($"        public {typeName} DeepCopy()");
		sb.AppendLine("        {");
		sb.AppendLine($"            var objectMap = new Dictionary<object, object>(System.Collections.Generic.ReferenceEqualityComparer.Instance);");
		sb.AppendLine($"            return DeepCopyInternal(0, {depth}, objectMap);");
		sb.AppendLine("        }");
		sb.AppendLine();

		// Internal implementation — single overload, always receives objectMap
		sb.AppendLine("        [System.CodeDom.Compiler.GeneratedCode(\"DeepCopyGenerator\", \"1.0\")]");
		sb.AppendLine($"        internal {typeName} DeepCopyInternal(int currentDepth, int maxDepth, Dictionary<object, object> objectMap)");
		sb.AppendLine("        {");

		if (!hasParameterlessCtor)
		{
			// Match constructor parameters against readable properties (including get-only ones).
			var propertyMap = Helper.GetReadableProperties(typeSymbol)
				.ToDictionary(p => p.Name, System.StringComparer.OrdinalIgnoreCase);

			if (typeSymbol.TypeKind == TypeKind.Class)
				Helper.AppendDeepCopyDepthGuard(sb, typeName);

			var ctorParamNames = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
			var args = ctor!.Parameters.Select(p =>
			{
				ctorParamNames.Add(p.Name);
				if (!propertyMap.TryGetValue(p.Name, out var prop))
					return $"default({p.Type.ToDisplayString()})";

				return Helper.GenerateDeepCopyAssignment(prop.Type, prop.Name);
			}).ToList();

			sb.AppendLine($"            var copy = new {typeName}({string.Join(", ", args)});");

			if (typeSymbol.TypeKind == TypeKind.Class)
				sb.AppendLine("            objectMap[this] = copy;");

			// Settable properties not supplied through the constructor must still be copied.
			// Assigning them after objectMap registration also lets circular references that
			// flow through a settable back-reference be resolved by the identity map.
			foreach (var member in Helper.GetAllProperties(typeSymbol))
			{
				if (ctorParamNames.Contains(member.Name)) continue;
				if (member.SetMethod is not { IsInitOnly: false }) continue; // init-only can't be set post-construction
				var assignment = Helper.GenerateDeepCopyAssignment(member.Type, member.Name);
				sb.AppendLine($"            copy.{member.Name} = {assignment};");
			}

			sb.AppendLine("            return copy;");
		}
		else if (typeSymbol.TypeKind == TypeKind.Class)
		{
			Helper.AppendDeepCopyDepthGuard(sb, typeName);

			var allProps = Helper.GetAllProperties(typeSymbol).ToList();
			var initOnlyProps = allProps.Where(p => p.SetMethod is { IsInitOnly: true }).ToList();
			var settableProps = allProps.Where(p => p.SetMethod is { IsInitOnly: false }).ToList();

			if (initOnlyProps.Count == 0)
			{
				sb.AppendLine($"            var copy = new {typeName}();");
			}
			else
			{
				// init-only properties can only be assigned through an object initializer.
				sb.AppendLine($"            var copy = new {typeName}");
				sb.AppendLine("            {");
				foreach (var member in initOnlyProps)
				{
					var assignment = Helper.GenerateDeepCopyAssignment(member.Type, member.Name);
					sb.AppendLine($"                {member.Name} = {assignment},");
				}
				sb.AppendLine("            };");
			}

			sb.AppendLine("            objectMap[this] = copy;");
			sb.AppendLine();

			foreach (var member in settableProps)
			{
				var assignment = Helper.GenerateDeepCopyAssignment(member.Type, member.Name);
				sb.AppendLine($"            copy.{member.Name} = {assignment};");
			}

			sb.AppendLine("            return copy;");
		}
		else
		{
			// Structs are value types — no circular-ref concern, just initialise and return
			sb.AppendLine($"            return new {typeName}");
			sb.AppendLine("            {");

			foreach (var member in Helper.GetAllProperties(typeSymbol))
			{
				var assignment = Helper.GenerateDeepCopyAssignment(member.Type, member.Name);
				sb.AppendLine($"                {member.Name} = {assignment},");
			}

			sb.AppendLine("            };");
		}

		sb.AppendLine("        }");
		Helper.AppendTypeDeclarationClose(sb);
		Helper.AppendEnclosingTypesClose(sb, enclosingChain);
		Helper.AppendNamespaceClose(sb, namespaceName);

		return sb.ToString();
	}
}
