// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class PhysicalStorageAccessTests
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

    [Fact]
    public void DeepCopy_DoesNotInvokeOverrideAccessors_WhenTheyThrow() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public virtual List<int> Values { get; set; } = new() { 7 };
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public override List<int> Values
            {
                get => throw new InvalidOperationException();
                set => throw new InvalidOperationException();
            }

            public List<int> ReadBaseValues() => base.Values;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                var copy = source.DeepCopy();
                source.ReadBaseValues()[0] = 9;

                return copy.ReadBaseValues()[0] == 7 &&
                    !ReferenceEquals(copy.ReadBaseValues(), source.ReadBaseValues());
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesSharedBackingValue_WhenReadonlyBaseAndOverrideAliasIt() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public virtual List<int> Values { get; } = new() { 7 };
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public override List<int> Values { get; }

            public Derived()
            {
                Values = base.Values;
            }

            public List<int> ReadBaseValues() => base.Values;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                var copy = source.DeepCopy();
                source.Values[0] = 9;

                return copy.Values[0] == 7 && ReferenceEquals(copy.Values, copy.ReadBaseValues()) &&
                    !ReferenceEquals(copy.Values, source.Values);
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesNestedGenericParameterOrderAndConstraints() => Evaluate("""
        public partial class Outer<TOuter> where TOuter : class, new()
        {
            [DeepCopy]
            public partial class Base<TInner> where TInner : struct
            {
                public virtual (TOuter Item, TInner Value) State { get; set; }
            }

            [DeepCopy]
            public partial class Derived<TInner> : Base<TInner> where TInner : struct
            {
                public override (TOuter Item, TInner Value) State { get; set; }

                public void SetBaseState((TOuter Item, TInner Value) state) => base.State = state;

                public (TOuter Item, TInner Value) ReadBaseState() => base.State;
            }
        }

        [DeepCopy]
        public partial class Node
        {
            public int Id { get; set; }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Outer<Node>.Derived<int>();
                source.SetBaseState((new Node { Id = 7 }, 1));
                source.State = (new Node { Id = 9 }, 2);
                var copy = source.DeepCopy();
                source.ReadBaseState().Item.Id = 11;
                source.State.Item.Id = 13;

                return copy.ReadBaseState().Item.Id == 7 && copy.ReadBaseState().Value == 1 &&
                    copy.State.Item.Id == 9 && copy.State.Value == 2;
            }
        }
        """);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeepCopy_PreservesExternalBaseSlots_WhenBaseAssemblyHasPhysicalSupport(bool generic)
    {
        var parameters = generic ? "<T>" : "";
        var arguments = generic ? "<int>" : "";
        var baseRun = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial class ExternalBase{{parameters}}
            {
                public virtual List<int> Values { get; } = new() { 7 };
            }
            """);

        Assert.Empty(baseRun.AnalyzerErrors);

        using var baseStream = new MemoryStream();
        var baseEmit = baseRun.OutputCompilation.Emit(baseStream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(baseEmit.Success, string.Join(Environment.NewLine, baseEmit.Diagnostics));

        var run = GeneratorHarness.Run($$"""
            [DeepCopy]
            public partial class Derived : ExternalBase{{arguments}}
            {
                public override List<int> Values { get; } = new() { 2 };

                public List<int> ReadBaseValues() => base.Values;
            }

            public static class Check
            {
                public static bool Evaluate()
                {
                    var source = new Derived();
                    var copy = source.DeepCopy();
                    source.Values[0] = 3;
                    source.ReadBaseValues()[0] = 9;

                    return copy.Values[0] == 2 && copy.ReadBaseValues()[0] == 7 &&
                        !ReferenceEquals(copy.Values, copy.ReadBaseValues());
                }
            }
            """, [.. GeneratorHarness.References, MetadataReference.CreateFromImage(baseStream.ToArray())]);

        Assert.Empty(run.AnalyzerErrors);

        using var stream = new MemoryStream();
        var emit = run.OutputCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        stream.Position = 0;
        baseStream.Position = 0;

        using var assembly = LoadedAssembly.From(stream, new[] { baseStream });

        Assert.True((bool)assembly.GetType("Check").GetMethod("Evaluate")!.Invoke(null, null)!);
    }

    [Fact]
    public async Task UV004_RejectsLegacyExternalBase_WithoutPhysicalStorageCapability()
    {
        var framework = await ReferenceAssemblies.Net.Net60.ResolveAsync(
            LanguageNames.CSharp,
            TestContext.Current.CancellationToken);
        var baseRun = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class ExternalBase
            {
                public virtual int Value { get; set; }
            }
            """, [.. framework, MetadataReference.CreateFromFile(typeof(DeepCopy).Assembly.Location)]);

        using var stream = new MemoryStream();
        var emit = baseRun.OutputCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Derived : ExternalBase
            {
                public override int Value { get; set; }
            }
            """, [.. GeneratorHarness.References, MetadataReference.CreateFromImage(stream.ToArray())]);

        Assert.Empty(run.GeneratedSources);
        Assert.Contains(run.AnalyzerErrors, diagnostic =>
            diagnostic.Id == "UV004" && diagnostic.GetMessage().Contains("rebuilt with accessible physical backing storage support"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeepCopy_PreservesPrivateNestedSlots_WhenContainingTypeIsGeneric(bool generic)
    {
        var parameters = generic ? "<@class>" : "";
        var arguments = generic ? "<int>" : "";

        Evaluate($$"""
            public partial class Outer{{parameters}}
            {
                [DeepCopy]
                private partial class Base<T>
                {
                    public virtual List<T> Values { get; } = new();
                }

                [DeepCopy]
                private partial class Derived : Base<int>
                {
                    public override List<int> Values { get; } = new();

                    public List<int> ReadBaseValues() => base.Values;
                }

                public static bool Evaluate()
                {
                    var source = new Derived();
                    source.ReadBaseValues().Add(7);
                    source.Values.Add(2);
                    var copy = source.DeepCopy();
                    source.ReadBaseValues()[0] = 9;
                    source.Values[0] = 3;

                    return copy.ReadBaseValues()[0] == 7 && copy.Values[0] == 2;
                }
            }

            public static class Check
            {
                public static bool Evaluate() => Outer{{arguments}}.Evaluate();
            }
            """);
    }

    [Fact]
    public void DeepCopy_PreservesProtectedNestedBaseStorage() => Evaluate("""
        public partial class Container
        {
            [DeepCopy]
            protected partial class Base
            {
                public virtual int Value { get; set; } = 7;
            }
        }

        public partial class DerivedContainer : Container
        {
            [DeepCopy]
            private partial class Derived : Base
            {
                public override int Value { get; set; } = 2;

                public int ReadBaseValue() => base.Value;
            }

            public static bool Evaluate()
            {
                var copy = new Derived().DeepCopy();

                return copy.Value == 2 && copy.ReadBaseValue() == 7;
            }
        }

        public static class Check
        {
            public static bool Evaluate() => DerivedContainer.Evaluate();
        }
        """);

    [Fact]
    public void DeepCopy_ReadsPhysicalPrimaryConstructorArguments_WhenOverrideAccessorsThrow() => Evaluate("""
        [DeepCopy]
        public partial class Base(int value)
        {
            public virtual int Value { get; set; } = value;
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public Derived() : base(7)
            {
            }

            public override int Value
            {
                get => throw new InvalidOperationException();
                set => throw new InvalidOperationException();
            }

            public int ReadBaseValue() => base.Value;
        }

        public static class Check
        {
            public static bool Evaluate() => new Derived().DeepCopy().ReadBaseValue() == 7;
        }
        """);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeepCopy_UsesGenericFieldAccess_OnlyWhenConsumerSupportsIt(bool supported)
    {
        var references = supported ? ReferenceAssemblies.Net.Net90 : ReferenceAssemblies.Net.Net80;
        var framework = await references.ResolveAsync(LanguageNames.CSharp, TestContext.Current.CancellationToken);
        var check = supported ? """
            public static class Check
            {
                public static bool Evaluate()
                {
                    var source = new Derived();
                    source.ReadBaseValues().Add(7);
                    source.Values.Add(2);
                    var copy = source.DeepCopy();

                    return copy.ReadBaseValues()[0] == 7 && copy.Values[0] == 2;
                }
            }
            """ : "";
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Base<T>
            {
                public virtual List<T> Values { get; } = new();
            }

            [DeepCopy]
            public partial class Derived : Base<int>
            {
                public override List<int> Values { get; } = new();

                public List<int> ReadBaseValues() => base.Values;
            }
            """ + check, [.. framework, MetadataReference.CreateFromFile(typeof(DeepCopy).Assembly.Location)]);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);

        if (supported)
        {
            Assert.Empty(run.AnalyzerErrors);

            using var assembly = GeneratorHarness.Load(run);

            Assert.True((bool)assembly.GetType("Check").GetMethod("Evaluate")!.Invoke(null, null)!);
        }
        else
        {
            Assert.Contains(run.AnalyzerErrors, diagnostic => diagnostic.Id == "UV004");
            Assert.DoesNotContain("UnsafeAccessor", run.GeneratedText);
        }
    }

    [Fact]
    public void DeepCopy_AccessesPublicNestedStorage_ThroughPrivateContainingType() => Evaluate("""
        public partial class Outer
        {
            private partial class Hidden
            {
                [DeepCopy]
                public partial class Base
                {
                    public virtual int Value { get; set; } = 7;
                }
            }

            [DeepCopy]
            private partial class Derived : Hidden.Base
            {
                public override int Value { get; set; } = 2;

                public int ReadBaseValue() => base.Value;
            }

            public static bool Evaluate()
            {
                var copy = new Derived().DeepCopy();

                return copy.Value == 2 && copy.ReadBaseValue() == 7;
            }
        }

        public static class Check
        {
            public static bool Evaluate() => Outer.Evaluate();
        }
        """);

    [Fact]
    public void UV004_ReportsStorageHelperCollision_InsteadOfEmittingDuplicateTypes()
    {
        const string model = """
            [DeepCopy]
            public partial class Base
            {
                public virtual int Value { get; set; }
            }
            """;
        var first = GeneratorHarness.Run(model);
        var helper = first.GeneratedSources.Single().SyntaxTree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText.StartsWith("__UtilityVerseStorage_", StringComparison.Ordinal));
        var run = GeneratorHarness.Run(model + $$"""
            namespace UtilityVerse.Copy.Generated
            {
                internal static class {{helper.Identifier.ValueText}}
                {
                }
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.GeneratedSources);
        Assert.Contains(run.AnalyzerErrors, diagnostic =>
            diagnostic.Id == "UV004" && diagnostic.GetMessage().Contains("helper name is reserved"));
    }

    [Fact]
    public void UV004_ReportsInaccessibleStorage_WhenNonPartialOwnerCannotHostHelper()
    {
        var run = GeneratorHarness.Run("""
            public class Container
            {
                protected class Base
                {
                    public virtual int Value { get; set; }
                }
            }

            public partial class DerivedContainer : Container
            {
                [DeepCopy]
                private partial class Derived : Base
                {
                    public override int Value { get; set; }
                }
            }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.GeneratedSources);
        Assert.Contains(run.AnalyzerErrors, diagnostic =>
            diagnostic.Id == "UV004" && diagnostic.GetMessage().Contains("accessible physical backing storage"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeepCopy_PreservesProtectedPropertyTypes_WithoutExposingGeneratedPublicHelpers(bool optedInBase) => Evaluate($$"""
        {{(optedInBase ? "[DeepCopy]" : "")}}
        public partial class Base
        {
            [DeepCopy]
            protected partial class Payload
            {
                public int Id { get; set; }
            }

            protected virtual Payload Value { get; set; } = new() { Id = 7 };
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            protected override Payload Value { get; set; } = new() { Id = 2 };

            public int ReadBaseValue() => base.Value.Id;

            public int ReadDerivedValue() => Value.Id;

            public void Mutate()
            {
                base.Value.Id = 9;
                Value.Id = 3;
            }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                var copy = source.DeepCopy();
                source.Mutate();

                return copy.ReadBaseValue() == 7 && copy.ReadDerivedValue() == 2;
            }
        }
        """);
}
