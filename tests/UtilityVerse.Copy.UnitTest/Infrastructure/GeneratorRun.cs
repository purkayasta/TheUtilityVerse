// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using UtilityVerse.Copy.Analyzers;

namespace UtilityVerse.Copy.UnitTest.Infrastructure;

internal sealed record GeneratorRun(
    GeneratorDriver Driver,
    Compilation OutputCompilation,
    ImmutableArray<GeneratedSourceResult> GeneratedSources,
    ImmutableArray<Diagnostic> GeneratorDiagnostics)
{
    public IReadOnlyList<Diagnostic> AnalyzerErrors => OutputCompilation
        .WithAnalyzers([new ValidationAnalyzer()]).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult()
        .Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

    public string GeneratedText =>
        string.Join(Environment.NewLine, GeneratedSources.Select(s => s.SourceText.ToString()));

    public IReadOnlyList<string> HintNames =>
        GeneratedSources.Select(s => s.HintName).ToList();

    public string GeneratedFile(string hintName) =>
        GeneratedSources.Single(s => string.Equals(s.HintName, hintName, StringComparison.Ordinal)).SourceText.ToString();

    public IReadOnlyList<Diagnostic> CompilationErrors =>
        OutputCompilation.GetDiagnostics()
            .Where(d => d.Severity is DiagnosticSeverity.Error)
            .ToList();

    public IReadOnlyList<string> CompilationErrorIds =>
        CompilationErrors.Select(d => d.Id).Distinct().ToList();
}
