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

    private const string GeneratedCodeAttribute = """[System.CodeDom.Compiler.GeneratedCode("DeepCopyGenerator", "1.0")]""";

    [Fact]
    public void DeepCopy_AssignsDirectly_WhenPropertiesArePrimitivesAndBuiltIns()
    {
        var generated = GeneratorHarness.Run("""
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
            """).GeneratedFile("Sample_Scalars_DeepCopy.g.cs");

        Assert.Contains(GeneratedCodeAttribute, generated);
        Assert.Contains("public Scalars DeepCopy()", generated);
        Assert.Contains("return new Scalars", generated);
        Assert.Contains("Id = this.Id,", generated);
        Assert.Contains("Count = this.Count,", generated);
        Assert.Contains("IsActive = this.IsActive,", generated);
        Assert.Contains("Ratio = this.Ratio,", generated);
        Assert.Contains("Optional = this.Optional,", generated);
        Assert.Contains("Name = this.Name,", generated);
        Assert.Contains("CreatedAt = this.CreatedAt,", generated);
        Assert.Contains("Day = this.Day,", generated);
        Assert.Contains("Duration = this.Duration,", generated);
        Assert.Contains("Key = this.Key,", generated);
        Assert.Contains("Amount = this.Amount,", generated);
        Assert.Contains("Link = this.Link,", generated);
        Assert.Contains("Release = this.Release,", generated);
        Assert.Contains("Status = this.Status,", generated);
        Assert.DoesNotContain("?.DeepCopy()", generated);
    }

    [Fact]
    public void DeepCopy_ClonesArrays_WhenElementsArePrimitiveOrOptedIn()
    {
        var generated = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Arrays
            {
                public int[]? Numbers { get; set; }
                public string[]? Names { get; set; }
                public Address[]? Addresses { get; set; }
            }
            """).GeneratedFile("Sample_Arrays_DeepCopy.g.cs");

        Assert.Contains("Numbers = this.Numbers?.ToArray(),", generated);
        Assert.Contains("Names = this.Names?.ToArray(),", generated);
        Assert.Contains("Addresses = this.Addresses?.Select(x => x?.DeepCopy()).ToArray(),", generated);
    }

    [Fact]
    public void DeepCopy_ProjectsToList_WhenPropertiesAreListLike()
    {
        var generated = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Lists
            {
                public List<int>? Numbers { get; set; }
                public List<Address>? Addresses { get; set; }
                public IList<Address>? AddressList { get; set; }
            }
            """).GeneratedFile("Sample_Lists_DeepCopy.g.cs");

        Assert.Contains("Numbers = this.Numbers?.Select(x => x).ToList(),", generated);
        Assert.Contains("Addresses = this.Addresses?.Select(x => x?.DeepCopy()).ToList(),", generated);
        Assert.Contains("AddressList = this.AddressList?.Select(x => x?.DeepCopy()).ToList(),", generated);
    }

    [Fact]
    public void DeepCopy_ProjectsToList_WhenPropertiesAreEnumerableInterfaces()
    {
        var generated = GeneratorHarness.Run($$"""
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
            """).GeneratedFile("Sample_Enumerables_DeepCopy.g.cs");

        Assert.Contains("Sequence = this.Sequence?.Select(x => x?.DeepCopy()).ToList(),", generated);
        Assert.Contains("Collection = this.Collection?.Select(x => x).ToList(),", generated);
        Assert.Contains("ReadOnlyList = this.ReadOnlyList?.Select(x => x?.DeepCopy()).ToList(),", generated);
        Assert.Contains("ReadOnlyCollection = this.ReadOnlyCollection?.Select(x => x).ToList(),", generated);
    }

    [Fact]
    public void DeepCopy_CreatesNewHashSet_WhenPropertiesAreHashSets()
    {
        var generated = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Sets
            {
                public HashSet<int>? Numbers { get; set; }
                public HashSet<Address>? Addresses { get; set; }
            }
            """).GeneratedFile("Sample_Sets_DeepCopy.g.cs");

        Assert.Contains("Numbers = this.Numbers != null ? new HashSet<int>(this.Numbers.Select(x => x).ToList()) : null,", generated);
        Assert.Contains("Addresses = this.Addresses != null ? new HashSet<Sample.Address>(this.Addresses.Select(x => x?.DeepCopy()).ToList()) : null,", generated);
    }

    [Fact]
    public void DeepCopy_UsesToDictionary_WhenPropertiesAreDictionaries()
    {
        var generated = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Maps
            {
                public Dictionary<int, string>? Names { get; set; }
                public Dictionary<string, Address>? AddressesByName { get; set; }
            }
            """).GeneratedFile("Sample_Maps_DeepCopy.g.cs");

        Assert.Contains("Names = this.Names?.ToDictionary(x => x.Key, x => x.Value),", generated);
        Assert.Contains("AddressesByName = this.AddressesByName?.ToDictionary(x => x.Key, x => x.Value?.DeepCopy()),", generated);
    }

    [Fact]
    public void DeepCopy_RebuildsTuples_WhenPropertiesAreTuples()
    {
        var generated = GeneratorHarness.Run($$"""
            namespace Sample;

            {{Address}}

            [DeepCopy]
            public partial class Tuples
            {
                public Tuple<int, Address> WithAddress { get; set; } = null!;
                public (int Id, string Name) ValuePair { get; set; }
            }
            """).GeneratedFile("Sample_Tuples_DeepCopy.g.cs");

        Assert.Contains("WithAddress = this.WithAddress is { } x ? new System.Tuple<int, Sample.Address>(x.Item1, x.Item2?.DeepCopy()) : default,", generated);
        Assert.Contains("ValuePair = this.ValuePair,", generated);
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

        var person = run.GeneratedFile("Sample_Person_DeepCopy.g.cs");
        var address = run.GeneratedFile("Sample_Address_DeepCopy.g.cs");

        Assert.Contains("Name = this.Name,", person);
        Assert.Contains("Home = this.Home?.DeepCopy(),", person);
        Assert.Contains("public Address DeepCopy()", address);
        Assert.Contains("Street = this.Street,", address);
    }

    [Fact]
    public void DeepCopy_UsesObjectInitializer_WhenTypeIsRecordWithInitProperties()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial record Person
            {
                public string? Name { get; init; }
                public int Age { get; init; }
            }
            """).GeneratedFile("Sample_Person_DeepCopy.g.cs");

        Assert.Contains("public partial record Person", generated);
        Assert.Contains("return new Person", generated);
        Assert.Contains("Name = this.Name,", generated);
        Assert.Contains("Age = this.Age,", generated);
    }

    [Fact]
    public void DeepCopy_UsesPrimaryConstructor_WhenTypeIsPositionalRecord()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial record Point(int X, int Y);
            """).GeneratedFile("Sample_Point_DeepCopy.g.cs");

        Assert.Contains("public partial record Point", generated);
        Assert.Contains("return new Point(this.X, this.Y);", generated);
        Assert.DoesNotContain("X = this.X,", generated);
    }

    [Fact]
    public void DeepCopy_GeneratesStructPartial_WhenStructOptsInViaInterface()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            public partial struct Money : IDeepCopy
            {
                public decimal Amount { get; set; }
                public string? Currency { get; set; }
            }
            """).GeneratedFile("Sample_Money_DeepCopy.g.cs");

        Assert.Contains("public partial struct Money", generated);
        Assert.Contains("public Money DeepCopy()", generated);
        Assert.Contains("Amount = this.Amount,", generated);
        Assert.Contains("Currency = this.Currency,", generated);
    }

    [Fact]
    public void DeepCopy_GeneratesRecordStructPartial_WhenRecordStructOptsInViaInterface()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            public partial record struct Coordinate : IDeepCopy
            {
                public double Latitude { get; set; }
                public double Longitude { get; set; }
            }
            """).GeneratedFile("Sample_Coordinate_DeepCopy.g.cs");

        Assert.Contains("public partial record struct Coordinate", generated);
        Assert.Contains("public Coordinate DeepCopy()", generated);
        Assert.Contains("Latitude = this.Latitude,", generated);
        Assert.Contains("Longitude = this.Longitude,", generated);
    }

    [Fact]
    public void DeepCopy_UsesLargestConstructor_WhenNoParameterlessConstructor()
    {
        var generated = GeneratorHarness.Run($$"""
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
            """).GeneratedFile("Sample_Customer_DeepCopy.g.cs");

        Assert.Contains("return new Customer(this.Id, this.Home?.DeepCopy(), default(string?));", generated);
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

        var generated = run.GeneratedFile("GlobalThing_DeepCopy.g.cs");

        Assert.DoesNotContain("namespace", generated);
        Assert.Contains("public partial class GlobalThing", generated);
        Assert.Contains("public GlobalThing DeepCopy()", generated);
        Assert.Contains("Id = this.Id,", generated);
    }

    [Fact]
    public void DeepCopy_OnlyGeneratesDeepCopy_WhenTypeOptsIntoDeepAndShallow()
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

        Assert.Equal(["Sample_Both_DeepCopy.g.cs"], run.HintNames);
        Assert.Contains("public Both DeepCopy()", run.GeneratedText);
        Assert.DoesNotContain("ShallowCopy", run.GeneratedText);
    }

    [Fact]
    public void DeepCopy_Generates_WhenClassOptsInViaInterface()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            public partial class ViaInterface : IDeepCopy
            {
                public int Id { get; set; }
                public string? Name { get; set; }
            }
            """).GeneratedFile("Sample_ViaInterface_DeepCopy.g.cs");

        Assert.Contains("public partial class ViaInterface", generated);
        Assert.Contains("public ViaInterface DeepCopy()", generated);
        Assert.Contains("Id = this.Id,", generated);
        Assert.Contains("Name = this.Name,", generated);
    }

    [Fact]
    public void DeepCopy_IgnoresGetOnlyProperties_WhenTypeHasComputedMembers()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            [DeepCopy]
            public partial class Computed
            {
                public int Id { get; set; }
                public int Double => Id * 2;
                public string Label { get; } = "fixed";
            }
            """).GeneratedFile("Sample_Computed_DeepCopy.g.cs");

        Assert.Contains("Id = this.Id,", generated);
        Assert.DoesNotContain("Double", generated);
        Assert.DoesNotContain("Label", generated);
    }

    [Fact]
    public void DeepCopy_IncludesBaseProperties_WhenTypeDerivesFromBaseClass()
    {
        var generated = GeneratorHarness.Run("""
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
            """).GeneratedFile("Sample_Product_DeepCopy.g.cs");

        Assert.Contains("Name = this.Name,", generated);
        Assert.Contains("Id = this.Id,", generated);
    }
}
