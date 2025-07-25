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
		if (typeSymbol.IsAbstract) return string.Empty;
		if (!Helper.IsPartial(typeSymbol)) return string.Empty;

		var isGlobalNamespace = typeSymbol.ContainingNamespace.IsGlobalNamespace;
		var namespaceName = isGlobalNamespace ? null : typeSymbol.ContainingNamespace.ToDisplayString();
		var typeName = typeSymbol.Name;

		var typeKind = typeSymbol switch
		{
			{ TypeKind: TypeKind.Class, IsRecord: true } => "record",
			{ TypeKind: TypeKind.Struct, IsRecord: true } => "record struct",
			{ TypeKind: TypeKind.Class } => "class",
			{ TypeKind: TypeKind.Struct } => "struct",
			_ => null
		};

		if (typeKind == null)
			return null;

		var sb = new StringBuilder();

		sb.AppendLine("""

                      // <auto-generated>
                      //     This code was generated by Copy.
                      //     Author: Pritom Purkayasta
                      //     DO NOT modify this file manually. Changes may be overwritten.
                      //     This file contains auto-generated ShallowCopy() implementations.
                      // </auto-generated>

                      using System;
                      using System.Linq;
                      using System.Collections.Generic;
                      using System.Collections.ObjectModel;
                      using System.Collections.Immutable;
                      using System.Collections.Concurrent;
                      using System.Collections.Frozen;

                      """);

		if (!string.IsNullOrEmpty(namespaceName))
		{
			sb.AppendLine($"namespace {namespaceName}");
			sb.AppendLine("{");
		}

		sb.AppendLine($"    public partial {typeKind} {typeName}");
		sb.AppendLine("    {");
		sb.AppendLine("        [System.CodeDom.Compiler.GeneratedCode(\"ShallowCopyGenerator\", \"1.0\")]");
		sb.AppendLine($"        public {typeName} ShallowCopy()");
		sb.AppendLine("        {");

		if (typeSymbol.TypeKind == TypeKind.Class || typeSymbol.IsRecord)
		{
			sb.AppendLine($"            var copy = ({typeName})this.MemberwiseClone();");

			foreach (var member in Helper.GetAllProperties(typeSymbol))
			{
				if (member.IsReadOnly || member.SetMethod is null)
					continue;

				var propType = member.Type;

				if (Helper.IsTrulyPrimitive(propType) || Helper.IsBuiltinType(propType)) continue;

				string? assignment = null;

				switch (propType)
				{
					case IArrayTypeSymbol:
						assignment = $"this.{member.Name} != null ? ({member.Type.ToDisplayString()})this.{member.Name}.Clone() : null";
						break;

					case INamedTypeSymbol named when named.IsGenericType:
						{
							var original = named.OriginalDefinition.ConstructUnboundGenericType().ToDisplayString();

							if (Helper.IsGenericCollection(original, out var kind))
							{
								var itemType = named.TypeArguments.FirstOrDefault()?.ToDisplayString() ?? "var";
								assignment = kind switch
								{
									"List" or "Enumerable" =>
										$"this.{member.Name}?.ToList()",

									"HashSet" =>
										$"this.{member.Name} != null ? new HashSet<{itemType}>(this.{member.Name}) : null",

									"Dictionary" =>
										$"this.{member.Name}?.ToDictionary(x => x.Key, x => x.Value)",

									_ => null
								};
							}
							else if (original.StartsWith("System.Tuple"))
							{
								assignment = $"this.{member.Name}";
							}

							break;
						}

					case INamedTypeSymbol named when named.ToDisplayString().StartsWith("System.ValueTuple"):
						assignment = $"this.{member.Name}";
						break;
				}

				if (assignment != null)
				{
					sb.AppendLine($"            copy.{member.Name} = {assignment};");
				}
			}

			sb.AppendLine("            return copy;");
		}
		else
		{
			sb.AppendLine("            return this;");
		}

		sb.AppendLine("        }");
		sb.AppendLine("    }");

		if (!string.IsNullOrEmpty(namespaceName))
		{
			sb.AppendLine("}");
		}

		return sb.ToString();
	}
}