// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class CopyIncrementalityTests
{
    [Theory]
    [InlineData(false, 2, 0)]
    [InlineData(true, 1, 1)]
    public void Generate_ReusesUnchangedSourceOutputs(bool editModel, int expectedCached, int expectedModified)
    {
        var options = new CSharpParseOptions(LanguageVersion.Latest);
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = CSharpSyntaxTree.ParseText("""
            [UtilityVerse.Copy.DeepCopy]
            public partial class First
            {
                public int Value { get; set; }
            }
            """, options, cancellationToken: cancellationToken);
        var second = CSharpSyntaxTree.ParseText("""
            [UtilityVerse.Copy.ShallowCopy]
            public partial class Second
            {
            }
            """, options, cancellationToken: cancellationToken);
        var unrelated = CSharpSyntaxTree.ParseText("public class Unrelated;", options, cancellationToken: cancellationToken);
        var compilation = CSharpCompilation.Create(
            "IncrementalCopyTest",
            [first, second, unrelated],
            GeneratorHarness.References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new CopyGenerator().AsSourceGenerator()],
            parseOptions: options,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(compilation, cancellationToken);
        Assert.Equal(2, Assert.Single(driver.GetRunResult().Results).GeneratedSources.Length);

        var edited = editModel
            ? compilation.ReplaceSyntaxTree(first, CSharpSyntaxTree.ParseText("""
                [UtilityVerse.Copy.DeepCopy]
                public partial class First
                {
                    public int RenamedValue { get; set; }
                }
                """, options, cancellationToken: cancellationToken))
            : compilation.ReplaceSyntaxTree(unrelated, CSharpSyntaxTree.ParseText(
                "public class Unrelated { public int X; }", options, cancellationToken: cancellationToken));
        driver = driver.RunGenerators(edited, cancellationToken);

        var result = Assert.Single(driver.GetRunResult().Results);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(2, result.GeneratedSources.Length);

        var reasons = result.TrackedOutputSteps.Values
            .SelectMany(steps => steps)
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason)
            .ToArray();

        Assert.Equal(expectedCached, reasons.Count(reason => reason == IncrementalStepRunReason.Cached));
        Assert.Equal(expectedModified, reasons.Count(reason => reason == IncrementalStepRunReason.Modified));
    }
}
