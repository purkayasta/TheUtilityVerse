// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class ShapedArrayCopyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeepCopy_PreservesShapedArrayAliasesBoundsAndCycles(bool runtimeViewFirst)
    {
        const string wide = "public Array? Wide { get; set; }";
        const string typed = "public Node?[,]? Typed { get; set; }";

        var run = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial class Node
            {
                public Grid? Owner { get; set; }
            }

            [DeepCopy]
            public partial class Grid
            {
                {{(runtimeViewFirst ? wide : typed)}}

                {{(runtimeViewFirst ? typed : wide)}}

                public int[,,]? Empty { get; set; }

                public int[,]? Missing { get; set; }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Grid();
                    var node = new Node { Owner = source };
                    var array = (Node?[,])Array.CreateInstance(typeof(Node), new[] { 2, 2 }, new[] { -2, 4 });
                    array[-2, 4] = node;
                    array[-1, 5] = node;
                    source.Typed = array;
                    source.Wide = array;
                    source.Empty = new int[2, 0, 3];

                    var copy = source.DeepCopy();
                    var copiedArray = copy.Typed!;

                    return !ReferenceEquals(array, copiedArray) &&
                        ReferenceEquals(copy.Wide, copiedArray) &&
                        copiedArray.GetLowerBound(0) == -2 &&
                        copiedArray.GetLowerBound(1) == 4 &&
                        copiedArray.GetUpperBound(0) == -1 &&
                        copiedArray.GetUpperBound(1) == 5 &&
                        !ReferenceEquals(copiedArray[-2, 4], node) &&
                        ReferenceEquals(copiedArray[-2, 4], copiedArray[-1, 5]) &&
                        ReferenceEquals(copiedArray[-2, 4]!.Owner, copy) &&
                        copiedArray[-2, 5] == null &&
                        !ReferenceEquals(copy.Empty, source.Empty) &&
                        copy.Empty!.GetLength(1) == 0 &&
                        copy.Empty.GetLength(2) == 3 &&
                        copy.Missing == null;
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
    public void Analyzer_RequiresOptInForMutableShapedArrayElements()
    {
        var run = GeneratorHarness.Run("""
            public class Element
            {
            }

            [DeepCopy]
            public partial class Model
            {
                public Element[,]? Values { get; set; }
            }
            """);

        Assert.Empty(run.CompilationErrors);

        var diagnostic = Assert.Single(run.AnalyzerErrors);
        Assert.Equal("UV002", diagnostic.Id);
        Assert.Contains("Element", diagnostic.GetMessage());
    }

    [Fact]
    public void Copy_PreservesNonVectorRankOneArrayAndNegativeBound()
    {
        var source = Array.CreateInstance(typeof(int), [2], [-3]);
        source.SetValue(12, -3);
        source.SetValue(34, -2);

        var copy = new CopyContext().Copy(source);

        Assert.NotSame(source, copy);
        Assert.Equal(source.GetType(), copy.GetType());
        Assert.Equal(-3, copy.GetLowerBound(0));
        Assert.Equal(-2, copy.GetUpperBound(0));
        Assert.Equal(12, copy.GetValue(-3));
        Assert.Equal(34, copy.GetValue(-2));
    }

    [Fact]
    public void Copy_RuntimeShapedArrayCanReferToItself()
    {
        var source = new object?[1, 2];
        source[0, 1] = source;

        var copy = Assert.IsType<object[,]>(new CopyContext().Copy<object>(source));

        Assert.NotSame(source, copy);
        Assert.Null(copy[0, 0]);
        Assert.Same(copy, copy[0, 1]);
    }

    [Fact]
    public void CopyArray_PreservesExistingVectorPath()
    {
        var source = new[] { 1, 2 };
        var context = new CopyContext();
        var copy = context.CopyArray(source, static (item, _) => item + 2);

        Assert.NotSame(source, copy);
        Assert.Equal(new[] { 3, 4 }, copy);
        Assert.Same(copy, context.CopyArray(source, static (item, _) => item));
    }
}
