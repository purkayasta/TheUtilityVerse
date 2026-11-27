// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class ExplicitInterfaceCopyTests
{
    [Theory]
    [InlineData("class", "get; set;")]
    [InlineData("struct", "get; init;")]
    public void DeepCopy_ReportsUnsupportedExplicitInterfaceAutoProperty(string typeKind, string accessors)
    {
        var run = GeneratorHarness.Run($$"""
            public interface IState
            {
                List<int>? Values { {{accessors}} }
            }

            [DeepCopy]
            public partial {{typeKind}} Model : IState
            {
                List<int>? IState.Values { {{accessors}} }
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.GeneratedSources);

        var diagnostic = Assert.Single(run.AnalyzerErrors);
        Assert.Equal("UV004", diagnostic.Id);
        Assert.Contains("explicit interface auto-property", diagnostic.GetMessage());
        Assert.Contains("IState.Values", diagnostic.GetMessage());
    }

    [Fact]
    public void DeepCopy_CopiesBackingFieldForComputedExplicitInterfaceProperty()
    {
        var run = GeneratorHarness.Run("""
            public interface IState
            {
                List<int> Values { get; }
            }

            [DeepCopy]
            public partial class Model : IState
            {
                private readonly List<int> values = new() { 42 };

                List<int> IState.Values => values;
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Model();
                    var copy = source.DeepCopy();
                    var sourceValues = ((IState)source).Values;
                    var copyValues = ((IState)copy).Values;
                    copyValues.Add(43);

                    return sourceValues.SequenceEqual(new[] { 42 }) &&
                        copyValues.SequenceEqual(new[] { 42, 43 });
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
    public void ShallowCopy_SharesExplicitInterfaceAutoPropertyValue()
    {
        var run = GeneratorHarness.Run("""
            public interface IState
            {
                List<int> Values { get; }
            }

            [ShallowCopy]
            public partial class Model : IState
            {
                List<int> IState.Values { get; } = new();
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Model();
                    var copy = source.ShallowCopy();

                    return !ReferenceEquals(source, copy) &&
                        ReferenceEquals(((IState)source).Values, ((IState)copy).Values);
                }
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }
}
