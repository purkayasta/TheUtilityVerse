// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class CopyMemberCollisionTests
{
    [Theory]
    [InlineData("ShallowCopy", "public int ShallowCopy;", "ShallowCopy")]
    [InlineData("DeepCopy", "public static int DeepCopy => 1;", "DeepCopy")]
    [InlineData("DeepCopy", "public event Action? DeepCopy;", "DeepCopy")]
    [InlineData("ShallowCopy", "public class ShallowCopy<T> { }", "ShallowCopy")]
    [InlineData("DeepCopy", "public static int DeepCopy() => 1;", "DeepCopy")]
    [InlineData("DeepCopy", "public int __UtilityVerseDeepCopy;", "__UtilityVerseDeepCopy")]
    [InlineData("DeepCopy", "private int __UtilityVerseDeepCopy(CopyContext _) => 1;", "__UtilityVerseDeepCopy")]
    public void UV004_ReportsMembersThatConflictWithGeneratedMethods(string attribute, string member, string name)
    {
        AssertCollision($$"""
            [{{attribute}}]
            public partial class Model
            {
                {{member}}
            }
            """, name);
    }

    [Theory]
    [InlineData("DeepCopy", "DeepCopy")]
    [InlineData("ShallowCopy", "Model<ShallowCopy>")]
    public void UV004_ReportsConflictingTypeAndTypeParameterNames(string attribute, string model)
    {
        AssertCollision($$"""
            [global::UtilityVerse.Copy.{{attribute}}]
            public partial class {{model}}
            {
            }
            """, attribute);
    }

    [Theory]
    [InlineData("class", "IDeepCopyable", "object IDeepCopyable.Copy(CopyContext context) => this;", "IDeepCopyable.Copy")]
    [InlineData("struct", "IBoxedDeepCopyable",
        """
            public object Copy(CopyContext context) => this;

                object IBoxedDeepCopyable.CopyBoxed(object source, CopyContext context) => this;
            """,
        "IBoxedDeepCopyable.CopyBoxed")]
    public void UV004_ReportsDuplicateExplicitCopyImplementations(
        string typeKind,
        string copyInterface,
        string implementation,
        string name)
    {
        AssertCollision($$"""
            [DeepCopy]
            public partial {{typeKind}} Model : {{copyInterface}}
            {
                {{implementation}}
            }
            """, name);
    }

    [Fact]
    public void UV004_ReportsExactGraphConstructorSignature()
    {
        AssertCollision("""
            [DeepCopy]
            public partial class Model
            {
                public Model(Model source, CopyContext context)
                {
                }
            }
            """, "copy constructor");
    }

    [Fact]
    public void DeepCopy_AllowsParameterlessGenericOverload()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Model
            {
                public int DeepCopy<T>() => 42;
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Model();

                    return !ReferenceEquals(source, source.DeepCopy()) && source.DeepCopy<int>() == 42;
                }
            }
            """);

        VerifyGeneratedCopy(run, 1);
    }

    [Fact]
    public void CopyMethods_AllowDistinctOverloadsAndUnrelatedExplicitImplementations()
    {
        var run = GeneratorHarness.Run("""
            public interface IUserCopy
            {
                int DeepCopy();
            }

            [DeepCopy]
            public partial class Deep : IUserCopy
            {
                public int Value { get; set; } = 42;

                public int DeepCopy<T>() => 1;

                public int DeepCopy(int count = 0) => count;

                int IUserCopy.DeepCopy() => 2;

                private int __UtilityVerseDeepCopy<T>(T context) => 3;

                private int __UtilityVerseDeepCopy(ref CopyContext context) => 4;
            }

            [ShallowCopy]
            public partial class Shallow
            {
                public int __UtilityVerseDeepCopy = 5;

                public int ShallowCopy<T>() => 6;

                public int ShallowCopy(int count) => count;
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Deep();
                    var deep = source.DeepCopy();
                    var shallow = new Shallow();

                    return !ReferenceEquals(source, deep) &&
                        deep.Value == 42 &&
                        source.DeepCopy<int>() == 1 &&
                        source.DeepCopy(8) == 8 &&
                        ((IUserCopy)source).DeepCopy() == 2 &&
                        !ReferenceEquals(shallow, shallow.ShallowCopy()) &&
                        shallow.ShallowCopy().__UtilityVerseDeepCopy == 5 &&
                        shallow.ShallowCopy<int>() == 6 &&
                        shallow.ShallowCopy(7) == 7;
                }
            }
            """);

        VerifyGeneratedCopy(run, 2);
    }

    [Fact]
    public void DeepCopy_AllowsGraphConstructorOverloadsWithByRefParameters()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Model
            {
                public int Value { get; set; }

                public Model()
                {
                }

                public Model(ref Model source, CopyContext context)
                {
                    Value = -1;
                }

                public Model(Model source, in CopyContext context)
                {
                    Value = -2;
                }
            }

            public static class Check
            {
                public static bool Verify()
                {
                    var source = new Model { Value = 42 };
                    var copy = source.DeepCopy();

                    return !ReferenceEquals(source, copy) && copy.Value == 42;
                }
            }
            """);

        VerifyGeneratedCopy(run, 1);
    }

    private static void AssertCollision(string source, string memberName)
    {
        var run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.GeneratedSources);

        var diagnostic = Assert.Single(run.AnalyzerErrors);
        Assert.Equal("UV004", diagnostic.Id);
        Assert.Contains(memberName, diagnostic.GetMessage());
    }

    private static void VerifyGeneratedCopy(GeneratorRun run, int expectedFiles)
    {
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.AnalyzerErrors);
        Assert.Empty(run.CompilationErrors);
        Assert.Equal(expectedFiles, run.GeneratedSources.Length);

        using var assembly = GeneratorHarness.Load(run);
        Assert.True((bool)assembly.GetType("Check").GetMethod("Verify")!.Invoke(null, null)!);
    }
}
