// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Reflection;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class MapRuntimeTypeTests
{
    [Theory]
    [InlineData("Dictionary", false)]
    [InlineData("Dictionary", true)]
    [InlineData("ConcurrentDictionary", false)]
    [InlineData("ConcurrentDictionary", true)]
    public void DeepCopy_RejectsUnsupportedMapSubclass_WithoutLosingAddedState(string map, bool populated)
    {
        var run = GeneratorHarness.Run(ModelSource(map));

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);

        var check = assembly.GetType("Check").GetMethod("CopyMap")!;
        var invocation = Assert.Throws<TargetInvocationException>(() => check.Invoke(null, ["derived", populated]));
        var error = Assert.IsType<NotSupportedException>(invocation.InnerException);

        Assert.Contains("LabeledMap", error.Message);
    }

    [Theory]
    [InlineData("Dictionary", "null", false)]
    [InlineData("Dictionary", "exact", false)]
    [InlineData("Dictionary", "exact", true)]
    [InlineData("ConcurrentDictionary", "null", false)]
    [InlineData("ConcurrentDictionary", "exact", false)]
    [InlineData("ConcurrentDictionary", "exact", true)]
    public void DeepCopy_PreservesSupportedMapStateAndGraph(string map, string shape, bool populated)
    {
        var run = GeneratorHarness.Run(ModelSource(map));

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);

        var check = assembly.GetType("Check").GetMethod("CopyMap")!;

        Assert.True((bool)check.Invoke(null, [shape, populated])!);
    }

    [Fact]
    public void CopyMap_RejectsTypeChangingFactory_AndAllowsRetryWithSubclassFactory()
    {
        var context = new CopyContext();
        var source = new LabeledDictionary(StringComparer.OrdinalIgnoreCase, "important") { ["key"] = 7 };
        var copyCalls = 0;

        var error = Assert.Throws<NotSupportedException>(() =>
            context.CopyMap<Dictionary<string, int>, string, int>(
                source,
                static map => new Dictionary<string, int>(map.Comparer),
                static (map, key, value) => map.Add(key, value),
                static (key, _) => key,
                (value, _) =>
                {
                    copyCalls++;

                    return value;
                }));

        Assert.Contains(nameof(LabeledDictionary), error.Message);
        Assert.Equal(0, copyCalls);
        Assert.Equal(7, source["KEY"]);

        var copy = context.CopyMap<Dictionary<string, int>, string, int>(
            source,
            static map => new LabeledDictionary(map.Comparer, ((LabeledDictionary)map).Label),
            static (map, key, value) => map.Add(key, value),
            static (key, _) => key,
            static (value, _) => value);

        var labeledCopy = Assert.IsType<LabeledDictionary>(copy);

        Assert.NotSame(source, labeledCopy);
        Assert.Equal(source.Label, labeledCopy.Label);
        Assert.Same(source.Comparer, labeledCopy.Comparer);
        Assert.Equal(7, labeledCopy["KEY"]);
    }

    private static string ModelSource(string map) => $$"""
        using System.Collections.Concurrent;

        [DeepCopy]
        public sealed partial class Model
        {
            public {{map}}<string, Model>? Entries { get; set; }

            public {{map}}<string, Model>? Alias { get; set; }
        }

        public sealed class LabeledMap : {{map}}<string, Model>
        {
            public string Label { get; } = "important";
        }

        public static class Check
        {
            public static bool CopyMap(string shape, bool populated)
            {
                {{map}}<string, Model>? entries = shape switch
                {
                    "derived" => new LabeledMap(),
                    "exact" => new {{map}}<string, Model>(StringComparer.OrdinalIgnoreCase),
                    _ => null
                };
                var source = new Model { Entries = entries, Alias = entries };

                if (populated)
                {
                    entries!["key"] = source;
                }

                var copy = source.DeepCopy();

                if (entries is null)
                {
                    return copy.Entries is null && copy.Alias is null;
                }

                return copy.Entries is not null &&
                    copy.Entries.GetType() == entries.GetType() &&
                    !ReferenceEquals(entries, copy.Entries) &&
                    ReferenceEquals(copy.Entries, copy.Alias) &&
                    ReferenceEquals(copy.Entries.Comparer, entries.Comparer) &&
                    copy.Entries.Count == entries.Count &&
                    (!populated || ReferenceEquals(copy.Entries["KEY"], copy));
            }
        }
        """;

    private sealed class LabeledDictionary(IEqualityComparer<string> comparer, string label)
        : Dictionary<string, int>(comparer)
    {
        internal string Label { get; } = label;
    }
}
