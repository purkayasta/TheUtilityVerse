// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class VirtualPropertyStorageTests
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
    [InlineData("get; set;", "public override int Value { get; set; } = 2;")]
    [InlineData("get; init;", "public override int Value { get; init; } = 2;")]
    [InlineData("get;", "public override int Value { get; } = 2;")]
    [InlineData("get; set;", "public sealed override int Value { get; set; } = 2;")]
    [InlineData("""
        get; set;
        """, """
        private int value = 2;

        public override int Value
        {
            get => value;
            set => this.value = value;
        }
        """)]
    [InlineData("""
        get; set;
        """, """
        public override int Value
        {
            get => base.Value;
            set => base.Value = value + 1;
        }
        """)]
    public void DeepCopy_PreservesBothBackingSlots_WhenStoredPropertyIsOverridden(
        string accessors,
        string declaration) => Evaluate($$"""
        [DeepCopy]
        public partial class Base
        {
            public virtual int Value { {{accessors}} } = 7;
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            {{declaration}}

            public int ReadBaseValue() => base.Value;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                var copy = source.DeepCopy();

                return !ReferenceEquals(source, copy) && copy.ReadBaseValue() == 7 &&
                    copy.Value == source.Value;
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesBothSlots_WhenBaseDoesNotOptIn() => Evaluate("""
        public class Base
        {
            public virtual int Value { get; set; } = 7;
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public override int Value { get; set; } = 2;

            public int ReadBaseValue() => base.Value;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                var copy = source.DeepCopy();

                return copy.ReadBaseValue() == 7 && copy.Value == 2;
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesInheritedSlots_WhenIntermediateTypeDoesNotOptIn() => Evaluate("""
        public class Base
        {
            public virtual int Value { get; set; } = 7;
        }

        public class Middle : Base
        {
            public override int Value { get; set; } = 2;

            public int ReadBaseValue() => base.Value;
        }

        [DeepCopy]
        public partial class Derived : Middle
        {
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                var copy = source.DeepCopy();

                return copy.ReadBaseValue() == 7 && copy.Value == 2;
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesCompleteOverrideChain_WhenIntermediateOverrideHasNoStoredProperty() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public virtual int Value { get; set; } = 7;
        }

        [DeepCopy]
        public partial class Middle : Base
        {
            public override int Value
            {
                get => base.Value;
                set => base.Value = value;
            }
        }

        [DeepCopy]
        public partial class Derived : Middle
        {
            public override int Value { get; set; } = 2;

            public int ReadBaseValue() => base.Value;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                var copy = source.DeepCopy();

                return copy.ReadBaseValue() == 7 && copy.Value == 2;
            }
        }
        """);

    [Fact]
    public void DeepCopy_ClonesBothGenericSlots_AndPreservesAliases() => Evaluate("""
        [DeepCopy]
        public partial class Base<T> where T : class
        {
            public virtual List<T> Values { get; set; } = new();
        }

        [DeepCopy]
        public partial class Derived<TUnused, T> : Base<T> where T : class
        {
            public override List<T> Values { get; set; } = new();

            public List<T> ReadBaseValues() => base.Values;
        }

        [DeepCopy]
        public partial class Node
        {
            public Node? Next { get; set; }
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var node = new Node();
                node.Next = node;
                var source = new Derived<int, Node>();
                source.Values.Add(node);
                source.ReadBaseValues().Add(node);

                var copy = source.DeepCopy();

                return !ReferenceEquals(copy.Values, source.Values) &&
                    !ReferenceEquals(copy.ReadBaseValues(), source.ReadBaseValues()) &&
                    !ReferenceEquals(copy.Values, copy.ReadBaseValues()) &&
                    ReferenceEquals(copy.Values[0], copy.ReadBaseValues()[0]) &&
                    ReferenceEquals(copy.Values[0], copy.Values[0].Next);
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesVirtualStoredProperties_WhenTheyAreNotOverridden() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public virtual List<int> Values { get; set; } = new();

            public virtual Base? Next { get; set; }
        }

        [DeepCopy]
        public partial class Derived : Base
        {
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                source.Values.Add(7);
                source.Next = source;

                var copy = source.DeepCopy();
                source.Values[0] = 9;

                return copy.Values[0] == 7 && ReferenceEquals(copy, copy.Next) &&
                    !ReferenceEquals(source.Values, copy.Values);
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesBothPropertySlots_WhenDerivedPropertyHidesInsteadOfOverrides() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public virtual List<int> Values { get; set; } = new();
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public new List<int> Values { get; set; } = new();

            public List<int> ReadBaseValues() => base.Values;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                source.Values.Add(2);
                source.ReadBaseValues().Add(7);

                var copy = source.DeepCopy();
                source.Values[0] = 3;
                source.ReadBaseValues()[0] = 9;

                return copy.Values[0] == 2 && copy.ReadBaseValues()[0] == 7 &&
                    !ReferenceEquals(copy.Values, copy.ReadBaseValues());
            }
        }
        """);

    [Fact]
    public void DeepCopy_PreservesOverrideStorage_WhenBasePropertyIsAbstract() => Evaluate("""
        [DeepCopy]
        public abstract partial class Base
        {
            public abstract List<int> Values { get; set; }
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public override List<int> Values { get; set; } = new();
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                source.Values.Add(7);
                var copy = source.DeepCopy();
                source.Values[0] = 9;

                return copy.Values[0] == 7 && !ReferenceEquals(source.Values, copy.Values);
            }
        }
        """);

    [Fact]
    public void DeepCopy_CopiesExplicitFields_WithoutInvokingVirtualAccessors() => Evaluate("""
        [DeepCopy]
        public partial class Base
        {
            public static bool RejectAccess;

            private List<int> values = new();

            public virtual List<int> Values
            {
                get
                {
                    if (RejectAccess)
                    {
                        throw new InvalidOperationException();
                    }

                    return values;
                }
                set => values = value;
            }

            public List<int> ReadBaseValues() => values;
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            private List<int> values = new();

            public override List<int> Values
            {
                get
                {
                    if (RejectAccess)
                    {
                        throw new InvalidOperationException();
                    }

                    return values;
                }
                set => values = value;
            }

            public List<int> ReadDerivedValues() => values;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                source.ReadBaseValues().Add(7);
                source.ReadDerivedValues().Add(2);
                Base.RejectAccess = true;

                var copy = source.DeepCopy();
                source.ReadBaseValues()[0] = 9;
                source.ReadDerivedValues()[0] = 3;

                return copy.ReadBaseValues()[0] == 7 && copy.ReadDerivedValues()[0] == 2 &&
                    !ReferenceEquals(copy.ReadBaseValues(), copy.ReadDerivedValues());
            }
        }
        """);

    [Theory]
    [InlineData("auto")]
    [InlineData("explicit")]
    [InlineData("abstract")]
    public void DeepCopy_ValidatesExternalBaseStorage_WhenOnlyMetadataIsAvailable(string shape)
    {
        var declaration = shape switch
        {
            "auto" => "public virtual int Value { get; set; }",
            "abstract" => "public abstract int Value { get; set; }",
            _ => """
                private int value;

                public virtual int Value
                {
                    get => value;
                    set => this.value = value;
                }
                """
        };

        var baseRun = GeneratorHarness.Run($$"""
            [DeepCopy]
            public {{(shape == "abstract" ? "abstract " : "")}}partial class ExternalBase
            {
                {{declaration}}
            }
            """);

        Assert.Empty(baseRun.GeneratorDiagnostics);
        Assert.Empty(baseRun.CompilationErrors);
        Assert.Empty(baseRun.AnalyzerErrors);

        using var stream = new MemoryStream();
        var emit = baseRun.OutputCompilation.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        var reference = MetadataReference.CreateFromImage(stream.ToArray());
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class Derived : ExternalBase
            {
                public override int Value { get; set; }
            }
            """, [.. GeneratorHarness.References, reference]);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);

        Assert.Single(run.GeneratedSources);
        Assert.Empty(run.AnalyzerErrors);

        using var output = new MemoryStream();
        var derivedEmit = run.OutputCompilation.Emit(output, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(derivedEmit.Success, string.Join(Environment.NewLine, derivedEmit.Diagnostics));
    }

    [Fact]
    public Task Analyzer_AcceptsStoredOverride_OnModernFrameworkWithoutRunningTheGenerator() => AnalyzerVerifier.VerifyAsync("""
        [DeepCopy]
        public partial class Base
        {
            public virtual int Value { get; set; }
        }

        [DeepCopy]
        public partial class Derived : Base
        {
            public override int Value { get; set; }
        }
        """);

    [Fact]
    public void ShallowCopy_PreservesBothBackingFields_WhenVirtualStoredPropertyIsOverridden() => Evaluate("""
        [ShallowCopy]
        public partial class Base
        {
            public virtual List<int> Values { get; set; } = new();
        }

        [ShallowCopy]
        public partial class Derived : Base
        {
            public override List<int> Values { get; set; } = new();

            public List<int> ReadBaseValues() => base.Values;
        }

        public static class Check
        {
            public static bool Evaluate()
            {
                var source = new Derived();
                source.Values.Add(2);
                source.ReadBaseValues().Add(7);

                var copy = source.ShallowCopy();

                return !ReferenceEquals(source, copy) &&
                    ReferenceEquals(source.Values, copy.Values) &&
                    ReferenceEquals(source.ReadBaseValues(), copy.ReadBaseValues()) &&
                    copy.Values[0] == 2 && copy.ReadBaseValues()[0] == 7;
            }
        }
        """);
}
