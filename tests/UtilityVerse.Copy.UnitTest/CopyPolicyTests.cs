// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class CopyPolicyTests
{
    private const string Models = """
        using System.IO;

        [DeepCopy]
        public partial class Model
        {
            [CopyPolicy(CopyBehavior.Share)]
            public Stream? Stream { get; set; }

            [field: CopyPolicy(CopyBehavior.Reset)]
            public Action? Callback { get; set; }

            [CopyPolicy(CopyBehavior.Custom)]
            public Resource? First { get; set; }

            [CopyPolicy(CopyBehavior.Custom)]
            public Resource? Second { get; set; }

            [CopyPolicy(CopyBehavior.Copy)]
            public List<int> Values { get; set; } = new();
        }

        public class Resource
        {
            public Model? Owner { get; set; }
        }

        public sealed class SpecialResource : Resource
        {
        }

        public static class Check
        {
            public static bool Verify()
            {
                Register();

                using var stream = new MemoryStream();
                var source = new Model { Stream = stream, Callback = static () => { } };
                var resource = new Resource { Owner = source };
                source.First = resource;
                source.Second = resource;
                source.Values.Add(42);
                var copy = source.DeepCopy();
                var direct = new CopyContext().Copy(resource);

                return ReferenceEquals(copy.Stream, stream) &&
                    copy.Callback == null &&
                    !ReferenceEquals(copy.Values, source.Values) &&
                    copy.Values.SequenceEqual(source.Values) &&
                    !ReferenceEquals(copy.First, resource) &&
                    ReferenceEquals(copy.First, copy.Second) &&
                    ReferenceEquals(copy, copy.First!.Owner) &&
                    !ReferenceEquals(direct, resource) &&
                    ReferenceEquals(direct, direct.Owner!.First);
            }

            public static void CopyUnregisteredSubtype()
            {
                Register();
                new Model { First = new SpecialResource() }.DeepCopy();
            }

            private static void Register() => CopyContext.RegisterExternalCopier<Resource>(static (source, context) =>
            {
                var copy = new Resource();
                context.Register(source, copy);
                copy.Owner = context.Copy(source.Owner);

                return copy;
            });
        }
        """;

    [Fact]
    public void DeepCopy_RespectsExplicitMemberPoliciesAndCustomGraphIdentity()
    {
        using var assembly = LoadModels();

        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }

    [Fact]
    public void DeepCopy_RequiresExternalCopierForActualRuntimeSubtype()
    {
        using var assembly = LoadModels();
        var method = assembly.GetType("Check").GetMethod("CopyUnregisteredSubtype")!;

        var invocation = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(null, null));
        Assert.Contains("SpecialResource", Assert.IsType<NotSupportedException>(invocation.InnerException).Message);
    }

    [Fact]
    public void Analyzer_RequiresExplicitPolicyOrOptInForExternalState()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Model
            {
                public System.IO.Stream? Stream { get; set; }
            }
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Equal("UV002", Assert.Single(run.AnalyzerErrors).Id);
    }

    private static LoadedAssembly LoadModels()
    {
        var run = GeneratorHarness.Run(Models);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        return GeneratorHarness.Load(run);
    }
}
