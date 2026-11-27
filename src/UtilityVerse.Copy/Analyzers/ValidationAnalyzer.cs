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
        var shallow = Helper.HasShallowCopyOptIn(type);

        if (!deep && !shallow)
        {
            return;
        }

        var location = syntax.Identifier.GetLocation();

        if (!Helper.IsPartial(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.MissingPartialRule, location, type.Name));
            return;
        }

        string? shallowReason = null;

        if (shallow && Validation.HasUnsupportedShape(type, false, out var reason, context.Compilation))
        {
            shallowReason = reason;
            context.ReportDiagnostic(Diagnostic.Create(Rules.UnsupportedShapeRule, location, type.Name, reason));
        }

        if (!deep)
        {
            return;
        }

        if (Validation.HasUnsupportedShape(type, true, out var deepReason, context.Compilation))
        {
            if (deepReason != shallowReason)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rules.UnsupportedShapeRule, location, type.Name, deepReason));
            }

            return;
        }

        foreach (var member in Helper.GetCopiedState(type))
        {
            if (Helper.GetCopyBehavior(member) != CopyBehavior.Copy)
            {
                continue;
            }

            foreach (var missing in Validation.MissingOptIns(Helper.StateType(member)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rules.DeepRule,
                    member.Locations.FirstOrDefault() ?? location,
                    member.Name,
                    type.Name,
                    missing.Name));
            }
        }
    }
}
