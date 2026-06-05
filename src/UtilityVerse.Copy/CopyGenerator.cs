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
    private const string DeepCopyAttribute = "UtilityVerse.Copy.DeepCopy";
    private const string ShallowCopyAttribute = "UtilityVerse.Copy.ShallowCopy";
    private const string DeepCopyInterface = "UtilityVerse.Copy.IDeepCopy";
    private const string ShallowCopyInterface = "UtilityVerse.Copy.IShallowCopy";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Attribute opt-in. ForAttributeWithMetadataName only visits types that actually carry the
        // attribute (instead of running a semantic transform on every type declaration), and each
        // transform produces an equatable (hintName, source) value rather than an ISymbol. Keeping
        // symbols out of the cached pipeline means the Compilation is not rooted across edits and the
        // expensive generation step is cached per type and only re-runs when that type changes.
        var deep = context.SyntaxProvider.ForAttributeWithMetadataName(
            DeepCopyAttribute,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, _) => GenerateFor(ctx.TargetSymbol, CopyMode.Deep));

        var shallow = context.SyntaxProvider.ForAttributeWithMetadataName(
            ShallowCopyAttribute,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, _) => GenerateFor(ctx.TargetSymbol, CopyMode.Shallow));

        // Interface opt-in (IDeepCopy / IShallowCopy). There is no attribute to key on, so a cheap
        // syntactic predicate (the type must have a base list) keeps the semantic transform off the
        // vast majority of type declarations.
        var viaInterface = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is TypeDeclarationSyntax { BaseList: not null },
                transform: static (ctx, _) => GenerateForInterfaces(ctx))
            .Where(static a => !a.IsDefaultOrEmpty)
            .SelectMany(static (a, _) => a);

        var all = deep.Collect()
            .Combine(shallow.Collect())
            .Combine(viaInterface.Collect());

        context.RegisterSourceOutput(all, static (spc, data) =>
        {
            var ((deepSources, shallowSources), interfaceSources) = data;

            // A partial type can carry the attribute on more than one part, and a type may opt in via
            // both an attribute and an interface; dedupe on the hint name so AddSource is never called
            // twice for the same file.
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);

            void Emit((string HintName, string Source)? source)
            {
                if (source is { } s && seen.Add(s.HintName))
                    spc.AddSource(s.HintName, SourceText.From(s.Source, Encoding.UTF8));
            }

            foreach (var s in deepSources) Emit(s);
            foreach (var s in shallowSources) Emit(s);
            foreach (var s in interfaceSources) Emit(s);
        });
    }

    private static (string HintName, string Source)? GenerateFor(ISymbol? symbol, CopyMode mode)
    {
        if (symbol is not INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } type)
            return null;

        var source = mode == CopyMode.Deep
            ? DeepCopyGenerator.Generate(type)
            : ShallowCopyGenerator.Generate(type);

        return string.IsNullOrWhiteSpace(source)
            ? null
            : (BuildHintName(type, mode), source!);
    }

    private static ImmutableArray<(string HintName, string Source)> GenerateForInterfaces(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not TypeDeclarationSyntax decl)
            return ImmutableArray<(string, string)>.Empty;

        if (ModelExtensions.GetDeclaredSymbol(ctx.SemanticModel, decl)
            is not INamedTypeSymbol { DeclaredAccessibility: Accessibility.Public } type)
            return ImmutableArray<(string, string)>.Empty;

        var builder = ImmutableArray.CreateBuilder<(string, string)>(2);

        // Attribute-based opt-in is handled by the ForAttributeWithMetadataName providers; here we only
        // react to the marker interfaces.
        if (type.AllInterfaces.Any(i => Helper.SymbolEquals(i, DeepCopyInterface)))
        {
            var src = DeepCopyGenerator.Generate(type);
            if (!string.IsNullOrWhiteSpace(src))
                builder.Add((BuildHintName(type, CopyMode.Deep), src!));
        }

        if (type.AllInterfaces.Any(i => Helper.SymbolEquals(i, ShallowCopyInterface)))
        {
            var src = ShallowCopyGenerator.Generate(type);
            if (!string.IsNullOrWhiteSpace(src))
                builder.Add((BuildHintName(type, CopyMode.Shallow), src!));
        }

        return builder.ToImmutable();
    }

    private static string BuildHintName(INamedTypeSymbol symbol, CopyMode mode)
    {
        var rawName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "")
            .Replace(".", "_");

        // Strip characters that are invalid in a hintName (generics produce '<', '>', ',', ' ').
        var sb = new StringBuilder(rawName.Length);
        foreach (var ch in rawName)
            sb.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');

        return $"{sb}_{mode}Copy.g.cs";
    }

    private enum CopyMode
    {
        Deep,
        Shallow
    }
}
