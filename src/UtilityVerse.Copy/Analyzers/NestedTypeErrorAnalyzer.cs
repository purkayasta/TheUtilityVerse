using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UtilityVerse.Copy.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NestedTypeErrorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor DeepRule = new(
        id: "UV002",
        title: "Nested type missing DeepCopy attribute",
        messageFormat: "Property '{0}' of type '{1}' requires nested type '{2}' to have DeepCopy attribute or implement IDeepCopy",
        category: "UtilityVerse.Copy",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ShallowRule = new DiagnosticDescriptor(
        id: "UV003",
        title: "Nested type missing ShallowCopy attribute",
        messageFormat: "Property '{0}' of type '{1}' requires nested type '{2}' to have ShallowCopy attribute or implement IShallowCopy",
        category: "UtilityVerse.Copy",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DeepRule, ShallowRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // Only analyze public partial types (assuming generator requires partial)
        if (namedType.DeclaredAccessibility != Accessibility.Public)
            return;

        if (!namedType.DeclaringSyntaxReferences.Any(syntaxRef =>
            syntaxRef.GetSyntax() is TypeDeclarationSyntax tds &&
            tds.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))))
            return;

        // Check if type opts into DeepCopy or ShallowCopy
        bool hasDeepCopy = Helper.HasDeepCopyOptIn(namedType);
        bool hasShallowCopy = Helper.HasShallowCopyOptIn(namedType);

        if (!hasDeepCopy && !hasShallowCopy)
            return; // type itself not opted in - no need to check nested types

        // Get all properties including inherited
        var properties = namedType.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic);

        foreach (var prop in properties)
        {
            if (prop.Type is not INamedTypeSymbol propType)
            {
                continue;
            }
            // Skip primitives and enums
            if (Helper.IsTrulyPrimitive(propType))
                continue;

            if (propType.IsGenericType)
            {
                foreach (var typeArg in propType.TypeArguments.OfType<INamedTypeSymbol>())
                {
                    if (Helper.IsTrulyPrimitive(typeArg))
                        continue;

                    if (hasDeepCopy && !Helper.HasDeepCopyOptIn(typeArg))
                    {
                        var diagnostic = Diagnostic.Create(
                            DeepRule,
                            prop.Locations.FirstOrDefault(),
                            prop.Name, namedType.Name, typeArg.Name);
                        context.ReportDiagnostic(diagnostic);
                    }

                    if (hasShallowCopy && !Helper.HasShallowCopyOptIn(typeArg))
                    {
                        var diagnostic = Diagnostic.Create(
                            ShallowRule,
                            prop.Locations.FirstOrDefault(),
                            prop.Name, namedType.Name, typeArg.Name);
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
            else
            {
                // Handle non-generic nested types
                if (hasDeepCopy && !Helper.HasDeepCopyOptIn(propType))
                {
                    var diagnostic = Diagnostic.Create(
                        DeepRule,
                        prop.Locations.FirstOrDefault(),
                        prop.Name, namedType.Name, propType.Name);
                    context.ReportDiagnostic(diagnostic);
                }

                if (hasShallowCopy && !Helper.HasShallowCopyOptIn(propType))
                {
                    var diagnostic = Diagnostic.Create(
                        ShallowRule,
                        prop.Locations.FirstOrDefault(),
                        prop.Name, namedType.Name, propType.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}