// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class RecordInheritanceCopyTests
{
    [Fact]
    public void DeepCopy_PreservesPrivateBaseStateInOptedInRecordHierarchy()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public abstract partial record Base(string Name)
            {
                private readonly List<Base> links = new();

                public List<Base> Links => links;
            }

            [DeepCopy]
            public sealed partial record Derived : Base
            {
                public static int ConstructorCalls;

                public Derived(string name) : base(name)
                {
                    ConstructorCalls++;
                }

                public List<Base> Alias { get; set; } = new();
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Derived("name");
                    source.Links.Add(source);
                    source.Alias = source.Links;
                    Base baseView = source;

                    var copy = (Derived)baseView.DeepCopy();
                    var shallow = source with { };

                    return Derived.ConstructorCalls == 1 &&
                        copy.Name == source.Name &&
                        !ReferenceEquals(source, copy) &&
                        !ReferenceEquals(source.Links, copy.Links) &&
                        ReferenceEquals(copy.Links, copy.Alias) &&
                        ReferenceEquals(copy, copy.Links[0]) &&
                        ReferenceEquals(shallow.Links, source.Links);
                }
            }
            """);

        VerifyGraph(run);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeepCopy_PreservesAccessibleStateInPositionalRecordHierarchy(bool optedInBase)
    {
        var baseOptIn = optedInBase ? "[DeepCopy]" : "";
        var run = GeneratorHarness.Run($$"""
            {{baseOptIn}}
            public partial record Base(int Id)
            {
                public List<int> Values { get; init; } = new();
            }

            [DeepCopy]
            public partial record Derived(int Id, List<int> Alias) : Base(Id);

            public static class Check
            {
                public static bool Verify()
                {
                    var values = new List<int> { 7 };
                    var source = new Derived(42, values) { Values = values };
                    var copy = source.DeepCopy();

                    return copy.Id == 42 &&
                        !ReferenceEquals(source.Values, copy.Values) &&
                        ReferenceEquals(copy.Alias, copy.Values) &&
                        copy.Values.SequenceEqual(source.Values);
                }
            }
            """);

        VerifyGraph(run);
    }

    [Fact]
    public void DeepCopy_RejectsCustomRecordCopyConstructorInUnoptedBase()
    {
        var run = GeneratorHarness.Run("""
            public record Base
            {
                public List<int> Values { get; init; } = new();

                public Base()
                {
                }

                protected Base(Base source)
                {
                    source.Values.Add(99);
                    Values = source.Values;
                }
            }

            [DeepCopy]
            public partial record Derived : Base;
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.GeneratedSources);

        var diagnostic = Assert.Single(run.AnalyzerErrors);
        Assert.Equal("UV004", diagnostic.Id);
        Assert.Contains("custom record copy constructor", diagnostic.GetMessage());
        Assert.Contains("Base", diagnostic.GetMessage());
    }

    [Fact]
    public void DeepCopy_RejectsPositionalRecordWithInaccessibleMutableBaseState()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial record Base(int Id)
            {
                private readonly List<int> values = new();

                public List<int> Values => values;
            }

            [DeepCopy]
            public partial record Derived(int Id, int Count) : Base(Id);
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Equal("Base_DeepCopy.g.cs", Assert.Single(run.HintNames));

        var diagnostic = Assert.Single(run.AnalyzerErrors);
        Assert.Equal("UV004", diagnostic.Id);
        Assert.Contains("inaccessible or readonly", diagnostic.GetMessage());
    }

    private static void VerifyGraph(GeneratorRun run)
    {
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }
}
