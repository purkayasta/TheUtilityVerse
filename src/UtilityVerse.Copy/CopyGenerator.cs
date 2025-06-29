/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using UtilityVerse.Copy.Generators;

namespace UtilityVerse.Copy;

[Generator]
public sealed class CopyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => s is TypeDeclarationSyntax,
                transform: (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        context.RegisterSourceOutput(typeDeclarations, (spc, types) =>
        {
            foreach (var group in types.GroupBy(x => (x.Symbol, x.Mode)))
            {
                var (symbol, mode) = group.Key;
                var source = mode switch
                {
                    CopyMode.Deep => GenerateDeepCopy(symbol),
                    CopyMode.Shallow => GenerateShallowCopy(symbol),
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(source)) continue;

                var hintName = $"{symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "").Replace(".", "_")}_{mode}Copy.g.cs";
                spc.AddSource(hintName, SourceText.From(source!, Encoding.UTF8));
            }
        });
    }

    private static (INamedTypeSymbol Symbol, CopyMode Mode)? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDecl)
            return null;

        if (!typeDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            return null;

        var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, typeDecl) as INamedTypeSymbol;
        if (symbol is not { DeclaredAccessibility: Accessibility.Public })
            return null;

        var hasDeep = symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "UtilityVerse.Copy.Attributes.DeepCopy") ||
                       symbol.AllInterfaces.Any(i => i.ToDisplayString() == "UtilityVerse.Copy.Interfaces.IDeepCopy");

        var hasShallow = symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "UtilityVerse.Copy.Attributes.ShallowCopy") ||
                          symbol.AllInterfaces.Any(i => i.ToDisplayString() == "UtilityVerse.Copy.Interfaces.IShallowCopy");

        if (hasDeep) return (symbol, CopyMode.Deep);
        if (hasShallow) return (symbol, CopyMode.Shallow);

        return null;
    }

    private static string GenerateDeepCopy(INamedTypeSymbol typeSymbol) => DeepCopyGenerator.Generate(typeSymbol);

    private static string? GenerateShallowCopy(INamedTypeSymbol typeSymbol) => ShallowCopyGenerator.Generate(typeSymbol);

    enum CopyMode
    {
        Deep,
        Shallow
    }
}