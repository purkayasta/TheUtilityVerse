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
        [Rules.MissingPartialRule, Rules.DeepRule, Rules.ShallowRule, Rules.UnsupportedShapeRule, Rules.IgnoredCopyPolicyRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct } type ||
            type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) is not TypeDeclarationSyntax syntax)
        {
            return;
        }

        var deep = Helper.HasDeepCopyOptIn(type);
        var shallow = Helper.HasShallowCopyOptIn(type);

        if (!deep && !shallow)
        {
            return;
        }

        if (type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() is
            "System.CodeDom.Compiler.GeneratedCodeAttribute" or "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
        {
            return;
        }

        var location = syntax.Identifier.GetLocation();

        if (Validation.FindFileLocalDeclaration(type) is { } fileLocal)
        {
            var declaration = (TypeDeclarationSyntax)fileLocal.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken);
            context.ReportDiagnostic(Diagnostic.Create(
                Rules.UnsupportedShapeRule,
                declaration.Identifier.GetLocation(),
                type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                Validation.FileLocalReason(fileLocal)));
            return;
        }

        if (!Helper.IsPartial(type))
        {
            var methods = (deep, shallow) switch
            {
                (true, true) => "DeepCopy() and ShallowCopy()",
                (true, false) => "DeepCopy()",
                _ => "ShallowCopy()",
            };

            context.ReportDiagnostic(Diagnostic.Create(Rules.MissingPartialRule, location, type.Name, methods));
            return;
        }

        ReportIgnoredPolicies(context, type, deep);

        string? shallowReason = null;

        if (shallow && Validation.HasUnsupportedShape(type, false, out var reason, context.Compilation))
        {
            shallowReason = reason;
            ReportUnsupportedShape(location, reason);
        }

        if (!deep)
        {
            return;
        }

        if (Validation.HasUnsupportedShape(type, true, out var deepReason, context.Compilation, ReportMemberIssue))
        {
            if (deepReason.Length != 0 && deepReason != shallowReason)
            {
                ReportUnsupportedShape(location, deepReason);
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
                ReportMissingOptIn(context, type, member, missing, location);
            }
        }

        void ReportMemberIssue(ISymbol member, string reason)
        {
            ReportUnsupportedShape(member.Locations.FirstOrDefault(source => source.IsInSource) ?? location, reason);
        }

        void ReportUnsupportedShape(Location source, string reason)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rules.UnsupportedShapeRule, source,
                type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), reason));
        }
    }

    private static void ReportMissingOptIn(SymbolAnalysisContext context, INamedTypeSymbol type, ISymbol member,
        INamedTypeSymbol missing, Location fallback)
    {
        var declaration = missing.OriginalDefinition.Locations.FirstOrDefault(source =>
            source.SourceTree is { } tree && context.Compilation.ContainsSyntaxTree(tree));
        var canOptIn = declaration != null &&
            missing is { TypeKind: TypeKind.Class or TypeKind.Struct, IsStatic: false, IsRefLikeType: false } &&
            Validation.FindFileLocalDeclaration(missing) == null;
        var guidance = canOptIn
            ? "Make that type partial and add [DeepCopy] or implement IDeepCopy"
            : "Apply [CopyPolicy(CopyBehavior.Custom)] to the stored member and use CopyContext.RegisterExternalCopier<T>() for its exact runtime type";

        context.ReportDiagnostic(Diagnostic.Create(
            Rules.DeepRule,
            member.Locations.FirstOrDefault(source => source.IsInSource) ?? fallback,
            additionalLocations: declaration == null ? [] : [declaration],
            properties: null,
            member.Name,
            type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            missing.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
            guidance));
    }

    private static void ReportIgnoredPolicies(SymbolAnalysisContext context, INamedTypeSymbol type, bool deep)
    {
        var policyType = context.Compilation.GetTypeByMetadataName("UtilityVerse.Copy.CopyPolicyAttribute");

        if (policyType == null)
        {
            return;
        }

        foreach (var member in type.GetMembers())
        {
            if (member is not (IFieldSymbol or IPropertySymbol))
            {
                continue;
            }

            foreach (var attribute in member.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, policyType) ||
                    attribute.ApplicationSyntaxReference is not { } syntax ||
                    attribute.ConstructorArguments.Length != 1 ||
                    attribute.ConstructorArguments[0].Value is not int policy)
                {
                    continue;
                }

                var state = member is IFieldSymbol { AssociatedSymbol: IPropertySymbol property } ? property : member;
                var isStored = !state.IsStatic && (state is IFieldSymbol { IsImplicitlyDeclared: false, IsConst: false } ||
                    state is IPropertySymbol { IsIndexer: false } stored && Helper.IsAutoProperty(stored));
                var reason = !isStored
                    ? "Copy policies apply only to stored instance fields and auto-properties. Remove the attribute or apply it to the member that stores the value"
                    : !deep
                        ? "ShallowCopy ignores member policies. Remove the attribute if shallow copying is intended. " +
                          "To apply a member policy, add [DeepCopy] or implement IDeepCopy and call DeepCopy()"
                        : null;

                if (reason != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Rules.IgnoredCopyPolicyRule,
                        syntax.GetSyntax(context.CancellationToken).GetLocation(),
                        (CopyBehavior)policy,
                        state.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        reason));
                }
            }
        }
    }
}
