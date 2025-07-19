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
        [Rules.MissingPartialRule, Rules.DeepRule, Rules.ShallowRule];
    
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
        
        if ((hasDeepCopy || hasShallowCopy) && !Helper.IsPartial(namedType))
        {
            var diagnostic = Diagnostic.Create(Rules.MissingPartialRule, typeSyntax.Identifier.GetLocation(), namedType.Name);
            context.ReportDiagnostic(diagnostic);
        }

        if (!hasDeepCopy && !hasShallowCopy)
            return;

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

    private static void AnalyzeTypeRecursive(
        SymbolAnalysisContext context,
        ITypeSymbol type,
        Location diagnosticLocation,
        string propertyName,
        INamedTypeSymbol parentType,
        bool requireDeepCopy,
        bool requireShallowCopy)
    {
        if (type is not INamedTypeSymbol namedType)
            return;

        var unwrapped = UnwrapNullable(namedType);

        if (Helper.IsTrulyPrimitive(unwrapped))
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

    private static INamedTypeSymbol UnwrapNullable(INamedTypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
               type.TypeArguments.FirstOrDefault() is INamedTypeSymbol underlying
            ? underlying
            : type;
    }

    private static bool IsInsideCopyEnabledPartialParent(INamedTypeSymbol? symbol)
    {
        while (symbol is not null)
        {
            if (!Helper.IsPartial(symbol))
                return false;

            if (Helper.HasDeepCopyOptIn(symbol) || Helper.HasShallowCopyOptIn(symbol))
                return true;

            symbol = symbol.ContainingType;
        }
        return false;
    }
}
