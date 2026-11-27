// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class FrameworkCompatibilityTests
{
    [Theory]
    [InlineData("netstandard2.0")]
    [InlineData("net6.0")]
    public async Task UV004_ReportsStoredOverrides_WhenConsumerTargetsOlderFrameworks(string framework)
    {
        var referenceAssemblies = framework == "netstandard2.0"
            ? ReferenceAssemblies.NetStandard.NetStandard20
            : ReferenceAssemblies.Net.Net60;
        var frameworkReferences = await referenceAssemblies.ResolveAsync(
            LanguageNames.CSharp,
            TestContext.Current.CancellationToken);

        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Base
            {
                public virtual List<int> Values { get; set; } = new();
            }

            [DeepCopy]
            public partial class Derived : Base
            {
                public override List<int> Values { get; set; } = new();
            }
            """, [.. frameworkReferences, MetadataReference.CreateFromFile(typeof(DeepCopy).Assembly.Location)]);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Equal("Base_DeepCopy.g.cs", Assert.Single(run.HintNames));
        Assert.Contains(run.AnalyzerErrors, diagnostic =>
            diagnostic.Id == "UV004" && diagnostic.GetMessage().Contains("backing storage"));

        using var output = new MemoryStream();
        var emit = run.OutputCompilation.Emit(output, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Theory]
    [InlineData("netstandard2.0")]
    [InlineData("net6.0")]
    public async Task CopyMethods_Compile_WhenConsumerHasNoFrozenOrImmutableCollections(string framework)
    {
        var referenceAssemblies = framework == "netstandard2.0"
            ? ReferenceAssemblies.NetStandard.NetStandard20
            : ReferenceAssemblies.Net.Net60;
        var frameworkReferences = await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Deep
            {
                public int Id { get; set; }
                public int[]? Numbers { get; set; }
                public List<int>? Items { get; set; }
                public HashSet<int>? Tags { get; set; }
                public Dictionary<string, int>? Counts { get; set; }
            }

            [ShallowCopy]
            public partial class Shallow
            {
                public int Id { get; set; }
                public int[]? Numbers { get; set; }
            }

            public static class Consumer
            {
                public static Deep Clone(Deep value) => value.DeepCopy();
                public static Shallow Clone(Shallow value) => value.ShallowCopy();
            }
            """, [.. frameworkReferences, MetadataReference.CreateFromFile(typeof(DeepCopy).Assembly.Location)]);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Equal(2, run.GeneratedSources.Length);
        Assert.DoesNotContain("using System.Collections.Frozen;", run.GeneratedText);
        Assert.DoesNotContain("using System.Collections.Immutable;", run.GeneratedText);
        using var output = new MemoryStream();
        var emit = run.OutputCompilation.Emit(output, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }
}
