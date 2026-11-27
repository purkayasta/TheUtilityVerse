// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class CovariantCollectionTests
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
    [InlineData(false, "IEnumerable<Base>", "List<Derived>")]
    [InlineData(true, "IEnumerable<Base>", "List<Derived>")]
    [InlineData(false, "IReadOnlyList<Base>", "List<Derived>")]
    [InlineData(true, "IReadOnlyList<Base>", "List<Derived>")]
    [InlineData(false, "IReadOnlyCollection<Base>", "HashSet<Derived>")]
    [InlineData(true, "IReadOnlyCollection<Base>", "HashSet<Derived>")]
    [InlineData(false, "IEnumerable<Base>", "Queue<Derived>")]
    [InlineData(true, "IEnumerable<Base>", "Queue<Derived>")]
    [InlineData(false, "IEnumerable<Base>", "Stack<Derived>")]
    [InlineData(true, "IEnumerable<Base>", "Stack<Derived>")]
    [InlineData(false, "IEnumerable<Base>", "System.Collections.ObjectModel.Collection<Derived>")]
    [InlineData(true, "IEnumerable<Base>", "System.Collections.ObjectModel.Collection<Derived>")]
    [InlineData(false, "IReadOnlyCollection<Base>", "System.Collections.ObjectModel.ObservableCollection<Derived>")]
    [InlineData(true, "IReadOnlyCollection<Base>", "System.Collections.ObjectModel.ObservableCollection<Derived>")]
    [InlineData(false, "IEnumerable<Base>", "System.ComponentModel.BindingList<Derived>")]
    [InlineData(true, "IEnumerable<Base>", "System.ComponentModel.BindingList<Derived>")]
    [InlineData(false, "IReadOnlyList<Base>", "System.Collections.ObjectModel.ReadOnlyCollection<Derived>")]
    [InlineData(true, "IReadOnlyList<Base>", "System.Collections.ObjectModel.ReadOnlyCollection<Derived>")]
    public void DeepCopy_PreservesRuntimeContainerAndAliases_RegardlessOfTraversalOrder(
        bool narrowFirst,
        string view,
        string collection)
    {
        var wide = $"public {view}? Wide {{ get; set; }}";
        var narrow = $"public {collection}? Narrow {{ get; set; }}";
        var initialize = collection switch
        {
            "System.Collections.ObjectModel.Collection<Derived>" or
            "System.Collections.ObjectModel.ReadOnlyCollection<Derived>" or
            "System.ComponentModel.BindingList<Derived>" => $"new {collection}(items.ToList())",
            _ => $"new {collection}(items)"
        };

        Evaluate($$"""
            [DeepCopy]
            public partial class Base
            {
                public int Id { get; set; }

                public Holder? Owner { get; set; }
            }

            [DeepCopy]
            public partial class Derived : Base
            {
                public string? Extra { get; set; }
            }

            [DeepCopy]
            public partial class Holder
            {
                {{(narrowFirst ? narrow : wide)}}

                {{(narrowFirst ? wide : narrow)}}
            }

            public static class Check
            {
                public static bool Evaluate()
                {
                    var source = new Holder();
                    var items = new[]
                    {
                        new Derived { Id = 1, Extra = "first", Owner = source },
                        new Derived { Id = 2, Extra = "second", Owner = source }
                    };
                    var collection = {{initialize}};
                    source.Wide = collection;
                    source.Narrow = collection;
                    var expected = collection.Select(item => item.Id).ToArray();

                    var copy = source.DeepCopy();

                    return copy.Wide!.GetType() == collection.GetType() &&
                        ReferenceEquals(copy.Wide, copy.Narrow) &&
                        !ReferenceEquals(copy.Wide, collection) &&
                        copy.Wide.Select(item => item.Id).SequenceEqual(expected) &&
                        copy.Wide.All(item => item is Derived derived &&
                            derived.Extra != null && ReferenceEquals(item.Owner, copy)) &&
                        !ReferenceEquals(copy.Wide.First(), collection.First());
                }
            }
            """);
    }

    [Fact]
    public void DeepCopy_PreservesEmptyAndNullOnlyCovariantLists() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
        }

        [DeepCopy]
        public partial class Derived : Base
        {
        }

        [DeepCopy]
        public partial class Holder
        {
            public IEnumerable<Base?>? Empty { get; set; }

            public IEnumerable<Base?>? Nulls { get; set; }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Holder
                {
                    Empty = new List<Derived?>(),
                    Nulls = new List<Derived?> { null }
                };
                var copy = source.DeepCopy();

                return copy.Empty is List<Derived?> empty && empty.Count == 0 &&
                    copy.Nulls is List<Derived?> nulls && nulls.Count == 1 && nulls[0] == null;
            }
        }
        """);
    [Fact]
    public void DeepCopy_PreservesNestedCovariantAliases() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public int Id { get; set; }
        }

        [DeepCopy]
        public partial class Derived : Base
        {
        }

        [DeepCopy]
        public partial class Holder
        {
            public IEnumerable<IEnumerable<Base>>? Wide { get; set; }

            public List<List<Derived>>? Narrow { get; set; }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var inner = new List<Derived> { new Derived { Id = 7 } };
                var outer = new List<List<Derived>> { inner, inner };
                var copy = new Holder { Wide = outer, Narrow = outer }.DeepCopy();

                return ReferenceEquals(copy.Wide, copy.Narrow) &&
                    ReferenceEquals(copy.Narrow![0], copy.Narrow[1]) &&
                    !ReferenceEquals(copy.Narrow[0], inner) &&
                    copy.Narrow[0][0].Id == 7;
            }
        }
        """);

    [Fact]
    public void DeepCopy_RegistersClosedGenericElements_BeforeBaseHydration() => Evaluate("""
        [DeepCopy]
        public partial class Item
        {
        }

        [DeepCopy]
        public partial class Item<T> : Item
        {
            public T? Value { get; set; }
        }

        [DeepCopy]
        public partial class HolderBase
        {
            public IEnumerable<Item>? Wide { get; set; }
        }

        [DeepCopy]
        public partial class Holder<T> : HolderBase
        {
            public List<Item<T>>? Narrow { get; set; }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var items = new List<Item<int>> { new Item<int> { Value = 7 } };
                var copy = new Holder<int> { Wide = items, Narrow = items }.DeepCopy();

                return ReferenceEquals(copy.Wide, copy.Narrow) &&
                    !ReferenceEquals(copy.Narrow, items) && copy.Narrow![0].Value == 7;
            }
        }
        """);

    [Fact]
    public void DeepCopy_RegistersVisibleModels_ForGenericElementViews() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
        }

        [DeepCopy]
        public partial class Derived : Base
        {
        }

        [DeepCopy]
        public partial class Holder<T> where T : Base
        {
            public IEnumerable<T>? Values { get; set; }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var values = new List<Derived>();
                var copy = new Holder<Base> { Values = values }.DeepCopy();

                return copy.Values is List<Derived> && !ReferenceEquals(copy.Values, values);
            }
        }
        """);

    [Fact]
    public void CopySequence_PreservesComparer_AndHonorsElementDelegate()
    {
        CopyContext.RegisterSequenceType<DirectDerived>();

        var comparer = EqualityComparer<DirectDerived>.Default;
        var source = new HashSet<DirectDerived>(comparer) { new DirectDerived { Id = 7 } };
        var calls = 0;
        var context = new CopyContext();
        var copy = context.CopySequence<DirectBase>(source, (value, _) =>
        {
            calls++;

            return new DirectDerived { Id = value.Id + 1 };
        });
        var set = Assert.IsType<HashSet<DirectDerived>>(copy);

        Assert.Same(comparer, set.Comparer);
        Assert.Equal(8, Assert.Single(set).Id);
        Assert.Equal(1, calls);
        Assert.Same(copy, context.CopySequence(source, static (value, _) => value));
    }

    [Fact]
    public void CopySequence_RejectsIncompatibleDelegateResult_AndRecovers()
    {
        CopyContext.RegisterSequenceType<InvalidResultDerived>();

        var source = new List<InvalidResultDerived> { new InvalidResultDerived() };
        var context = new CopyContext();

        Assert.Throws<InvalidCastException>(() =>
            context.CopySequence<DirectBase>(source, static (_, _) => new DirectBase()));

        var copy = context.CopySequence<DirectBase>(source, static (_, _) => new InvalidResultDerived());

        Assert.IsType<List<InvalidResultDerived>>(copy);
        Assert.NotSame(source[0], Assert.Single(copy!));
    }

    [Fact]
    public void CopySequence_RequiresRegistration_ForUnknownRuntimeElementType()
    {
        var source = new List<UnregisteredDerived>();
        var context = new CopyContext();
        var error = Assert.Throws<NotSupportedException>(() =>
            context.CopySequence<DirectBase>(source, static (value, _) => value));

        Assert.Contains("RegisterSequenceType", error.Message);

        CopyContext.RegisterSequenceType<UnregisteredDerived>();

        var copy = context.CopySequence<DirectBase>(source, static (value, _) => value);

        Assert.IsType<List<UnregisteredDerived>>(copy);
        Assert.NotSame(source, copy);
    }

    [Fact]
    public void CopySequence_PreservesBuiltInScalarRuntimeType()
    {
        var source = new List<string> { "first" };
        var copy = new CopyContext().CopySequence<object>(source, static (value, _) => value);

        Assert.Equal("first", Assert.Single(Assert.IsType<List<string>>(copy)));
    }

    [Fact]
    public void CopySequence_RejectsCollectionSubclass_WithoutDroppingState()
    {
        var source = new CustomList();

        Assert.Throws<NotSupportedException>(() =>
            new CopyContext().CopySequence<DirectBase>(source, static (value, _) => value));
    }

    [Theory]
    [InlineData("__UtilityVerseSequenceTypes")]
    [InlineData("__UtilityVerseRegisterSequenceTypes")]
    public void DeepCopy_DiagnosesRegistrationMemberCollision(string memberName)
    {
        var run = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial class Model
            {
                public int {{memberName}};
            }
            """);

        Assert.Contains(run.AnalyzerErrors, diagnostic => diagnostic.Id == "UV004");
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.GeneratedSources);
    }

    private class DirectBase
    {
        public int Id { get; set; }
    }

    private sealed class DirectDerived : DirectBase
    {
    }

    private sealed class InvalidResultDerived : DirectBase
    {
    }

    private sealed class UnregisteredDerived : DirectBase
    {
    }

    private sealed class CustomList : List<DirectDerived>
    {
        public int AdditionalState { get; set; }
    }
}
