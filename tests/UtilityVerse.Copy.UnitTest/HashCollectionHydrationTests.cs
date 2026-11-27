// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class HashCollectionHydrationTests
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

    [Theory]
    [InlineData("Dictionary<Key, int>", "Entries.Add(key, 42)", "Entries[key] == 42", false)]
    [InlineData("HashSet<Key>", "Entries.Add(key)", "Entries.Contains(key)", false)]
    [InlineData("System.Collections.Concurrent.ConcurrentDictionary<Key, int>", "Entries.TryAdd(key, 42)", "Entries[key] == 42", false)]
    [InlineData("Dictionary<Key, int>", "Entries.Add(key, 42)", "Entries[key] == 42", true)]
    [InlineData("HashSet<Key>", "Entries.Add(key)", "Entries.Contains(key)", true)]
    [InlineData("System.Collections.Concurrent.ConcurrentDictionary<Key, int>", "Entries.TryAdd(key, 42)", "Entries[key] == 42", true)]
    public void DeepCopy_PreservesHashLookups_WhenKeyDependsOnOwnerHydratedLater(
        string collection,
        string insert,
        string lookup,
        bool useInterface) => Evaluate($$"""
        [DeepCopy]
        public partial class Graph
        {
            public {{collection}} Entries { get; } = new();

            public int Seed { get; set; }

            public Key? Shared { get; set; }
        }

        [DeepCopy]
        public sealed partial class Key
        {
            public Graph? Owner { get; set; }

            public override int GetHashCode() => Owner!.Seed;

            public override bool Equals(object? other) => ReferenceEquals(this, other);
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Graph { Seed = 7 };
                var key = new Key { Owner = source };
                source.{{insert}};
                source.Shared = key;

                var copy = {{(useInterface
                    ? "(Graph)((IDeepCopyable)source).Copy(new CopyContext())"
                    : "source.DeepCopy()")}};
                key = copy.Shared!;

                return copy.{{lookup}} && copy.Entries.Count == 1 &&
                    ReferenceEquals(key.Owner, copy) &&
                    !ReferenceEquals(key, source.Shared) &&
                    !ReferenceEquals(copy.Entries, source.Entries);
            }
        }
        """);

    [Theory]
    [InlineData("Dictionary<Key, int>", "Entries.Add(key, 42)", "Entries[key] == 42")]
    [InlineData("HashSet<Key>", "Entries.Add(key)", "Entries.Contains(key)")]
    [InlineData("System.Collections.Concurrent.ConcurrentDictionary<Key, int>", "Entries.TryAdd(key, 42)", "Entries[key] == 42")]
    public void DeepCopy_DoesNotCollapseDistinctKeys_WhenEqualityDependsOnLaterState(
        string collection,
        string insert,
        string lookup) => Evaluate($$"""
        [DeepCopy]
        public partial class Graph
        {
            public {{collection}} Entries { get; } = new();

            public Graph? Next { get; set; }

            public int Identity { get; set; }
        }

        [DeepCopy]
        public sealed partial class Key
        {
            public Graph? Owner { get; set; }

            public int Id { get; set; }

            public override int GetHashCode() => Owner!.Identity * Id;

            public override bool Equals(object? other) =>
                other is Key key && key.Owner!.Identity * key.Id == Owner!.Identity * Id;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Graph { Identity = 7 };
                var next = new Graph { Identity = 9, Next = source };
                source.Next = next;

                var key = new Key { Owner = source, Id = 1 };
                source.{{insert}};
                key = new Key { Owner = source, Id = 2 };
                source.{{insert}};

                var copy = source.DeepCopy();
                key = new Key { Owner = copy, Id = 1 };
                var firstFound = copy.{{lookup}};
                key = new Key { Owner = copy, Id = 2 };

                return firstFound && copy.{{lookup}} && copy.Entries.Count == 2 &&
                    ReferenceEquals(copy, copy.Next!.Next);
            }
        }
        """);

    [Theory]
    [InlineData("Dictionary<Key, int>", "Entries.Add(key, 42)", "Entries[key] == 42")]
    [InlineData("HashSet<Key>", "Entries.Add(key)", "Entries.Contains(key)")]
    [InlineData("System.Collections.Concurrent.ConcurrentDictionary<Key, int>", "Entries.TryAdd(key, 42)", "Entries[key] == 42")]
    public void DeepCopy_RebuildsHashBuckets_WhenKeyDependsOnAnotherCollectionPopulatedLater(
        string collection,
        string insert,
        string lookup) => Evaluate($$"""
        [DeepCopy]
        public partial class Graph
        {
            public {{collection}} Entries { get; } = new();

            public Dictionary<int, int> Later { get; } = new();

            public Key? Shared { get; set; }
        }

        [DeepCopy]
        public sealed partial class Key
        {
            public Graph? Owner { get; set; }

            public override int GetHashCode() => Owner!.Later.Count;

            public override bool Equals(object? other) => ReferenceEquals(this, other);
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Graph();
                source.Later.Add(7, 9);
                var key = new Key { Owner = source };
                source.{{insert}};
                source.Shared = key;

                var copy = source.DeepCopy();
                key = copy.Shared!;

                return copy.{{lookup}} && copy.Entries.Count == 1 &&
                    copy.Later[7] == 9 && ReferenceEquals(key.Owner, copy);
            }
        }
        """);

    [Theory]
    [InlineData("""
        context.CopyArray(
            new[] { source },
            static (item, copyContext) => copyContext.Copy(item))![0]
        """)]
    [InlineData("""
        context.CopySequence(
            new[] { source },
            static (item, copyContext) => copyContext.Copy(item))!.Single()
        """)]
    [InlineData("""
        context.CopyImmutable(
            Tuple.Create(source),
            item => Tuple.Create(context.Copy(item.Item1)))!.Item1
        """)]
    [InlineData("""
        context.CopyMap<Dictionary<int, Graph>, int, Graph>(
            new Dictionary<int, Graph> { [1] = source },
            static original => new Dictionary<int, Graph>(original.Comparer),
            static (copy, key, item) => copy.Add(key, item),
            static (key, _) => key,
            static (item, copyContext) => copyContext.Copy(item))![1]
        """)]
    public void CopyContext_CompletesPendingCollections_WhenContainerIsTheRoot(string expression) => Evaluate($$"""
        [DeepCopy]
        public partial class Graph
        {
            public Dictionary<Key, int> Entries { get; } = new();

            public int Seed { get; set; }
        }

        [DeepCopy]
        public sealed partial class Key
        {
            public Graph? Owner { get; set; }

            public override int GetHashCode() => Owner!.Seed;

            public override bool Equals(object? other) => ReferenceEquals(this, other);
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Graph { Seed = 7 };
                source.Entries.Add(new Key { Owner = source }, 42);
                var context = new CopyContext();
                var copy = {{expression}};
                var key = copy.Entries.Keys.Single();

                return copy.Entries[key] == 42 && ReferenceEquals(key.Owner, copy) &&
                    !ReferenceEquals(source, copy);
            }
        }
        """);

    [Fact]
    public void CopyContext_DiscardsPendingWorkAndPartialCopies_WhenNestedCopyFails()
    {
        var context = new CopyContext();
        var source = new[] { new HashSet<int> { 1 }, new HashSet<int> { 2 } };
        var fail = true;

        HashSet<int> CopyElement(HashSet<int> values, CopyContext copyContext) =>
            (HashSet<int>)copyContext.CopySequence(values, (value, _) =>
            {
                if (fail && value == 2)
                {
                    throw new InvalidOperationException("Test copy failed.");
                }

                return value;
            })!;

        Assert.Throws<InvalidOperationException>(() => context.CopyArray(source, CopyElement));
        fail = false;

        var copy = context.CopyArray(source, CopyElement)!;

        Assert.NotSame(source, copy);
        Assert.NotSame(source[0], copy[0]);
        Assert.True(copy[0].SetEquals(source[0]));
        Assert.True(copy[1].SetEquals(source[1]));
        Assert.Same(copy, context.CopyArray(source, CopyElement));
    }

    [Fact]
    public void CopyContext_RejectsDuplicateSetElements_AndRemainsReusable()
    {
        var context = new CopyContext();
        var source = new HashSet<int> { 1, 2 };

        Assert.Throws<InvalidOperationException>(() =>
            context.CopySequence(source, static (_, _) => 0));

        var copy = (HashSet<int>)context.CopySequence(source, static (value, _) => value)!;

        Assert.True(copy.SetEquals(source));
        Assert.NotSame(source, copy);
    }

    [Fact]
    public void CopyContext_RejectsDuplicateDictionaryKeys_AndRemainsReusable()
    {
        var context = new CopyContext();
        var source = new Dictionary<int, int> { [1] = 7, [2] = 9 };

        Assert.Throws<ArgumentException>(() => context.CopyMap<Dictionary<int, int>, int, int>(
            source,
            static original => new Dictionary<int, int>(original.Comparer),
            static (copy, key, value) => copy.Add(key, value),
            static (_, _) => 0,
            static (value, _) => value));

        var copy = context.CopyMap<Dictionary<int, int>, int, int>(
            source,
            static original => new Dictionary<int, int>(original.Comparer),
            static (destination, key, value) => destination.Add(key, value),
            static (key, _) => key,
            static (value, _) => value)!;

        Assert.Equal(7, copy[1]);
        Assert.Equal(9, copy[2]);
        Assert.NotSame(source, copy);
    }

    [Fact]
    public void DeepCopy_RejectsUnstableHashing_WhenKeyDependsOnItsOwnCollectionCount() => Evaluate("""
        [DeepCopy]
        public partial class Graph
        {
            public Dictionary<Key, int> Entries { get; } = new();
        }

        [DeepCopy]
        public sealed partial class Key
        {
            public Graph? Owner { get; set; }

            public override int GetHashCode() => Owner!.Entries.Count;

            public override bool Equals(object? other) => ReferenceEquals(this, other);
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Graph();
                source.Entries.Add(new Key { Owner = source }, 42);

                try
                {
                    source.DeepCopy();

                    return false;
                }
                catch (NotSupportedException error)
                {
                    return error.Message.Contains("unstable collection population");
                }
            }
        }
        """);

    [Fact]
    public void CopyContext_RejectsIncompleteGraph_WhenCustomCopierSuppressesNestedFailure()
    {
        var context = new CopyContext();

        Assert.Throws<InvalidOperationException>(() => context.Copy(new SuppressingCopier()));
        Assert.Equal(7, context.Copy(7));
    }

    [Fact]
    public void CopyGenerated_RejectsNullSource_AndRemainsReusable()
    {
        var context = new CopyContext();

        Assert.Throws<ArgumentNullException>(() => context.CopyGenerated<object>(
            null!,
            static (source, _) => source));
        Assert.Equal(7, context.Copy(7));
    }

    [Fact]
    public void CopyGenerated_RejectsNullFactory_AndRemainsReusable()
    {
        var context = new CopyContext();

        Assert.Throws<ArgumentNullException>(() => context.CopyGenerated(new object(), null!));
        Assert.Equal(7, context.Copy(7));
    }

    private sealed class SuppressingCopier : IDeepCopyable
    {
        public object Copy(CopyContext context)
        {
            var copy = new SuppressingCopier();
            context.Register(this, copy);

            try
            {
                context.Copy(new object());
            }
            catch (NotSupportedException)
            {
                // Simulate a custom copier hiding an unsupported child.
            }

            return copy;
        }
    }
}
