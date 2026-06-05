using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UtilityVerse.Copy.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ValidationAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[Rules.MissingPartialRule, Rules.DeepRule, Rules.ShallowRule, Rules.NegativeDepthRule,
		 Rules.NonPublicTypeRule, Rules.AbstractTypeRule, Rules.EnclosingTypeNotPartialRule];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationContext =>
		{
			compilationContext.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
		});
	}

	private static void AnalyzeNamedType(SymbolAnalysisContext context)
	{
		if (context.Symbol is not INamedTypeSymbol namedType)
			return;

		if (namedType.TypeKind is not (TypeKind.Class or TypeKind.Struct))
			return;

		if (namedType.GetAttributes().Any(attr =>
			attr.AttributeClass?.ToDisplayString() is
				"System.CodeDom.Compiler.GeneratedCodeAttribute" or
				"System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
			return;

		var syntaxRef = namedType.DeclaringSyntaxReferences.FirstOrDefault();
		if (syntaxRef?.GetSyntax() is not TypeDeclarationSyntax typeSyntax)
			return;

		var hasDeepCopy = Helper.HasDeepCopyOptIn(namedType);
		var hasShallowCopy = Helper.HasShallowCopyOptIn(namedType);

		if (!hasDeepCopy && !hasShallowCopy)
			return;

		// UV001: must be partial
		if (!Helper.IsPartial(namedType))
		{
			context.ReportDiagnostic(Diagnostic.Create(Rules.MissingPartialRule, typeSyntax.Identifier.GetLocation(), namedType.Name));
		}

		// UV006: non-public type is silently ignored by the generator
		if (namedType.DeclaredAccessibility != Accessibility.Public)
		{
			context.ReportDiagnostic(Diagnostic.Create(Rules.NonPublicTypeRule, typeSyntax.Identifier.GetLocation(), namedType.Name));
		}

		// UV008: abstract type cannot be instantiated
		if (namedType.IsAbstract)
		{
			context.ReportDiagnostic(Diagnostic.Create(Rules.AbstractTypeRule, typeSyntax.Identifier.GetLocation(), namedType.Name));
		}

		// UV009: every enclosing type of a nested copyable type must be partial
		for (var enclosing = namedType.ContainingType; enclosing != null; enclosing = enclosing.ContainingType)
		{
			if (!Helper.IsPartial(enclosing))
			{
				context.ReportDiagnostic(Diagnostic.Create(
					Rules.EnclosingTypeNotPartialRule, typeSyntax.Identifier.GetLocation(), namedType.Name, enclosing.Name));
				break;
			}
		}

		// UV004: negative depth
		if (hasDeepCopy)
		{
			var deepAttr = namedType.GetAttributes()
				.FirstOrDefault(a => a.AttributeClass?.Name == "DeepCopy");
			if (deepAttr != null && ExtractDepthFromAttribute(deepAttr) is int deepDepth && deepDepth < 0)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					Rules.NegativeDepthRule,
					typeSyntax.Identifier.GetLocation(),
					namedType.Name, deepDepth));
			}
		}

		if (hasShallowCopy)
		{
			var shallowAttr = namedType.GetAttributes()
				.FirstOrDefault(a => a.AttributeClass?.Name == "ShallowCopy");
			if (shallowAttr != null && ExtractDepthFromAttribute(shallowAttr) is int shallowDepth && shallowDepth < 0)
			{
				context.ReportDiagnostic(Diagnostic.Create(
					Rules.NegativeDepthRule,
					typeSyntax.Identifier.GetLocation(),
					namedType.Name, shallowDepth));
			}
		}

		// UV002/UV003: nested types missing copy attribute
		foreach (var prop in Helper.GetAllProperties(namedType).Where(p => !p.IsStatic))
		{
			AnalyzeTypeRecursive(
				context,
				prop.Type,
				prop.Locations.FirstOrDefault() ?? typeSyntax.Identifier.GetLocation(),
				prop.Name,
				namedType,
				hasDeepCopy,
				hasShallowCopy
			);
		}
	}

	/// <summary>
	/// Returns the effective depth from a copy attribute, checking both positional and named arguments.
	/// Returns null if no depth argument is present.
	/// </summary>
	private static int? ExtractDepthFromAttribute(AttributeData attr)
	{
		// Named argument takes priority (e.g. [DeepCopy(Depth = -1)])
		var named = attr.NamedArguments.FirstOrDefault(na => na.Key == "Depth");
		if (named.Value.Value is int namedDepth)
			return namedDepth;

		// Positional argument (e.g. [DeepCopy(-1)])
		if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int positionalDepth)
			return positionalDepth;

		return null;
	}

	private static void AnalyzeTypeRecursive(
		SymbolAnalysisContext context,
		ITypeSymbol type,
		Location diagnosticLocation,
		string propertyName,
		INamedTypeSymbol parentType,
		bool requireDeepCopy,
		bool requireShallowCopy)
	{
		// Handle arrays: check element type
		if (type is IArrayTypeSymbol arrayType)
		{
			AnalyzeTypeRecursive(context, arrayType.ElementType, diagnosticLocation, propertyName, parentType, requireDeepCopy, requireShallowCopy);
			return;
		}

		if (type is not INamedTypeSymbol namedType)
			return;

		var unwrapped = Helper.UnwrapNullable(namedType);

		if (Helper.IsTrulyPrimitive(unwrapped) || Helper.IsBuiltinType(unwrapped))
			return;

		if (Helper.IsGenericCollection(unwrapped.OriginalDefinition.ToDisplayString(), out _))
		{
			foreach (var typeArg in unwrapped.TypeArguments.OfType<ITypeSymbol>())
			{
				AnalyzeTypeRecursive(context, typeArg, diagnosticLocation, propertyName, parentType, requireDeepCopy, requireShallowCopy);
			}
			return;
		}

		if (requireDeepCopy && !Helper.HasDeepCopyOptIn(unwrapped))
		{
			var diag = Diagnostic.Create(Rules.DeepRule, diagnosticLocation,
				propertyName, parentType.Name, unwrapped.Name);
			context.ReportDiagnostic(diag);
		}

		if (requireShallowCopy && !Helper.HasShallowCopyOptIn(unwrapped))
		{
			var diag = Diagnostic.Create(Rules.ShallowRule, diagnosticLocation,
				propertyName, parentType.Name, unwrapped.Name);
			context.ReportDiagnostic(diag);
		}
	}

}
