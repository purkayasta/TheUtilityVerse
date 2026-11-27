// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Collections.ObjectModel;
using System.ComponentModel;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class CollectionWrapperCopyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void DeepCopy_RejectsListAndViewSharingBackingStore(bool listFirst, bool populated)
    {
        const string listProperty = "public List<Derived> List { get; set; } = new();";
        const string viewProperty = "public IReadOnlyList<Base> View { get; set; } = default!;";

        var run = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial class Base
            {
            }

            [DeepCopy]
            public partial class Derived : Base
            {
            }

            [DeepCopy]
            public partial class Graph
            {
                {{(listFirst ? listProperty : viewProperty)}}

                {{(listFirst ? viewProperty : listProperty)}}
            }

            public static class Check
            {
                public static void Copy(bool populated)
                {
                    var source = new Graph();

                    if (populated)
                    {
                        source.List.Add(new Derived());
                    }

                    source.View = source.List.AsReadOnly();
                    source.DeepCopy();
                }
            }
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);
        var method = assembly.GetType("Check").GetMethod("Copy")!;
        var failure = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(null, [populated]));

        Assert.Contains("backing", Assert.IsType<NotSupportedException>(failure.InnerException).Message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CopySequence_RejectsArrayAndWrapperSharingBackingStore(bool arrayFirst)
    {
        var source = new[] { 42 };
        var view = Array.AsReadOnly(source);
        var context = new CopyContext();

        var failure = Assert.Throws<NotSupportedException>(() =>
        {
            if (arrayFirst)
            {
                context.CopyArray(source, static (value, _) => value);
            }

            context.CopySequence(view, static (value, _) => value);

            if (!arrayFirst)
            {
                context.CopyArray(source, static (value, _) => value);
            }
        });

        Assert.Contains("backing", failure.Message);

        var retry = Assert.IsType<ReadOnlyCollection<int>>(context.CopySequence(view, static (value, _) => value));
        Assert.Equal(42, Assert.Single(retry));
    }

    [Fact]
    public void CopySequence_RejectsDistinctWrappersSharingBackingStore()
    {
        var backing = new List<int> { 42 };
        var context = new CopyContext();
        context.CopySequence(new BindingList<int>(backing), static (value, _) => value);

        var failure = Assert.Throws<NotSupportedException>(() =>
            context.CopySequence(new Collection<int>(backing), static (value, _) => value));

        Assert.Contains("backing", failure.Message);
    }

    [Fact]
    public void CopySequence_RejectsBackingStoreWithOpaqueIdentity()
    {
        IList<int> backing = new GenericOnlyList();
        var wrapper = new ReadOnlyCollection<int>(backing);

        var failure = Assert.Throws<NotSupportedException>(() =>
            new CopyContext().CopySequence(wrapper, static (value, _) => value));

        Assert.Contains("backing", failure.Message);
    }

    [Fact]
    public void CopySequence_RejectsFixedSizeBackingForMutableWrapper()
    {
        var wrapper = new Collection<int>(new[] { 42 });

        var failure = Assert.Throws<NotSupportedException>(() =>
            new CopyContext().CopySequence(wrapper, static (value, _) => value));

        Assert.Contains("backing", failure.Message);
    }

    [Fact]
    public void CopySequence_PreservesCopiesOfIndependentCollectionsAndRepeatedWrapperViews()
    {
        var values = new List<string> { "same contents" };
        var view = new List<string>(values).AsReadOnly();
        var context = new CopyContext();

        var listCopy = Assert.IsType<List<string>>(context.CopySequence(values, static (value, _) => value));
        var viewCopy = Assert.IsType<ReadOnlyCollection<string>>(context.CopySequence(view, static (value, _) => value));

        Assert.Same(viewCopy, context.CopySequence<object>(view, static (value, _) => value));
        listCopy.Add("copy only");
        Assert.Single(viewCopy);
        Assert.Single(values);
    }

    private sealed class GenericOnlyList : IList<int>
    {
        private readonly List<int> items = [42];

        public int this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public void Add(int item) => items.Add(item);

        public void Clear() => items.Clear();

        public bool Contains(int item) => items.Contains(item);

        public void CopyTo(int[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

        public IEnumerator<int> GetEnumerator() => items.GetEnumerator();

        public int IndexOf(int item) => items.IndexOf(item);

        public void Insert(int index, int item) => items.Insert(index, item);

        public bool Remove(int item) => items.Remove(item);

        public void RemoveAt(int index) => items.RemoveAt(index);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
