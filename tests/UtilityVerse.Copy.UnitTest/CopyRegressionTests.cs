// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class CopyRegressionTests
{
    private const string Node = """
        [DeepCopy]
        public partial class Node
        {
            public int Id
            {
                get; set;
            }

            public Node? Next
            {
                get; set;
            }
        }
        """;

    private static bool Evaluate(string source)
    {
        var run = GeneratorHarness.Run(source);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.OutputCompilation.GetDiagnostics().Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning &&
            d.Location.SourceTree?.FilePath.EndsWith(".g.cs", StringComparison.Ordinal) == true));
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);

        return (bool)assembly.GetType("Check").GetMethod("Evaluate")!.Invoke(null, null)!;
    }

    [Fact]
    public void DeepCopy_PreservesCycleAndSharedReferences_WhenGraphContainsBoth() => Assert.True(Evaluate(Node + """
        [DeepCopy]
        public partial class Holder
        {
            public Node? A
            {
                get; set;
            }

            public Node? B
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var node = new Node { Id = 7 };
                node.Next = node;
                var source = new Holder { A = node, B = node };
                var copy = source.DeepCopy();

                return !ReferenceEquals(copy.A, node) && ReferenceEquals(copy.A, copy.B) && ReferenceEquals(copy.A, copy.A!.Next) && copy.A.Id == 7;
            }
        }
        """));

    [Fact]
    public void DeepCopy_PreservesFieldsAndStoredGetOnlyProperties_WhenStateIsReadonly() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            private readonly List<int> data = new();

            public int Id;

            public List<int> Values { get; } = new();

            public int Count => data.Count;

            public void Add(int value) => data.Add(value);
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item { Id = 7 };
                source.Values.Add(1);
                source.Add(2);
                var copy = source.DeepCopy();
                source.Values[0] = 9;
                source.Add(3);

                return copy.Id == 7 && copy.Values[0] == 1 && copy.Count == 1;
            }
        }
        """));

    [Fact]
    public void DeepCopy_PreservesConstructorAndRemainingState_WhenNoDefaultConstructorExists() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            public Item(int id, List<int> values)
            {
                Id = id;
                Values = values;
            }

            public int Id
            {
                get;
            }

            public List<int> Values
            {
                get;
            }

            public string? Note
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item(7, new() { 1 }) { Note = "fragile" };
                var copy = source.DeepCopy();
                source.Values[0] = 9;

                return copy.Id == 7 && copy.Note == "fragile" && copy.Values[0] == 1;
            }
        }
        """));

    [Fact]
    public void DeepCopy_ClonesStructAndTupleReferences_WhenStateContainsMutableValues() => Assert.True(Evaluate(Node + """
        [DeepCopy]
        public partial struct Payload
        {
            public List<Node>? Values
            {
                get; set;
            }
        }

        [DeepCopy]
        public partial class Item
        {
            public Payload Data
            {
                get; set;
            }

            public Payload? Optional
            {
                get; set;
            }

            public (int Id, Node Node) Pair
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var node = new Node { Id = 7 };
                var data = new Payload { Values = new() { node } };
                var source = new Item { Data = data, Optional = data, Pair = (1, node) };
                var copy = source.DeepCopy();
                node.Id = 9;

                return copy.Data.Values![0].Id == 7 && copy.Optional!.Value.Values![0].Id == 7 && copy.Pair.Node.Id == 7 &&
                    ReferenceEquals(copy.Data.Values, copy.Optional.Value.Values) && ReferenceEquals(copy.Data.Values[0], copy.Pair.Node);
            }
        }
        """));

    [Fact]
    public void DeepCopy_PreservesRuntimeTypeAndBaseState_WhenPropertyIsPolymorphic() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            private readonly List<int> data = new();

            public virtual int Id
            {
                get; set;
            }

            public int Count => data.Count;

            public void Add(int value) => data.Add(value);
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public override int Id
            {
                get; set;
            }

            public List<int> Extra { get; } = new();
        }

        [DeepCopy]
        public partial class Holder
        {
            public Base? Value
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived { Id = 7 };
                source.Add(1);
                source.Extra.Add(2);
                var copy = (Derived)new Holder { Value = source }.DeepCopy().Value!;
                source.Add(3);
                source.Extra[0] = 9;

                return copy.Id == 7 && copy.Count == 1 && copy.Extra[0] == 2;
            }
        }
        """));

    [Fact]
    public void DeepCopy_PreservesAbstractBaseState_WhenConcreteTypeOptsIn() => Assert.True(Evaluate("""
        [DeepCopy]
        public abstract partial class Base
        {
            public int Id
            {
                get; set;
            }
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public string? Name
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                Base source = new Derived { Id = 7, Name = "name" };
                var copy = source.DeepCopy();

                return copy is Derived d && d.Id == 7 && d.Name == "name";
            }
        }
        """));

    [Fact]
    public void DeepCopy_PreservesComparers_WhenCopyingSetsAndMaps() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            public Dictionary<string, int> Map { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            public HashSet<string> Set { get; set; } = new(StringComparer.OrdinalIgnoreCase);

            public System.Collections.Concurrent.ConcurrentDictionary<string, int> Concurrent { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item();
                source.Map.Add("A", 1);
                source.Set.Add("B");
                source.Concurrent.TryAdd("C", 2);
                var copy = source.DeepCopy();

                return copy.Map.ContainsKey("a") && copy.Set.Contains("b") && copy.Concurrent.ContainsKey("c") && !ReferenceEquals(copy.Map, source.Map);
            }
        }
        """));

    [Fact]
    public void DeepCopy_PreservesOrderAndIndependence_WhenCopyingConcreteCollections() => Assert.True(Evaluate(Node + """
        [DeepCopy]
        public partial class Item
        {
            public Queue<Node> Queue { get; set; } = new();

            public Stack<Node> Stack { get; set; } = new();

            public System.Collections.ObjectModel.ObservableCollection<Node> Observable { get; set; } = new();

            public System.Collections.ObjectModel.ReadOnlyCollection<Node> ReadOnly { get; set; } = new(new List<Node>());

            public System.ComponentModel.BindingList<Node> Binding { get; set; } = new();

            public System.Collections.ObjectModel.Collection<Node> Collection { get; set; } = new();
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var a = new Node { Id = 1 };
                var b = new Node { Id = 2 };
                var source = new Item();
                source.Queue.Enqueue(a);
                source.Queue.Enqueue(b);
                source.Stack.Push(a);
                source.Stack.Push(b);
                source.Observable.Add(a);
                source.ReadOnly = new(new List<Node> { a });
                source.Binding.Add(a);
                source.Collection.Add(a);
                var copy = source.DeepCopy();
                a.Id = 9;

                return copy.Queue.Dequeue().Id == 1 && copy.Queue.Dequeue().Id == 2 && copy.Stack.Pop().Id == 2 && copy.Stack.Pop().Id == 1 &&
                    copy.Observable[0].Id == 1 && copy.ReadOnly[0].Id == 1 && copy.Binding[0].Id == 1 && copy.Collection[0].Id == 1;
            }
        }
        """));

    [Fact]
    public void DeepCopy_CopiesNestedCollectionsAndSharedContainers_WhenCollectionsRepeat() => Assert.True(Evaluate(Node + """
        [DeepCopy]
        public partial class Item
        {
            public List<List<Node>>? Rows
            {
                get; set;
            }

            public List<Node>? Other
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var node = new Node { Id = 1 };
                var list = new List<Node> { node };
                var source = new Item { Rows = new() { list, list }, Other = list };
                var copy = source.DeepCopy();
                node.Id = 9;

                return copy.Rows![0][0].Id == 1 && ReferenceEquals(copy.Rows[0], copy.Rows[1]) && ReferenceEquals(copy.Rows[0], copy.Other);
            }
        }
        """));

    [Fact]
    public void DeepCopy_HandlesNullableBuiltinsAndRepeatedTuples_WhenPropertiesAreNullable() => Assert.True(Evaluate(Node + """
        [DeepCopy]
        public partial class Item
        {
            public Uri? Link
            {
                get; set;
            }

            public Version? Version
            {
                get; set;
            }

            public Tuple<int, Node>? First
            {
                get; set;
            }

            public Tuple<int, Node>? Second
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var node = new Node { Id = 1 };
                var tuple = Tuple.Create(7, node);
                var source = new Item { Link = new Uri("https://example.com"), First = tuple, Second = tuple };
                var copy = source.DeepCopy();
                node.Id = 9;

                return copy.Link == source.Link && copy.Version == null && copy.First!.Item2.Id == 1 && ReferenceEquals(copy.First, copy.Second);
            }
        }
        """));

    [Fact]
    public void DeepCopy_HandlesNestedGenericTypes_WhenContainingTypesArePartial() => Assert.True(Evaluate("""
        public partial class Outer<T> where T : class
        {
            [DeepCopy]
            public partial class Inner<U> where U : struct
            {
                public U Value
                {
                    get; set;
                }
            }
        }

        [DeepCopy]
        public partial class Unrelated
        {
            public int Id
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Outer<string>.Inner<int> { Value = 7 };
                var copy = source.DeepCopy();

                return copy.Value == 7 && !ReferenceEquals(copy, source) && new Unrelated { Id = 9 }.DeepCopy().Id == 9;
            }
        }
        """));

    [Fact]
    public void DeepCopy_HandlesStaticAndKeywordMembers_WhenNamesNeedEscaping() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class @class
        {
            public static int Count
            {
                get; set;
            }

            public int @event
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                @class.Count = 3;

                return new @class { @event = 7 }.DeepCopy().@event == 7 && @class.Count == 3;
            }
        }
        """));

    [Fact]
    public void DeepCopy_HandlesInitOnlyPropertiesAndRequiredState_WhenModelIsRecord() => Assert.True(Evaluate(Node + """
        [DeepCopy]
        public partial record Item
        {
            public required Node Value
            {
                get; init;
            }

            public Node[]? Values
            {
                get; init;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var node = new Node { Id = 1 };
                var source = new Item { Value = node, Values = new[] { node } };
                var copy = source.DeepCopy();
                node.Id = 9;

                return copy.Value.Id == 1 && ReferenceEquals(copy.Value, copy.Values![0]) && !ReferenceEquals(copy.Values, source.Values);
            }
        }
        """));

    [Fact]
    public void ShallowCopy_SharesAllReferences_WhenPropertiesAreInitOnlyOrGetOnly() => Assert.True(Evaluate("""
        [ShallowCopy]
        public partial record Item
        {
            public int[]? Values
            {
                get; init;
            }

            public List<int> Items { get; } = new();
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Item { Values = new[] { 1 } };
                var copy = source.ShallowCopy();

                return !ReferenceEquals(source, copy) && ReferenceEquals(source.Values, copy.Values) && ReferenceEquals(source.Items, copy.Items);
            }
        }
        """));

    [Fact]
    public void CopyMethods_Compile_WhenTypeIsInternalOrStructWithAttributes() => Assert.True(Evaluate("""
        [DeepCopy]
        internal partial class Hidden
        {
            public int Id
            {
                get; set;
            }
        }

        [DeepCopy]
        public partial struct Money
        {
            public decimal Amount
            {
                get; set;
            }
        }

        [ShallowCopy] public partial record struct Coordinate(int X, int Y);

        public static class Check
        {
            public static bool Evaluate() => new Hidden { Id = 1 }.DeepCopy().Id == 1 && new Money { Amount = 2 }.DeepCopy().Amount == 2 && new Coordinate(3, 4).ShallowCopy().X == 3;
        }
        """));

    [Fact]
    public Task UV002_Reports_WhenArrayElementIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public class Node
        {
            public int Id
            {
                get; set;
            }
        }

        [DeepCopy]
        public partial class Holder
        {
            public Node[]? {|UV002:Values|}
            {
                get; set;
            }
        }
        """);

    [Fact]
    public Task UV002_Reports_WhenMutableStructIsNotOptedIn() => AnalyzerVerifier.VerifyAsync("""
        public struct Payload
        {
            public List<int>? Values
            {
                get; set;
            }
        }

        [DeepCopy]
        public partial class Holder
        {
            public Payload {|UV002:Data|}
            {
                get; set;
            }
        }
        """);

    [Fact]
    public Task UV004_Reports_WhenOuterTypeIsNotPartial() => AnalyzerVerifier.VerifyAsync("""
        public class Outer
        {
            [DeepCopy]
            public partial class {|UV004:Inner|}
            {
                public int Id
                {
                    get; set;
                }
            }
        }
        """);

    [Fact]
    public void DeepCopy_BypassesConstructor_WhenParameterCannotBeMatched() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            public Item(int unmatched)
            {
            }

            public int Id
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate() => new Item(1) { Id = 7 }.DeepCopy().Id == 7;
        }
        """));

    [Fact]
    public Task UV004_Reports_WhenArrayHasMultipleDimensions() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public partial class {|UV004:Item|}
        {
            public int[,]? Values
            {
                get; set;
            }
        }
        """);

    [Fact]
    public Task UV004_Reports_WhenBaseContainsInaccessibleState() => AnalyzerVerifier.VerifyAsync("""
        public class Base
        {
            public int Id { get; } = 7;
        }

        [DeepCopy]
        public partial class {|UV004:Item|} : Base
        {
            public int Value
            {
                get; set;
            }
        }
        """);

    [Fact]
    public Task UV004_Reports_WhenImmutableCollectionWouldNeedUnsupportedConstruction() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public partial class {|UV004:Item|}
        {
            public System.Collections.Immutable.ImmutableList<int>? Values
            {
                get; set;
            }
        }
        """);

    [Fact]
    public Task UV004_Reports_WhenPrimaryConstructorCapturesHiddenState() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public partial class {|UV004:Item|}(List<int> values)
        {
            public List<int> Values => values;
        }
        """);

    [Fact]
    public Task UV004_Reports_WhenRecordInheritsStoredState() => AnalyzerVerifier.VerifyAsync("""
        public record Base(int Id);

        [DeepCopy] public partial record {|UV004:Item|}(int Id) : Base(Id);
        """);

    [Fact]
    public void DeepCopy_PreservesArrayIdentityAcrossInterfaces_WhenArrayIsCovariant() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public int Id
            {
                get; set;
            }
        }

        [DeepCopy]
        public partial class Derived : Base
        {
        }

        [DeepCopy]
        public partial class Holder
        {
            public IEnumerable<Base>? Sequence
            {
                get; set;
            }

            public Base[]? Array
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                Base[] array = new Derived[] { new Derived { Id = 7 } };
                var copy = new Holder { Sequence = array, Array = array }.DeepCopy();

                return copy.Array is Derived[] && ReferenceEquals(copy.Sequence, copy.Array) && !ReferenceEquals(copy.Array, array) && copy.Array[0].Id == 7;
            }
        }
        """));

    [Fact]
    public void DeepCopy_PreservesNulls_WhenNestedCollectionsAndItemsAreNull() => Assert.True(Evaluate(Node + """
        [DeepCopy]
        public partial class Holder
        {
            public Node? Value
            {
                get; set;
            }

            public List<Node?>? Values
            {
                get; set;
            }

            public List<Node?>? Missing
            {
                get; set;
            }

            public Node?[]? Array
            {
                get; set;
            }

            public Dictionary<string, Node?>? Map
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var copy = new Holder { Values = new() { null }, Array = new Node?[] { null }, Map = new() { ["null"] = null } }.DeepCopy();

                return copy.Value == null && copy.Missing == null && copy.Values![0] == null && copy.Array![0] == null && copy.Map!["null"] == null;
            }
        }
        """));

    [Fact]
    public void DeepCopy_ThrowsInsteadOfSlicing_WhenDerivedRuntimeTypeHasNoOptIn() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public int Id
            {
                get; set;
            }
        }

        public class Derived : Base
        {
            public int Extra
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                Base source = new Derived();

                try
                {
                    source.DeepCopy();

                    return false;
                }
                catch (NotSupportedException) { return true; }
            }
        }
        """));

    [Fact]
    public void DeepCopy_HandlesPrimaryConstructor_WhenStateIsExplicitlyStored() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Item(int id)
        {
            public int Id { get; } = id;
        }

        public static class Check
        {
            public static bool Evaluate() => new Item(7).DeepCopy().Id == 7;
        }
        """));

    [Fact]
    public void DeepCopy_PreservesRequiredBaseState_WhenDerivedTypeIsConstructed() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public required int Id
            {
                get; init;
            }
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public string? Name
            {
                get; init;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var copy = new Derived { Id = 7, Name = "name" }.DeepCopy();

                return copy.Id == 7 && copy.Name == "name";
            }
        }
        """));

    [Fact]
    public void DeepCopy_RejectsImmutableContainerCycles_WhenTuplePointsBackToItself() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Node
        {
            public Tuple<Node>? Tuple
            {
                get; set;
            }
        }

        [DeepCopy]
        public partial class Holder
        {
            public Tuple<Node>? Tuple
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var node = new Node();
                var tuple = Tuple.Create(node);
                node.Tuple = tuple;

                try
                {
                    new Holder { Tuple = tuple }.DeepCopy();

                    return false;
                }
                catch (NotSupportedException) { return true; }
            }
        }
        """));

    [Fact]
    public void Generator_UsesDistinctHints_WhenQualifiedNamesWouldPreviouslyCollide()
    {
        var run = GeneratorHarness.Run("""
            namespace A_B
            {
                [DeepCopy]
                public partial class C
                {
                    public int Id
                    {
                        get; set;
                    }
                }
            }

            namespace A
            {
                [DeepCopy]
                public partial class B_C
                {
                    public int Id
                    {
                        get; set;
                    }
                }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Equal(2, run.HintNames.Distinct().Count());
    }

    [Fact]
    public void Generator_EmitsOneCopy_WhenTypeHasMultiplePartialDeclarations()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Item
            {
                public int Id
                {
                    get; set;
                }
            }

            public partial class Item
            {
                public string? Name
                {
                    get; set;
                }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Single(run.GeneratedSources);
    }

    [Fact]
    public void DeepCopy_BypassesConstructor_WhenStateNameMatchesButTypeDoesNot() => Assert.True(Evaluate("""
        [DeepCopy]
        public partial class Item
        {
            public Item(string id)
            {
                Id = int.Parse(id);
            }

            public int Id
            {
                get; set;
            }
        }

        public static class Check
        {
            public static bool Evaluate() => new Item("7").DeepCopy().Id == 7;
        }
        """));

    [Fact]
    public Task UV004_Reports_WhenHiddenInheritedFieldWouldOtherwiseBeLost() => AnalyzerVerifier.VerifyAsync("""
        public class Base
        {

            private int value = 7; public int Original => value;
        }

        [DeepCopy]
        public partial class {|UV004:Item|} : Base
        {

            private int value = 9; public int Current => value;
        }
        """);
}
