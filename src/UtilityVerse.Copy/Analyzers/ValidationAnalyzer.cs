// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

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
        [Rules.MissingPartialRule, Rules.DeepRule, Rules.ShallowRule, Rules.UnsupportedShapeRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type || type.TypeKind is not (TypeKind.Class or TypeKind.Struct)) return;
        if (type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() is
            "System.CodeDom.Compiler.GeneratedCodeAttribute" or "System.Runtime.CompilerServices.CompilerGeneratedAttribute")) return;
        if (type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not TypeDeclarationSyntax syntax) return;
        var deep = Helper.HasDeepCopyOptIn(type);
        if (!deep && !Helper.HasShallowCopyOptIn(type)) return;
        if (!Helper.IsPartial(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MissingPartialRule, syntax.Identifier.GetLocation(), type.Name));
            return;
        }
        if (Validation.HasUnsupportedShape(type, deep, out var reason))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UnsupportedShapeRule, syntax.Identifier.GetLocation(), type.Name, reason));
            return;
        }
        if (!deep) return;
        foreach (var member in Helper.GetCopiedState(type))
            foreach (var missing in Validation.MissingOptIns(Helper.StateType(member)))
                context.ReportDiagnostic(Diagnostic.Create(Rules.DeepRule, member.Locations.FirstOrDefault() ?? syntax.Identifier.GetLocation(),
                    member.Name, type.Name, missing.Name));
    }
}
