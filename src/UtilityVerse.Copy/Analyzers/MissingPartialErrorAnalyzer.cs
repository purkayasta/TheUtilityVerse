using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace UtilityVerse.Copy.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingPartialErrorAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor MissingPartialModifierRule = new(
            id: "UV001",
            title: "Type must be marked as partial for DeepCopy/ShallowCopy generation",
            messageFormat: "Type '{0}' must be marked as 'partial' to support DeepCopy/ShallowCopy generation",
            category: "UtilityVerse.Copy",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "UtilityVerse.Copy requires all target types to be marked with 'partial' so the DeepCopy() or ShallowCopy() method can be injected."
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [MissingPartialModifierRule];

    public override void Initialize(AnalysisContext context)
    {
        // Configure analysis for syntax trees
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
    }

    private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDecl = (TypeDeclarationSyntax)context.Node;

        // Skip if already marked partial
        if (typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            return;

        // Skip if not user-defined (e.g., external/metadata references)
        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDecl);
        if (symbol is null || symbol.DeclaringSyntaxReferences.Length == 0)
            return;

        // Optionally: check if it's used for DeepCopy somehow
        // Example: skip types with [GeneratedCode] or [CompilerGenerated]
        if (symbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() is "System.CodeDom.Compiler.GeneratedCodeAttribute" or "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            return;

        var diagnostic = Diagnostic.Create(MissingPartialModifierRule, typeDecl.Identifier.GetLocation(), symbol.Name);
        context.ReportDiagnostic(diagnostic);
    }
}