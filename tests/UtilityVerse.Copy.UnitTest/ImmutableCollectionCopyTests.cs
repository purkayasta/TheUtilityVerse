// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class ImmutableCollectionCopyTests
{
    [Fact]
    public void DeepCopy_PreservesImmutableAndFrozenCollectionsAndSharedChildren()
    {
        var run = GeneratorHarness.Run("""
            using System.Collections.Frozen;
            using System.Collections.Immutable;

            [DeepCopy]
            public partial class Model
            {
                public ImmutableList<Node>? List { get; set; }

                public ImmutableList<Node>? Alias { get; set; }

                public ImmutableDictionary<string, Node>? Map { get; set; }

                public ImmutableSortedDictionary<string, Node>? SortedMap { get; set; }

                public ImmutableHashSet<string>? Set { get; set; }

                public ImmutableSortedSet<string>? SortedSet { get; set; }

                public FrozenDictionary<string, Node>? FrozenMap { get; set; }

                public FrozenSet<string>? FrozenSet { get; set; }

                public ImmutableArray<int> Array { get; set; }
            }

            [DeepCopy]
            public partial class Node
            {
                public Model? Owner { get; set; }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Model();
                    var node = new Node { Owner = source };
                    var comparer = StringComparer.OrdinalIgnoreCase;
                    var entries = new Dictionary<string, Node> { ["key"] = node };
                    source.List = ImmutableList.Create(node);
                    source.Alias = source.List;
                    source.Map = ImmutableDictionary.Create<string, Node>(comparer).Add("key", node);
                    source.SortedMap = ImmutableSortedDictionary.Create<string, Node>(comparer).Add("key", node);
                    source.Set = ImmutableHashSet.Create(comparer, "key");
                    source.SortedSet = ImmutableSortedSet.Create(comparer, "key");
                    source.FrozenMap = entries.ToFrozenDictionary(comparer);
                    source.FrozenSet = new[] { "key" }.ToFrozenSet(comparer);
                    source.Array = ImmutableArray.Create(42);

                    var copy = source.DeepCopy();
                    var child = copy.List![0];

                    return !ReferenceEquals(source.List, copy.List) &&
                        ReferenceEquals(copy.List, copy.Alias) &&
                        !ReferenceEquals(node, child) &&
                        ReferenceEquals(copy, child.Owner) &&
                        ReferenceEquals(child, copy.Map!["KEY"]) &&
                        ReferenceEquals(child, copy.SortedMap!["KEY"]) &&
                        ReferenceEquals(child, copy.FrozenMap!["KEY"]) &&
                        copy.Set!.Contains("KEY") &&
                        copy.SortedSet!.Contains("KEY") &&
                        copy.FrozenSet!.Contains("KEY") &&
                        copy.Array[0] == 42;
                }
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }

    [Fact]
    public void DeepCopy_RejectsCyclesThroughImmutableContainers()
    {
        var run = GeneratorHarness.Run("""
            using System.Collections.Immutable;

            [DeepCopy]
            public partial class Node
            {
                public ImmutableList<Node>? Container { get; set; }
            }

            public static class Check
            {
                public static void Copy()
                {
                    var first = new Node();
                    var second = new Node();
                    first.Container = ImmutableList.Create(second);
                    second.Container = first.Container;

                    first.DeepCopy();
                }
            }
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);
        var invocation = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => assembly.GetType("Check").GetMethod("Copy")!.Invoke(null, null));
        Assert.Contains("immutable", Assert.IsType<NotSupportedException>(invocation.InnerException).Message);
    }

    [Fact]
    public void Analyzer_RejectsNonScalarImmutableKeys()
    {
        var run = GeneratorHarness.Run("""
            using System.Collections.Frozen;

            [DeepCopy]
            public partial class Key
            {
                public string? Value { get; set; }
            }

            [DeepCopy]
            public partial class Model
            {
                public FrozenDictionary<Key, int>? Map { get; set; }
            }
            """);

        Assert.Empty(run.CompilationErrors);

        var diagnostic = Assert.Single(run.AnalyzerErrors);
        Assert.Equal("UV004", diagnostic.Id);
        Assert.Contains("immutable scalar", diagnostic.GetMessage());
    }
}
