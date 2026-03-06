/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
                transform: (ctx, _) => GetSemanticTargets(ctx))
            .Where(static m => m.Length > 0)
            .SelectMany(static (m, _) => m)
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

    private static ImmutableArray<(INamedTypeSymbol Symbol, CopyMode Mode)> GetSemanticTargets(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDecl)
            return ImmutableArray<(INamedTypeSymbol, CopyMode)>.Empty;

        var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, typeDecl) as INamedTypeSymbol;
        if (symbol is not { DeclaredAccessibility: Accessibility.Public })
            return ImmutableArray<(INamedTypeSymbol, CopyMode)>.Empty;

        var hasDeep = Helper.HasDeepCopyOptIn(symbol);
        var hasShallow = Helper.HasShallowCopyOptIn(symbol);

        var builder = ImmutableArray.CreateBuilder<(INamedTypeSymbol, CopyMode)>(2);
        if (hasDeep) builder.Add((symbol, CopyMode.Deep));
        if (hasShallow) builder.Add((symbol, CopyMode.Shallow));
        return builder.ToImmutable();
    }

    private static string? GenerateDeepCopy(INamedTypeSymbol typeSymbol) => DeepCopyGenerator.Generate(typeSymbol);

    private static string? GenerateShallowCopy(INamedTypeSymbol typeSymbol) => ShallowCopyGenerator.Generate(typeSymbol);

    enum CopyMode
    {
        Deep,
        Shallow
    }
}
