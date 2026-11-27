// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class GenericSequenceCopyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeepCopy_PreservesAliasBetweenGenericAndDeclaredSequence(bool genericFirst)
    {
        const string generic = "public T Generic { get; set; } = default!;";
        const string declared = "public List<int> Declared { get; set; } = new();";

        var run = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial class Holder<T>
            {
                {{(genericFirst ? generic : declared)}}

                {{(genericFirst ? declared : generic)}}
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var values = new List<int> { 1, 2 };
                    var source = new Holder<List<int>> { Generic = values, Declared = values };
                    var copy = source.DeepCopy();

                    return !ReferenceEquals(copy.Generic, values) &&
                        ReferenceEquals(copy.Generic, copy.Declared) &&
                        copy.Declared.SequenceEqual(values);
                }
            }
            """);

        VerifyGeneratedGraph(run);
    }

    [Fact]
    public void DeepCopy_RegistersVisibleModelsForGenericSequenceState()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Holder<T>
            {
                public T Value { get; set; } = default!;
            }

            [DeepCopy]
            public partial class Node
            {
                public Holder<List<Node>>? Owner { get; set; }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Holder<List<Node>>();
                    var node = new Node { Owner = source };
                    source.Value = new List<Node> { node, node };

                    var copy = source.DeepCopy();

                    return !ReferenceEquals(copy.Value, source.Value) &&
                        !ReferenceEquals(copy.Value[0], node) &&
                        ReferenceEquals(copy.Value[0], copy.Value[1]) &&
                        ReferenceEquals(copy.Value[0].Owner, copy);
                }
            }
            """);

        VerifyGeneratedGraph(run);
    }

    [Fact]
    public void Copy_DispatchesRuntimeNestedSequencesAndCycles()
    {
        var inner = new List<int> { 42 };
        var source = new List<object> { inner, inner };
        source.Add(source);

        var copy = Assert.IsType<List<object>>(new CopyContext().Copy<object>(source));
        var innerCopy = Assert.IsType<List<int>>(copy[0]);

        Assert.NotSame(source, copy);
        Assert.NotSame(inner, innerCopy);
        Assert.Equal(42, Assert.Single(innerCopy));
        Assert.Same(innerCopy, copy[1]);
        Assert.Same(copy, copy[2]);
    }

    [Fact]
    public void Copy_PreservesRuntimeSetComparerAndQueueOrder()
    {
        var source = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "First" };
        var context = new CopyContext();
        var copy = Assert.IsType<HashSet<string>>(context.Copy<object>(source));
        var queue = Assert.IsType<Queue<int>>(context.Copy<object>(new Queue<int>([2, 1])));

        Assert.NotSame(source, copy);
        Assert.Same(source.Comparer, copy.Comparer);
        Assert.Contains("first", copy);
        Assert.Equal(new[] { 2, 1 }, queue.ToArray());
    }

    [Fact]
    public void Copy_RequiresRegistrationForHiddenElementTypeEvenWhenEmpty()
    {
        var source = new List<Hidden>();
        var context = new CopyContext();

        var error = Assert.Throws<NotSupportedException>(() => context.Copy<object>(source));
        Assert.Contains("RegisterSequenceType", error.Message);

        CopyContext.RegisterSequenceType<Hidden>();
        var copy = Assert.IsType<List<Hidden>>(context.Copy<object>(source));

        Assert.NotSame(source, copy);
        Assert.Empty(copy);
    }

    [Fact]
    public void Copy_RejectsRuntimeSequenceSubclass()
    {
        Assert.Throws<NotSupportedException>(() => new CopyContext().Copy<object>(new ListSubclass()));
    }

    private static void VerifyGeneratedGraph(GeneratorRun run)
    {
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }

    private sealed class Hidden;

    private sealed class ListSubclass : List<int>;
}
