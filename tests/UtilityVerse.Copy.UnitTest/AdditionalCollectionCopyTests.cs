// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class AdditionalCollectionCopyTests
{
    [Theory]
    [InlineData("Dictionary", false)]
    [InlineData("Dictionary", true)]
    [InlineData("ConcurrentDictionary", false)]
    [InlineData("SortedDictionary", true)]
    [InlineData("SortedList", false)]
    public void DeepCopy_PreservesSupportedMapAcrossInterfaceAndConcreteViews(string kind, bool concreteFirst)
    {
        var concrete = kind == "ConcurrentDictionary"
            ? "System.Collections.Concurrent.ConcurrentDictionary<string, Node>"
            : $"{kind}<string, Node>";
        var typed = $"public {concrete} Typed {{ get; set; }} = new(StringComparer.OrdinalIgnoreCase);";
        const string wide = "public IReadOnlyDictionary<string, Node>? Wide { get; set; }";

        var run = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial class Model
            {
                {{(concreteFirst ? typed : wide)}}

                {{(concreteFirst ? wide : typed)}}

                public IDictionary<string, Node>? Mutable { get; set; }
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
                    source.Typed["key"] = node;
                    source.Wide = source.Typed;
                    source.Mutable = source.Typed;

                    var copy = source.DeepCopy();

                    return ReferenceEquals(copy.Typed, copy.Wide) &&
                        ReferenceEquals(copy.Typed, copy.Mutable) &&
                        !ReferenceEquals(copy.Typed, source.Typed) &&
                        ReferenceEquals(copy.Typed.Comparer, source.Typed.Comparer) &&
                        copy.Mutable!.ContainsKey("KEY") &&
                        !ReferenceEquals(copy.Typed["key"], node) &&
                        ReferenceEquals(copy.Typed["key"].Owner, copy);
                }
            }
            """);

        VerifyGraph(run);
    }

    [Fact]
    public void DeepCopy_RejectsUnknownMapRuntimeTypesThroughDictionaryInterfaces()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Model
            {
                public IDictionary<string, int>? Map { get; set; }
            }

            public sealed class SpecialMap : Dictionary<string, int>
            {
            }

            public static class Check
            {
                public static void Copy()
                {
                    var source = new Model { Map = new SpecialMap() };
                    source.DeepCopy();
                }
            }
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);
        var invocation = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => assembly.GetType("Check").GetMethod("Copy")!.Invoke(null, null));
        Assert.Contains("SpecialMap", Assert.IsType<NotSupportedException>(invocation.InnerException).Message);
    }

    [Fact]
    public void DeepCopy_PreservesLinkedAndSortedSequences()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Model
            {
                public IEnumerable<int>? Sequence { get; set; }

                public LinkedList<int> Linked { get; set; } = new();

                public SortedSet<string> Sorted { get; set; } = new(StringComparer.OrdinalIgnoreCase);
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Model();
                    source.Linked.AddLast(2);
                    source.Linked.AddLast(1);
                    source.Sequence = source.Linked;
                    source.Sorted.Add("z");
                    source.Sorted.Add("a");
                    var copy = source.DeepCopy();

                    return !ReferenceEquals(copy.Linked, source.Linked) &&
                        ReferenceEquals(copy.Linked, copy.Sequence) &&
                        copy.Linked.SequenceEqual(new[] { 2, 1 }) &&
                        !ReferenceEquals(copy.Sorted, source.Sorted) &&
                        ReferenceEquals(copy.Sorted.Comparer, source.Sorted.Comparer) &&
                        copy.Sorted.Contains("A") &&
                        copy.Sorted.SequenceEqual(new[] { "a", "z" });
                }
            }
            """);

        VerifyGraph(run);
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
