// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System.Reflection;
using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class UriRuntimeTypeTests
{
    private const string Models = """
        [DeepCopy]
        public partial class Model
        {
            public Uri? Value { get; set; }

            public Uri?[]? Array { get; set; }

            public List<Uri>? List { get; set; }

            public Dictionary<Uri, Uri>? Map { get; set; }

            public (Uri Left, Uri Right)? Tuple { get; set; }

            public Value? Wrapped { get; set; }
        }

        [DeepCopy]
        public partial struct Value
        {
            public Uri? Uri { get; set; }
        }

        [DeepCopy]
        public partial class Generic<T>
        {
            public T Value { get; set; } = default!;
        }

        public sealed class MutableUri : Uri
        {
            public MutableUri() : base("https://example.test/")
            {
            }

            public List<int> State { get; } = new() { 7 };
        }

        public sealed class CopyableUri : Uri, IDeepCopyable
        {
            public CopyableUri() : base("https://example.test/")
            {
            }

            public List<int> State { get; private set; } = new();

            public Model? Owner { get; set; }

            object IDeepCopyable.Copy(CopyContext context) =>
                context.CopyGenerated(this, static (source, copyContext) =>
                {
                    var copy = new CopyableUri();
                    copyContext.Register(source, copy);

                    copy.State = (List<int>)copyContext.CopySequence(source.State, static (value, _) => value)!;
                    copy.Owner = copyContext.Copy(source.Owner);

                    return copy;
                });
        }

        public static class Check
        {
            public static void Reject(string shape)
            {
                var uri = new MutableUri();
                var source = new Model();

                switch (shape)
                {
                    case "direct":
                        source.Value = uri;
                        break;

                    case "array":
                        source.Array = new[] { uri };
                        break;

                    case "list":
                        source.List = new List<Uri> { uri };
                        break;

                    case "map key":
                        source.Map = new Dictionary<Uri, Uri> { [uri] = new Uri("relative", UriKind.Relative) };
                        break;

                    case "map value":
                        source.Map = new Dictionary<Uri, Uri> { [new Uri("relative", UriKind.Relative)] = uri };
                        break;

                    case "tuple":
                        source.Tuple = (uri, uri);
                        break;

                    case "struct":
                        source.Wrapped = new Value { Uri = uri };
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(shape));
                }

                source.DeepCopy();
            }

            public static bool PreserveExactTypes()
            {
                var relative = new Uri("relative", UriKind.Relative);
                var absolute = new Uri("https://example.test/");
                var source = new Model
                {
                    Array = new Uri?[] { null, relative, absolute },
                    List = new List<Uri> { relative },
                    Map = new Dictionary<Uri, Uri> { [relative] = absolute },
                    Tuple = (relative, absolute),
                    Wrapped = new Value { Uri = relative }
                };
                var copy = source.DeepCopy();

                return copy.Value is null &&
                    !ReferenceEquals(copy.Array, source.Array) &&
                    copy.Array![0] is null &&
                    ReferenceEquals(copy.Array[1], relative) &&
                    ReferenceEquals(copy.Array[2], absolute) &&
                    ReferenceEquals(copy.List![0], relative) &&
                    ReferenceEquals(copy.Map![relative], absolute) &&
                    ReferenceEquals(copy.Tuple!.Value.Left, relative) &&
                    ReferenceEquals(copy.Wrapped!.Value.Uri, relative) &&
                    ReferenceEquals(new Generic<Uri> { Value = absolute }.DeepCopy().Value, absolute);
            }

            public static bool PreserveCustomCopy()
            {
                var uri = new CopyableUri();
                uri.State.Add(7);

                var source = new Model { Value = uri, List = new List<Uri> { uri } };
                uri.Owner = source;

                var copy = source.DeepCopy();

                return copy.Value is CopyableUri derived &&
                    !ReferenceEquals(uri, derived) &&
                    !ReferenceEquals(uri.State, derived.State) &&
                    derived.State.SequenceEqual(new[] { 7 }) &&
                    ReferenceEquals(derived.Owner, copy) &&
                    ReferenceEquals(copy.List![0], derived);
            }
        }
        """;

    [Theory]
    [InlineData("direct")]
    [InlineData("array")]
    [InlineData("list")]
    [InlineData("map key")]
    [InlineData("map value")]
    [InlineData("tuple")]
    [InlineData("struct")]
    public void DeepCopy_RejectsUnsupportedRuntimeUriSubclass(string shape)
    {
        using var assembly = LoadModels();

        var check = assembly.GetType("Check").GetMethod("Reject")!;
        var invocation = Assert.Throws<TargetInvocationException>(() => check.Invoke(null, [shape]));
        var error = Assert.IsType<NotSupportedException>(invocation.InnerException);

        Assert.Contains("MutableUri", error.Message);
    }

    [Theory]
    [InlineData("PreserveExactTypes")]
    [InlineData("PreserveCustomCopy")]
    public void DeepCopy_PreservesSupportedUriBehavior(string method)
    {
        using var assembly = LoadModels();

        Assert.True((bool)assembly.GetType("Check").GetMethod(method)!.Invoke(null, null)!);
    }

    [Fact]
    public void CopyContext_RejectsUnsupportedSubtypeThroughBaseAndObjectViews()
    {
        var source = new UnsupportedUri();
        var context = new CopyContext();

        Assert.Throws<NotSupportedException>(() => context.Copy<Uri>(source));
        Assert.Throws<NotSupportedException>(() => context.Copy<object>(source));

        var exact = new Uri("relative", UriKind.Relative);
        var version = new Version(1, 2);

        Assert.Same(exact, context.Copy<object>(exact));
        Assert.Same(version, context.Copy<object>(version));
        Assert.Null(context.Copy<Uri?>(null));
    }

    [Fact]
    public void Analyzer_RequiresOptInForStructContainingUri()
    {
        var run = GeneratorHarness.Run("""
            public struct Wrapper
            {
                public Uri? Value { get; set; }
            }

            [DeepCopy]
            public partial class Model
            {
                public Wrapper State { get; set; }
            }
            """);

        Assert.Empty(run.CompilationErrors);
        Assert.Contains(run.AnalyzerErrors, diagnostic =>
            diagnostic.Id == "UV002" && diagnostic.GetMessage().Contains("Wrapper"));
    }

    private static LoadedAssembly LoadModels()
    {
        var run = GeneratorHarness.Run(Models);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);

        return GeneratorHarness.Load(run);
    }

    private sealed class UnsupportedUri() : Uri("https://example.test/")
    {
        internal List<int> State { get; } = [7];
    }
}
