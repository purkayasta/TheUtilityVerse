// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace UtilityVerse.Copy.UnitTest.Infrastructure;

internal static class GeneratorHarness
{
    private const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using UtilityVerse.Copy;
        """;

    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    public static IReadOnlyList<MetadataReference> References { get; } =
    [
        .. Net90.References.All,
        MetadataReference.CreateFromFile(typeof(DeepCopy).Assembly.Location),
    ];

    public static GeneratorRun Run(string source)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName: $"CopyGeneratorTest_{Guid.NewGuid():N}",
            syntaxTrees:
            [
                CSharpSyntaxTree.ParseText(GlobalUsings, ParseOptions, path: "GlobalUsings.cs"),
                CSharpSyntaxTree.ParseText(source, ParseOptions, path: "Source.cs"),
            ],
            references: References,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new CopyGenerator().AsSourceGenerator()],
            parseOptions: ParseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        var result = driver.GetRunResult().Results.Single();

        return new GeneratorRun(driver, outputCompilation, result.GeneratedSources, diagnostics);
    }

    public static LoadedAssembly Load(GeneratorRun run)
    {
        using var stream = new MemoryStream();
        var emitResult = run.OutputCompilation.Emit(stream);
        var errors = emitResult.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error).ToList();
        Assert.True(emitResult.Success, $"Generated compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");

        stream.Position = 0;
        return LoadedAssembly.From(stream);
    }
}
