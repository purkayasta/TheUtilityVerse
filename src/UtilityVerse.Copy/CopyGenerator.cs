// Author: Pritom Purkayasta
//  Copyright (c) Pritom Purkayasta All rights reserved.
//  FREE TO USE TO CONNECT THE WORLD

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using UtilityVerse.Copy.Generators;

namespace UtilityVerse.Copy;

[Generator]
public sealed class CopyGenerator : IIncrementalGenerator
{
    /// <summary>Registers model discovery and incremental generation for deep-copy and shallow-copy opt-ins.</summary>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => s is TypeDeclarationSyntax,
                transform: (ctx, _) => GetSemanticTarget(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        var sources = typeDeclarations.Combine(context.CompilationProvider)
            .SelectMany(static (input, cancellationToken) => GenerateSources(input.Left, input.Right, cancellationToken));

        context.RegisterSourceOutput(sources, static (output, source) =>
        {
            output.AddSource(source.HintName, SourceText.From(source.Text, Encoding.UTF8));
        });
    }

    private static ImmutableArray<(string HintName, string Text)> GenerateSources(
        ImmutableArray<(INamedTypeSymbol Symbol, CopyMode Modes)> types,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var deepModels = types
            .Where(item => (item.Modes & CopyMode.Deep) != 0)
            .Select(item => item.Symbol)
            .ToArray();

        var sources = ImmutableArray.CreateBuilder<(string HintName, string Text)>();

        foreach (var (symbol, modes) in types)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if ((modes & CopyMode.Deep) != 0 && !Validation.HasUnsupportedShape(symbol, true, out _, compilation, cancellationToken: cancellationToken))
            {
                AddSource(sources, symbol, CopyMode.Deep, DeepCopyGenerator.Generate(symbol, compilation, deepModels));
            }

            if ((modes & CopyMode.Shallow) != 0 && !Validation.HasUnsupportedShape(symbol, false, out _, compilation, cancellationToken: cancellationToken))
            {
                AddSource(sources, symbol, CopyMode.Shallow, ShallowCopyGenerator.Generate(symbol));
            }
        }

        // Comparing individual names and text lets Roslyn reuse unchanged output
        // even when another model or the compilation has changed.
        return sources.ToImmutable();
    }

    private static void AddSource(
        ImmutableArray<(string HintName, string Text)>.Builder sources,
        INamedTypeSymbol symbol,
        CopyMode mode,
        string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        var fullName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        var safeName = string.Concat(fullName.Select(character => character switch
        {
            '.' => "_",
            _ when char.IsLetterOrDigit(character) => character.ToString(),
            _ => "_" + ((int)character).ToString("X4")
        }));
        sources.Add(($"{safeName}_{mode}Copy.g.cs", source!));
    }

    private static (INamedTypeSymbol Symbol, CopyMode Modes)? GetSemanticTarget(GeneratorSyntaxContext context)
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

        // The symbol includes attributes and interfaces from every partial declaration.
        var firstDeclaration = symbol.DeclaringSyntaxReferences[0];

        if (firstDeclaration.SyntaxTree != typeDecl.SyntaxTree || firstDeclaration.Span != typeDecl.Span)
        {
            return null;
        }

        var modes = CopyMode.None;

        if (Helper.HasDeepCopyOptIn(symbol))
        {
            modes |= CopyMode.Deep;
        }

        if (Helper.HasShallowCopyOptIn(symbol))
        {
            modes |= CopyMode.Shallow;
        }

        return modes == CopyMode.None ? null : (symbol, modes);
    }

    [Flags]
    private enum CopyMode
    {
        None = 0,
        Deep = 1,
        Shallow = 2
    }
}
