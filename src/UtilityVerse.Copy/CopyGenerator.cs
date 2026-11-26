/// <summary>
/// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD
/// </summary>

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
                transform: (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        context.RegisterSourceOutput(typeDeclarations, (spc, types) =>
        {
            foreach (var group in types.GroupBy(x => (x.Symbol, x.Mode)))
            {
                var (symbol, mode) = group.Key;

                if (Validation.HasUnsupportedShape(symbol, mode == CopyMode.Deep, out _))
                {
                    continue;
                }

                var source = mode switch
                {
                    CopyMode.Deep => GenerateDeepCopy(symbol),
                    CopyMode.Shallow => GenerateShallowCopy(symbol),
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(source))
                {
                    continue;
                }

                var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                var safeName = string.Concat(fullName.Select(character => character switch
                {
                    '.' => "_",
                    _ when char.IsLetterOrDigit(character) => character.ToString(),
                    _ => "_" + ((int)character).ToString("X4")
                }));
                var hintName = $"{safeName}_{mode}Copy.g.cs";
                spc.AddSource(hintName, SourceText.From(source!, Encoding.UTF8));
            }
        });
    }

    private static (INamedTypeSymbol Symbol, CopyMode Mode)? GetSemanticTarget(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDecl)
        {
            return null;
        }

        var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, typeDecl) as INamedTypeSymbol;

        if (symbol is null)
        {
            return null;
        }

        var hasDeep = Helper.HasDeepCopyOptIn(symbol);

        var hasShallow = Helper.HasShallowCopyOptIn(symbol);

        if (hasDeep)
        {
            return (symbol, CopyMode.Deep);
        }

        if (hasShallow)
        {
            return (symbol, CopyMode.Shallow);
        }

        return null;
    }

    private static string? GenerateDeepCopy(INamedTypeSymbol typeSymbol) => DeepCopyGenerator.Generate(typeSymbol);

    private static string? GenerateShallowCopy(INamedTypeSymbol typeSymbol) => ShallowCopyGenerator.Generate(typeSymbol);

    enum CopyMode
    {
        Deep,
        Shallow
    }
}
