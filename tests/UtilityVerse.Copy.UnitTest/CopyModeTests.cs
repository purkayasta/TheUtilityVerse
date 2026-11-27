// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class CopyModeTests
{
    [Fact]
    public void CopyMethods_GenerateBothModesAcrossPartialDeclarationsAndInheritedMarkers()
    {
        var run = GeneratorHarness.Run("""
            public interface ICopyBoth : IDeepCopy, IShallowCopy
            {
            }

            public partial class Base : ICopyBoth
            {
                public List<int> Items { get; } = new() { 42 };
            }

            [DeepCopy]
            public partial class Derived : Base
            {
            }

            [ShallowCopy]
            public partial class Derived
            {
            }

            [DeepCopy, ShallowCopy]
            public partial struct Point
            {
                public int X { get; set; }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Derived();
                    var deep = source.DeepCopy();
                    var shallow = source.ShallowCopy();
                    var point = new Point { X = 42 };

                    return deep.GetType() == typeof(Derived) &&
                        shallow.GetType() == typeof(Derived) &&
                        !ReferenceEquals(source, deep) &&
                        !ReferenceEquals(source, shallow) &&
                        !ReferenceEquals(source.Items, deep.Items) &&
                        ReferenceEquals(source.Items, shallow.Items) &&
                        deep.Items.SequenceEqual(source.Items) &&
                        point.DeepCopy().X == 42 &&
                        point.ShallowCopy().X == 42;
                }
            }
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Equal(6, run.GeneratedSources.Length);
        Assert.DoesNotContain(run.OutputCompilation.GetDiagnostics(TestContext.Current.CancellationToken), diagnostic =>
            diagnostic.Id is "CS0108" or "CS0109");

        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }

    [Theory]
    [InlineData("DeepCopy", "ShallowCopy")]
    [InlineData("ShallowCopy", "DeepCopy")]
    public void CopyMethods_GenerateValidModeWhenOtherModeHasCollision(string collision, string valid)
    {
        var run = GeneratorHarness.Run($$"""
            [DeepCopy, ShallowCopy]
            public partial class Model
            {
                public int {{collision}} { get; set; }
            }
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Equal($"Model_{valid}.g.cs", Assert.Single(run.HintNames));

        var diagnostic = Assert.Single(run.AnalyzerErrors);
        Assert.Equal("UV004", diagnostic.Id);
        Assert.Contains(collision, diagnostic.GetMessage());
    }
}
