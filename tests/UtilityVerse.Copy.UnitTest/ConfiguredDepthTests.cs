// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using UtilityVerse.Copy.Analyzers;

namespace UtilityVerse.Copy.UnitTest;

public class ConfiguredDepthTests
{
    [Theory]
    [InlineData("[DeepCopy]", 5)]
    [InlineData("[DeepCopy(depth: 1)]", 1)]
    [InlineData("[CopyDepth(100)]", 100)]
    public void DeepCopy_EnforcesRootBudget(string attribute, int limit)
    {
        var run = GeneratorHarness.Run($$$"""
            {{{attribute}}}
            public partial class Node : IDeepCopy
            {
                public Node? Next { get; set; }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var root = new Node();
                    var tail = root;

                    for (var index = 1; index < {{{limit}}}; index++)
                    {
                        tail.Next = new Node();
                        tail = tail.Next;
                    }

                    tail.Next = root;
                    root.DeepCopy();
                    new CopyContext().Copy(root);
                    ((IDeepCopyable)root).Copy(new CopyContext());
                    tail.Next = new Node();

                    try
                    {
                        root.DeepCopy();
                        return false;
                    }
                    catch (NotSupportedException)
                    {
                        return true;
                    }
                }
            }
            """);
        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Analyzer_WarnsAndUsesDefaultForInvalidDepth(int depth)
    {
        var run = GeneratorHarness.Run($"[DeepCopy(depth: {depth})] public partial class Model {{ }}");
        var diagnostics = await run.OutputCompilation.WithAnalyzers([new ValidationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "UV006" && diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.Contains("CopyDepth => 5", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
    }

    [Fact]
    public void Compiler_RejectsExcessiveTypeNestingWithoutCrashing()
    {
        var nested = "int";

        for (var index = 0; index < 101; index++)
        {
            nested = $"List<{nested}>";
        }

        var run = GeneratorHarness.Run($"[DeepCopy] public partial class Model {{ public {nested}? Value {{ get; set; }} }}");

        Assert.Empty(run.GeneratedSources);
        Assert.Contains(run.AnalyzerErrors, diagnostic => diagnostic.Id == "UV004");
    }

    [Theory]
    [InlineData("[DeepCopy(depth: 1)]", "new Model { Items = new List<Child>() }", false)]
    [InlineData("[DeepCopy(depth: 2)]", "new Model { Items = new List<Child>() }", true)]
    [InlineData("[DeepCopy(depth: 2)]", "new Model { Items = new List<Child> { new Child() } }", false)]
    [InlineData("[DeepCopy(depth: 3)]", "new Model { Items = new List<Child> { new Child() } }", true)]
    public void DeepCopy_CountsContainersAndIgnoresNestedModelSettings(string attribute, string value, bool succeeds)
    {
        var run = GeneratorHarness.Run($$$"""
            {{{attribute}}}
            public partial class Model
            {
                public List<Child>? Items { get; set; }
            }

            [DeepCopy(depth: 1)]
            public partial class Child
            {
                public int Value { get; set; }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    try
                    {
                        ({{{value}}}).DeepCopy();
                        return true;
                    }
                    catch (NotSupportedException)
                    {
                        return false;
                    }
                }
            }
            """);
        using var assembly = GeneratorHarness.Load(run);
        Assert.Equal(succeeds, (bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }

    [Theory]
    [InlineData("[CopyDepth(0)] public partial class Model : IDeepCopy { }", 5)]
    [InlineData("[CopyDepth(9)] [DeepCopy(depth: 2)] public partial class Model { }", 2)]
    [InlineData("[CopyDepth(9)] public class Model { }", 0)]
    public async Task Analyzer_WarnsAboutInterfaceConfiguration(string source, int effectiveDepth)
    {
        var run = GeneratorHarness.Run(source);
        var diagnostics = await run.OutputCompilation.WithAnalyzers([new ValidationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "UV006");

        if (effectiveDepth != 0)
        {
            Assert.Contains($"CopyDepth => {effectiveDepth}", run.GeneratedText);
        }
    }

    [Fact]
    public void DeepCopy_UsesOneLevelForStructForwarding()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy(depth: 1)]
            public partial struct Model
            {
                public int Value { get; set; }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Model { Value = 42 };
                    return source.DeepCopy().Value == 42 &&
                        new CopyContext().Copy(source).Value == 42 &&
                        ((Model)((IDeepCopyable)source).Copy(new CopyContext())).Value == 42 &&
                        ((Model)new CopyContext().Copy((object)source)).Value == 42;
                }
            }
            """);
        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }
}
