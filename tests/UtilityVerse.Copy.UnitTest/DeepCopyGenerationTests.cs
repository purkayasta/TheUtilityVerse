// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class DeepCopyGenerationTests
{
    private const string Address = """
        [DeepCopy]
        public partial class Address
        {
            public string? Street { get; set; }
        }
        """;

    [Fact]
    public void DeepCopy_AssignsDirectly_WhenPropertiesArePrimitivesAndBuiltIns()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            public enum Status { Ready, Done }

            [DeepCopy]
            public partial class Scalars
            {
                public int Id { get; set; }
                public long Count { get; set; }
                public bool IsActive { get; set; }
                public double Ratio { get; set; }
                public int? Optional { get; set; }
                public string? Name { get; set; }
                public DateTime CreatedAt { get; set; }
                public DateOnly Day { get; set; }
                public TimeSpan Duration { get; set; }
                public Guid Key { get; set; }
                public decimal Amount { get; set; }
                public Uri Link { get; set; } = null!;
                public Version Release { get; set; } = null!;
                public Status Status { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_ClonesArrays_WhenElementsArePrimitiveOrOptedIn()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Arrays
            {
                public int[]? Numbers { get; set; }
                public string[]? Names { get; set; }
                public Address[]? Addresses { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_ProjectsToList_WhenPropertiesAreListLike()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Lists
            {
                public List<int>? Numbers { get; set; }
                public List<Address>? Addresses { get; set; }
                public IList<Address>? AddressList { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_ProjectsToList_WhenPropertiesAreEnumerableInterfaces()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Enumerables
            {
                public IEnumerable<Address>? Sequence { get; set; }
                public ICollection<int>? Collection { get; set; }
                public IReadOnlyList<Address>? ReadOnlyList { get; set; }
                public IReadOnlyCollection<string>? ReadOnlyCollection { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_CreatesNewHashSet_WhenPropertiesAreHashSets()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Sets
            {
                public HashSet<int>? Numbers { get; set; }
                public HashSet<Address>? Addresses { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_UsesToDictionary_WhenPropertiesAreDictionaries()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Maps
            {
                public Dictionary<int, string>? Names { get; set; }
                public Dictionary<string, Address>? AddressesByName { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_RebuildsTuples_WhenPropertiesAreTuples()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Tuples
            {
                public Tuple<int, Address> WithAddress { get; set; } = null!;
                public (int Id, string Name) ValuePair { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_CallsNestedDeepCopy_WhenPropertyIsOptedInType()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Person
            {
                public string? Name { get; set; }
                public Address? Home { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_UsesObjectInitializer_WhenTypeIsRecordWithInitProperties()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial record Person
            {
                public string? Name { get; init; }
                public int Age { get; init; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_UsesPrimaryConstructor_WhenTypeIsPositionalRecord()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial record Point(int X, int Y);
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_GeneratesStructPartial_WhenStructOptsInViaInterface()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            public partial struct Money : IDeepCopy
            {
                public decimal Amount { get; set; }
                public string? Currency { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_GeneratesRecordStructPartial_WhenRecordStructOptsInViaInterface()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            public partial record struct Coordinate : IDeepCopy
            {
                public double Latitude { get; set; }
                public double Longitude { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_BypassesExistingConstructorsAndHydratesState_WhenConstructorsDiffer()
    {
        var run = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Customer
            {
                public Customer(int id) => Id = id;

                public Customer(int id, Address? home, string? unmatched)
                {
                    Id = id;
                    Home = home;
                }

                public int Id { get; set; }
                public Address? Home { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_OmitsNamespace_WhenTypeIsInGlobalNamespace()
    {
        var run = GeneratorHarness.Run("""
            [DeepCopy]
            public partial class GlobalThing
            {
                public int Id { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void CopyMethods_GenerateBoth_WhenTypeOptsIntoDeepAndShallow()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            [ShallowCopy]
            public partial class Both
            {
                public int Id { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Equal(2, run.GeneratedSources.Length);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Contains(" ShallowCopy()", run.GeneratedText);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_Generates_WhenClassOptsInViaInterface()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            public partial class ViaInterface : IDeepCopy
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_CopiesStoredGetOnlyProperties_WhenTypeAlsoHasComputedMembers()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Computed
            {
                public int Id { get; set; }
                public int Double => Id * 2;
                public string Label { get; } = "fixed";
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }

    [Fact]
    public void DeepCopy_IncludesBaseProperties_WhenTypeDerivesFromBaseClass()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            public class Entity
            {
                public Guid Id { get; set; }
            }

            [DeepCopy]
            public partial class Product : Entity
            {
                public string? Name { get; set; }
            }
            """);
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.NotEmpty(run.GeneratedSources);
        Assert.Contains(" DeepCopy()", run.GeneratedText);
        Assert.Empty(run.CompilationErrors);
        Assert.Empty(run.AnalyzerErrors);
        using var assembly = GeneratorHarness.Load(run);
    }
}
