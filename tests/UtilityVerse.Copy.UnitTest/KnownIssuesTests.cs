// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class KnownIssuesTests
{
    [Fact]
    [Trait("KnownIssue", "shallow-collections-not-rewrapped")]
    public void ShallowCopy_SharesCollectionReference_WhenPropertyIsGenericCollection()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [ShallowCopy]
            public partial class Basket
            {
                public List<int>? Items { get; set; }
            }
            """);
        using var assembly = GeneratorHarness.Load(run);
        var original = assembly.Create("Sample.Basket");
        original.Items = new List<int> { 1 };

        var copy = original.ShallowCopy();

        Assert.DoesNotContain("copy.Items =", run.GeneratedText);
        Assert.Same(original.Items, copy.Items);
    }

    [Theory]
    [Trait("KnownIssue", "deep-nonassignable-collections")]
    [InlineData("Queue<int>")]
    [InlineData("Stack<int>")]
    [InlineData("System.Collections.ObjectModel.ObservableCollection<int>")]
    [InlineData("System.Collections.Concurrent.ConcurrentDictionary<int, int>")]
    public void DeepCopy_GeneratesNonCompilingAssignment_WhenCollectionIsNotAssignableFromListOrDictionary(string collectionType)
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            [DeepCopy]
            public partial class Holder
            {
                public {{collectionType}}? Values { get; set; }
            }
            """);

        Assert.Equal(["CS0029"], run.CompilationErrorIds);
    }

    [Fact]
    [Trait("KnownIssue", "analyzer-skips-array-elements")]
    public async Task Analyzer_ReportsNothingButGeneratedCodeFails_WhenArrayElementIsNotOptedIn()
    {
        const string source = """
            public class Foo
            {
                public int Id { get; set; }
            }

            [DeepCopy]
            public partial class Holder
            {
                public Foo[]? Items { get; set; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync(source);
        Assert.Equal(["CS1061"], GeneratorHarness.Run(source).CompilationErrorIds);
    }

    [Fact]
    [Trait("KnownIssue", "tuple-false-positive")]
    public Task UV002_Reports_WhenPropertyIsSystemTupleOfPrimitives() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public partial class Holder
        {
            public Tuple<int, string>? {|UV002:Pair|} { get; set; }
        }
        """);

    [Fact]
    [Trait("KnownIssue", "attribute-not-on-structs")]
    public void DeepCopyAttribute_CausesCS0592_WhenAppliedToStruct()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial struct Money
            {
                public decimal Amount { get; set; }
            }
            """);

        Assert.Equal(["CS0592"], run.CompilationErrorIds);
    }

    [Fact]
    [Trait("KnownIssue", "nested-type-wrong-scope")]
    public void DeepCopy_EmitsPartialAtNamespaceLevel_WhenOptedInTypeIsNested()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            public partial class Outer
            {
                [DeepCopy]
                public partial class Inner
                {
                    public int Id { get; set; }
                }
            }
            """);

        Assert.DoesNotContain("partial class Outer", run.GeneratedText);
        Assert.Equal(["CS0117", "CS1061"], run.CompilationErrorIds.Order());
    }

    [Fact]
    [Trait("KnownIssue", "generic-type-params-dropped")]
    public void Generator_FailsWithInvalidHintNameAndEmitsNothing_WhenOptedInTypeIsGeneric()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Box<T>
            {
                public T? Value { get; set; }
            }

            [DeepCopy]
            public partial class Unrelated
            {
                public int Id { get; set; }
            }
            """);

        var failure = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("CS8785", failure.Id);
        Assert.Contains("Sample_Box<T>_DeepCopy.g.cs", failure.GetMessage());
        Assert.Empty(run.GeneratedSources);
    }

    [Fact]
    [Trait("KnownIssue", "frozen-using-unconditional")]
    public void Generator_EmitsFrozenUsing_WhenAnyTypeIsGenerated()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Deep { public int Id { get; set; } }

            [ShallowCopy]
            public partial class Shallow { public int Id { get; set; } }
            """);

        Assert.Equal(2, run.GeneratedSources.Length);
        Assert.All(run.GeneratedSources, s => Assert.Contains("using System.Collections.Frozen;", s.SourceText.ToString()));
    }

    [Fact]
    [Trait("KnownIssue", "recursive-collections")]
    public async Task DeepCopy_CallsDeepCopyOnInnerList_WhenPropertyIsListOfLists()
    {
        const string source = """
            [DeepCopy]
            public partial class Foo
            {
                public int Id { get; set; }
            }

            [DeepCopy]
            public partial class Grid
            {
                public List<List<Foo>>? Rows { get; set; }
            }
            """;

        var run = GeneratorHarness.Run(source);

        await AnalyzerVerifier.VerifyAsync(source);
        Assert.Contains("Rows = this.Rows?.Select(x => x?.DeepCopy()).ToList(),", run.GeneratedText);
        Assert.Equal(["CS1061"], run.CompilationErrorIds);
    }

    [Fact]
    [Trait("KnownIssue", "ctor-path-drops-properties")]
    public void DeepCopy_DropsPropertyNotCoveredByConstructor_WhenNoParameterlessConstructor()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Order
            {
                public Order(int id) => Id = id;

                public int Id { get; set; }
                public string? Note { get; set; }
            }
            """);
        using var assembly = GeneratorHarness.Load(run);
        var original = assembly.Create("Sample.Order", 7);
        original.Note = "fragile";

        var copy = original.DeepCopy();

        Assert.Equal(7, (int)copy.Id);
        Assert.Null((string?)copy.Note);
    }

    [Fact]
    [Trait("KnownIssue", "nonpublic-silent")]
    public async Task Generator_EmitsNothingAndAnalyzerIsSilent_WhenOptedInTypeIsInternal()
    {
        const string source = """
            [DeepCopy]
            internal partial class Hidden
            {
                public int Id { get; set; }
            }
            """;

        await AnalyzerVerifier.VerifyAsync(source);
        Assert.Empty(GeneratorHarness.Run(source).GeneratedSources);
    }

    [Fact]
    [Trait("KnownIssue", "nullable-reference-builtins")]
    public void DeepCopy_CallsDeepCopyOnBuiltIn_WhenReferenceBuiltInIsNullableAnnotated()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Link
            {
                public Uri? Target { get; set; }
            }
            """);

        Assert.Contains("Target = this.Target?.DeepCopy(),", run.GeneratedText);
        Assert.Equal(["CS1061"], run.CompilationErrorIds);
    }

    [Fact]
    [Trait("KnownIssue", "tuple-codegen-errors")]
    public void DeepCopy_GeneratesNonCompilingTupleCopy_WhenTupleIsNullableOrRepeated()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Pairs
            {
                public Tuple<int, string>? First { get; set; }
                public Tuple<int, string> Second { get; set; } = null!;
            }
            """);

        Assert.Equal(["CS0128", "CS0165", "CS8628"], run.CompilationErrorIds.Order());
    }
}
