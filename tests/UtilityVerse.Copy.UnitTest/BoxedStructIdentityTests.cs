// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class BoxedStructIdentityTests
{
    private const string AliasSource = """
        [DeepCopy]
        public partial class Pair<TFirst, TSecond>
        {
            public TFirst First { get; set; } = default!;

            public TSecond Second { get; set; } = default!;
        }

        [DeepCopy]
        public partial record struct Value
        {
            public Node Child { get; }

            public Value(Node child)
            {
                Child = child;
            }
        }

        [DeepCopy]
        public partial class Node
        {
            public int Id { get; set; }
        }

        public static class Check
        {
            public static bool Aliases(bool shared)
            {
                var value = new Value(new Node { Id = 7 });
                object first = value;
                IDeepCopyable second = shared ? (IDeepCopyable)first : value;
                var source = new Pair<object, IDeepCopyable> { First = first, Second = second };
                var copy = source.DeepCopy();
                var firstValue = (Value)copy.First;
                var secondValue = (Value)copy.Second;

                return ReferenceEquals(copy.First, copy.Second) == shared &&
                    !ReferenceEquals(first, copy.First) &&
                    !ReferenceEquals(second, copy.Second) &&
                    !ReferenceEquals(value.Child, firstValue.Child) &&
                    ReferenceEquals(firstValue.Child, secondValue.Child) &&
                    firstValue.Child.Id == 7;
            }

            public static bool Values()
            {
                var value = new Value(new Node { Id = 7 });
                var source = new Pair<Value, Value?> { First = value, Second = value };
                var copy = source.DeepCopy();
                var direct = value.DeepCopy();
                copy.First = new Value(new Node());

                return copy.Second is Value second && second.Child.Id == 7 &&
                    direct.Child.Id == 7 && !ReferenceEquals(value.Child, direct.Child) &&
                    !ReferenceEquals(value.Child, second.Child) && source.First.Child.Id == 7;
            }
        }
        """;

    private const string CycleSource = """
        [DeepCopy]
        public readonly partial struct Value
        {
            public Node<object> Node { get; }

            public int Code { get; }

            public Value(Node<object> node, int code)
            {
                Node = node;
                Code = code;
            }

            public override int GetHashCode() => Code;
        }

        [DeepCopy]
        public partial class Node<T> where T : notnull
        {
            public T Owner { get; set; } = default!;

            public Dictionary<T, int> Entries { get; } = new();
        }

        public static class Check
        {
            public static bool Cycle()
            {
                var node = new Node<object>();
                object source = new Value(node, 41);
                node.Owner = source;
                node.Entries.Add(source, 7);

                var context = new CopyContext();

                var copy = context.Copy(source);
                var value = (Value)copy;

                return !ReferenceEquals(source, copy) &&
                    ReferenceEquals(copy, context.Copy((IDeepCopyable)source)) &&
                    ReferenceEquals(copy, value.Node.Owner) &&
                    !ReferenceEquals(node, value.Node) &&
                    ReferenceEquals(copy, value.Node.Entries.Keys.Single()) &&
                    value.Node.Entries[copy] == 7 && value.Code == 41;
            }
        }
        """;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeepCopy_PreservesBoxIdentityAcrossObjectAndInterfaceViews(bool shared)
    {
        using var assembly = Load(GeneratorHarness.Run(AliasSource));

        Assert.True((bool)assembly.GetType("Check").GetMethod("Aliases")!.Invoke(null, [shared])!);
    }

    [Fact]
    public void DeepCopy_PreservesOrdinaryAndNullableStructValues()
    {
        using var assembly = Load(GeneratorHarness.Run(AliasSource));

        Assert.True((bool)assembly.GetType("Check").GetMethod("Values")!.Invoke(null, null)!);
    }

    [Fact]
    public void CopyContext_PreservesReadonlyBoxCycle_AndDeferredHashKeys()
    {
        using var assembly = Load(GeneratorHarness.Run(CycleSource));

        Assert.True((bool)assembly.GetType("Check").GetMethod("Cycle")!.Invoke(null, null)!);
    }

    [Fact]
    public async Task DeepCopy_PreservesAcyclicBoxAliases_OnNetStandard20()
    {
        using var assembly = Load(await RunOnNetStandard20(AliasSource));

        Assert.True((bool)assembly.GetType("Check").GetMethod("Aliases")!.Invoke(null, [true])!);
    }

    [Fact]
    public async Task CopyContext_RejectsBoxCycle_WhenConsumerLacksUnbox()
    {
        using var assembly = Load(await RunOnNetStandard20(CycleSource));
        var check = assembly.GetType("Check").GetMethod("Cycle")!;

        var invocation = Assert.Throws<System.Reflection.TargetInvocationException>(() => check.Invoke(null, null));
        var error = Assert.IsType<NotSupportedException>(invocation.InnerException);

        Assert.Contains("boxed", error.Message);
    }

    private static async Task<GeneratorRun> RunOnNetStandard20(string source)
    {
        var framework = await ReferenceAssemblies.NetStandard.NetStandard20.ResolveAsync(
            LanguageNames.CSharp,
            TestContext.Current.CancellationToken);

        return GeneratorHarness.Run(source,
            [.. framework, MetadataReference.CreateFromFile(typeof(DeepCopy).Assembly.Location)]);
    }

    private static LoadedAssembly Load(GeneratorRun run)
    {
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        return GeneratorHarness.Load(run);
    }
}
