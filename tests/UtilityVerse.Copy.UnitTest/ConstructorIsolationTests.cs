// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class ConstructorIsolationTests
{
    private static void Evaluate(string source)
    {
        var run = GeneratorHarness.Run(source);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Evaluate")!.Invoke(null, null)!);
    }

    [Fact]
    public void DeepCopy_DoesNotMutateSource_WhenConstructorMutatesItsArgument() => Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            public static int Calls;

            private readonly List<int> values;

            public List<int> Values => values;

            public Item(List<int> values)
            {
                Calls++;
                values.Add(7);
                this.values = values;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item(new());
                var copy = source.DeepCopy();

                return Item.Calls == 1 && source.Values.SequenceEqual(new[] { 7 }) &&
                    copy.Values.SequenceEqual(new[] { 7 }) && !ReferenceEquals(source.Values, copy.Values);
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesCyclesAndAliases_WhenConstructorRequiresMutableState() => Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            public static int Calls;

            public List<Item> Children
            {
                get;
            }

            public List<Item> Alias
            {
                get;
            }

            public Item(List<Item> children)
            {
                Calls++;
                Children = children;
                Alias = children;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item(new());
                source.Children.Add(source);
                var copy = source.DeepCopy();

                return Item.Calls == 1 && source.Children.Count == 1 &&
                    ReferenceEquals(copy.Children, copy.Alias) && ReferenceEquals(copy, copy.Children[0]);
            }
        }
        """);

    [Fact]
    public void DeepCopy_BypassesConstructorsAcrossHierarchy_WhenBaseHasReadonlyState() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public static int Calls;

            private readonly List<int> values;

            public List<int> Values => values;

            public Base(List<int> values)
            {
                Calls++;
                values.Add(7);
                this.values = values;
            }
        }

        [DeepCopy]
        public partial class Item : Base
        {
            public static int DerivedCalls;

            public Item(List<int> values) : base(values) { DerivedCalls++; }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item(new());
                var copy = source.DeepCopy();

                return Base.Calls == 1 && Item.DerivedCalls == 1 && source.Values.Count == 1 &&
                    copy.Values.Count == 1 && !ReferenceEquals(source.Values, copy.Values);
            }
        }
        """);

    [Fact]
    public void DeepCopy_BypassesParameterlessConstructor_WhenItThrowsOnSecondInvocation() => Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            public static int Calls;

            public int Value { get; } = 7;

            public Item()
            {
                if (++Calls > 1)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public static class Check
        {
            public static bool Evaluate() => new Item().DeepCopy().Value == 7 && Item.Calls == 1;
        }
        """);

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record struct")]
    public void DeepCopy_CopiesPrimaryConstructorState_WhenArgumentsAreValues(string kind) => Evaluate($$"""
        [DeepCopy]
        public partial {{kind}} Item(int id)
        {
            public int Id { get; } = id;
        }

        public static class Check
        {
            public static bool Evaluate() => new Item(7).DeepCopy().Id == 7;
        }
        """);

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    [InlineData("record struct")]
    public void DeepCopy_RejectsMutablePrimaryConstructorArguments_BeforeGeneratingUnsafeCode(string kind)
    {
        var run = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial {{kind}} Item(List<int> values)
            {
                public List<int> Values { get; } = values;
            }
            """);
        Assert.Empty(run.GeneratedSources);
        Assert.Contains(run.AnalyzerErrors, d => d.Id == "UV004" && d.GetMessage().Contains("primary constructor"));
    }

    [Fact]
    public void DeepCopy_RejectsCustomRecordCopyConstructor_BeforeItCanMutateSource()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial record Item
            {
                public List<int> Values { get; } = new();

                protected Item(Item source)
                {
                    source.Values.Add(7);
                    Values = source.Values;
                }
            }
            """);
        Assert.Empty(run.GeneratedSources);
        Assert.Contains(run.AnalyzerErrors, d => d.Id == "UV004" && d.GetMessage().Contains("record copy constructor"));
    }

    [Fact]
    public void DeepCopy_DoesNotReplayRecordInitializers_WhenSynthesizedCopyConstructorIsUsed() => Evaluate("""
        [DeepCopy]
        public partial record Item(List<Item> Children)
        {
            public static int Calls;

            public int Id { get; } = ++Calls;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item(new());
                source.Children.Add(source);
                var copy = source.DeepCopy();

                return Item.Calls == 1 && copy.Id == source.Id && ReferenceEquals(copy, copy.Children[0]) &&
                    !ReferenceEquals(source.Children, copy.Children);
            }
        }
        """);

    [Theory]
    [InlineData("""
        public struct Payload
        {
            public List<int>? Values;
        }

        [DeepCopy]
        public partial class Item(Payload payload)
        {
            public Payload Payload { get; } = payload;
        }
        """)]
    [InlineData("""
        public struct Payload
        {
            public Uri? Value;
        }

        [DeepCopy]
        public partial class Item(Payload payload)
        {
            public Payload Payload { get; } = payload;
        }
        """)]
    [InlineData("""
        [DeepCopy]
        public partial class Item(int id)
        {
            public int Id => 7;
        }
        """)]
    [InlineData("""
        [DeepCopy]
        public partial class Base
        {
        }

        [DeepCopy]
        public partial class Item(int id) : Base
        {
            public int Id { get; } = id;
        }
        """)]
    [InlineData("""
        public class Base
        {
            public Base(int id)
            {
            }
        }

        [DeepCopy]
        public partial class Item : Base
        {
            public Item() : base(7) { }
        }
        """)]
    public void DeepCopy_ReportsUnsupportedConstruction_WhenSourceIsolationCannotBeGuaranteed(string source)
    {
        var run = GeneratorHarness.Run(source);
        Assert.DoesNotContain(run.GeneratedSources, s => s.HintName.StartsWith("Item_", StringComparison.Ordinal));
        Assert.Contains(run.AnalyzerErrors, d => d.Id == "UV004" && d.GetMessage().Contains("Item"));
    }
}
