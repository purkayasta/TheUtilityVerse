// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using UtilityVerse.Copy.Analyzers;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class EditorDiagnosticsTests
{
    [Fact]
    public void AnalyzerAndGenerator_TolerateRecursiveStructDuringEditing()
    {
        var run = GeneratorHarness.Run("""
            public struct Editing
            {
                public Editing Value;
            }

            public partial class Model : IDeepCopy
            {
                public Editing Value { get; set; }
            }
            """);

        Assert.Contains("CS0523", run.CompilationErrorIds);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(AnalyzerDiagnostics(run));
    }

    [Theory]
    [InlineData("[DeepCopy] file partial class Model { public int Value { get; set; } }", "Model")]
    [InlineData("[ShallowCopy] file partial class Model { }", "Model")]
    [InlineData("file partial class Outer { public partial class Model : IDeepCopy { } }", "Outer")]
    [InlineData("file partial class Outer { public partial class Model : IShallowCopy { } }", "Outer")]
    public void UV004_ReportsFileLocalDeclaration_AndSkipsGeneration(string source, string identifier)
    {
        var run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratedSources);
        Assert.Empty(run.CompilationErrors);
        var error = Assert.Single(AnalyzerDiagnostics(run));
        Assert.Equal("UV004", error.Id);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("file-local", error.GetMessage());
        Assert.Equal(identifier, DiagnosticText(error));
    }

    [Fact]
    public Task UV004_ReportsEveryUnsupportedMemberAtItsDeclaration() => AnalyzerVerifier.VerifyAsync("""
        using System.Collections.Immutable;

        [DeepCopy]
        public partial class Model
        {
            public ImmutableArray<object> {|UV004:First|} { get; set; }

            public ImmutableArray<object> {|UV004:Second|};
        }
        """);

    [Fact]
    public void UV005_WarnsAtIgnoredPolicies_AndLeavesEffectivePoliciesAlone()
    {
        var run = GeneratorHarness.Run("""
            public partial class ShallowModel : IShallowCopy
            {
                [CopyPolicy(CopyBehavior.Reset)]
                public int Value { get; set; }
            }

            [DeepCopy]
            [ShallowCopy]
            public partial class BothModes
            {
                [CopyPolicy(CopyBehavior.Share)]
                public static int Shared { get; set; }

                [CopyPolicy(CopyBehavior.Copy)]
                public int Computed => Stored + 1;

                [field: CopyPolicy(CopyBehavior.Reset)]
                public int Stored { get; set; }

                [CopyPolicy(CopyBehavior.Reset)]
                private int count = 3;

                public int Count => count;
            }
            """);

        Assert.Empty(run.CompilationErrors);
        var diagnostics = AnalyzerDiagnostics(run).OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start);
        Assert.Collection(diagnostics,
            diagnostic => AssertPolicyWarning(diagnostic, "CopyPolicy(CopyBehavior.Reset)", "ShallowCopy"),
            diagnostic => AssertPolicyWarning(diagnostic, "CopyPolicy(CopyBehavior.Share)", "stored"),
            diagnostic => AssertPolicyWarning(diagnostic, "CopyPolicy(CopyBehavior.Copy)", "stored"));
    }

    [Theory]
    [InlineData("[DeepCopy]", "", "DeepCopy()", 1)]
    [InlineData("", " : IDeepCopy", "DeepCopy()", 1)]
    [InlineData("[ShallowCopy]", "", "ShallowCopy()", 1)]
    [InlineData("", " : IShallowCopy", "ShallowCopy()", 1)]
    [InlineData("[DeepCopy]", " : IShallowCopy", "DeepCopy() and ShallowCopy()", 2)]
    public void UV001_ExplainsWhatToAdd_AndClearsWhenPartialIsAdded(
        string attribute,
        string interfaces,
        string methods,
        int generatedCount)
    {
        var invalid = GeneratorHarness.Run($"{attribute}\npublic class Model{interfaces} {{ }}");
        var diagnostic = Assert.Single(AnalyzerDiagnostics(invalid));

        Assert.Empty(invalid.GeneratedSources);
        Assert.Equal("UV001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("Model", DiagnosticText(diagnostic));
        Assert.Equal($"Add 'partial' to the declaration of 'Model' to generate {methods}", diagnostic.GetMessage());

        var corrected = GeneratorHarness.Run($"{attribute}\npublic partial class Model{interfaces} {{ }}");

        Assert.Equal(generatedCount, corrected.GeneratedSources.Length);
        Assert.Empty(corrected.CompilationErrors);
        Assert.Empty(AnalyzerDiagnostics(corrected));
    }

    [Fact]
    public void UV002_IdentifiesTheMemberAndTheSourceTypeToEdit()
    {
        var run = GeneratorHarness.Run("""
            namespace Models;

            public class Address { }

            [DeepCopy]
            public partial class Order
            {
                public List<Address>? addresses;
            }
            """);

        var diagnostic = Assert.Single(AnalyzerDiagnostics(run));

        Assert.Equal("UV002", diagnostic.Id);
        Assert.Equal("addresses", DiagnosticText(diagnostic.Location));
        Assert.Equal("Address", DiagnosticText(Assert.Single(diagnostic.AdditionalLocations)));
        Assert.Contains("Member 'addresses' on 'Models.Order' needs deep-copy support for 'Models.Address'", diagnostic.GetMessage());
        Assert.Contains("Make that type partial and add [DeepCopy] or implement IDeepCopy", diagnostic.GetMessage());
    }

    [Fact]
    public void UV002_ExplainsHowToHandleAnExternalType()
    {
        var run = GeneratorHarness.Run("""
            public partial class Model : IDeepCopy
            {
                public System.IO.MemoryStream? Stream { get; set; }
            }
            """);

        var diagnostic = Assert.Single(AnalyzerDiagnostics(run));

        Assert.Equal("UV002", diagnostic.Id);
        Assert.Empty(diagnostic.AdditionalLocations);
        Assert.Contains("System.IO.MemoryStream", diagnostic.GetMessage());
        Assert.Contains("[CopyPolicy(CopyBehavior.Custom)]", diagnostic.GetMessage());
        Assert.Contains("RegisterExternalCopier", diagnostic.GetMessage());
        Assert.DoesNotContain("Make that type partial", diagnostic.GetMessage());
    }

    [Fact]
    public void UV004_NamesTheContainingDeclarationAndHowToFixIt()
    {
        var run = GeneratorHarness.Run("""
            public class Container
            {
                public partial class Model : IShallowCopy { }
            }
            """);

        var diagnostic = Assert.Single(AnalyzerDiagnostics(run));

        Assert.Equal("UV004", diagnostic.Id);
        Assert.Contains("Add 'partial' to the containing type 'Container'", diagnostic.GetMessage());
        Assert.Contains("ShallowCopy()", diagnostic.GetMessage());
    }

    [Fact]
    public void UV005_NamesThePolicyAndExplainsHowToMakeItEffective()
    {
        var run = GeneratorHarness.Run("""
            public partial class Model : IShallowCopy
            {
                [CopyPolicy(CopyBehavior.Reset)]
                public int Count { get; set; }
            }
            """);

        var diagnostic = Assert.Single(AnalyzerDiagnostics(run));

        Assert.Equal("UV005", diagnostic.Id);
        Assert.Contains("CopyPolicy 'Reset' on 'Model.Count'", diagnostic.GetMessage());
        Assert.Contains("Remove the attribute if shallow copying is intended", diagnostic.GetMessage());
        Assert.Contains("implement IDeepCopy", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("UV001")]
    [InlineData("UV002")]
    [InlineData("UV004")]
    [InlineData("UV005")]
    public void DiagnosticRule_ProvidesDescriptionAndRuleSpecificHelp(string id)
    {
        var descriptor = Assert.Single(new ValidationAnalyzer().SupportedDiagnostics, rule => rule.Id == id);

        Assert.False(string.IsNullOrWhiteSpace(descriptor.Description.ToString()));
        Assert.EndsWith("/src/UtilityVerse.Copy/README.md#" + id.ToLowerInvariant(), descriptor.HelpLinkUri);
    }

    private static IReadOnlyList<Diagnostic> AnalyzerDiagnostics(GeneratorRun run) =>
        run.OutputCompilation.WithAnalyzers([new ValidationAnalyzer()])
            .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken).GetAwaiter().GetResult();

    private static string DiagnosticText(Diagnostic diagnostic) => DiagnosticText(diagnostic.Location);

    private static string DiagnosticText(Location location) =>
        location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(location.SourceSpan);

    private static void AssertPolicyWarning(Diagnostic diagnostic, string text, string message)
    {
        Assert.Equal("UV005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(text, DiagnosticText(diagnostic));
        Assert.Contains(message, diagnostic.GetMessage());
    }
}
