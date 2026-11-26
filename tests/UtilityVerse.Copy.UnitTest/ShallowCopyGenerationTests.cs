// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class ShallowCopyGenerationTests
{
    private const string GeneratedCodeAttribute = """[System.CodeDom.Compiler.GeneratedCode("ShallowCopyGenerator", "1.0")]""";

    [Fact]
    public void ShallowCopy_UsesMemberwiseClone_WhenTypeIsClass()
    {
        var run = GeneratorHarness.Run("""
            namespace Sample;

            [ShallowCopy]
            public partial class Address
            {
                public string? Street { get; set; }
            }

            [ShallowCopy]
            public partial class Order
            {
                public int Id { get; set; }
                public string? Name { get; set; }
                public DateTime PlacedAt { get; set; }
                public Address? ShipTo { get; set; }
                public int[]? Quantities { get; set; }
                public Address[]? History { get; set; }
                public List<Address>? Lines { get; set; }
                public HashSet<int>? Tags { get; set; }
                public Dictionary<string, int>? Counts { get; set; }
                public Tuple<int, string>? Pair { get; set; }
                public (int, string) ValuePair { get; set; }
            }
            """);

        var order = run.GeneratedFile("Sample_Order_ShallowCopy.g.cs");
        var address = run.GeneratedFile("Sample_Address_ShallowCopy.g.cs");

        Assert.Contains(GeneratedCodeAttribute, order);
        Assert.Contains("public Order ShallowCopy()", order);
        Assert.Contains("var copy = (Order)this.MemberwiseClone();", order);
        Assert.Contains("copy.Quantities = this.Quantities != null ? (int[]?)this.Quantities.Clone() : null;", order);
        Assert.Contains("copy.History = this.History != null ? (Sample.Address[]?)this.History.Clone() : null;", order);
        Assert.Contains("copy.Pair = this.Pair;", order);
        Assert.DoesNotContain("copy.Lines", order);
        Assert.DoesNotContain("copy.ShipTo", order);
        Assert.Contains("return copy;", order);
        Assert.Contains("var copy = (Address)this.MemberwiseClone();", address);
    }

    [Fact]
    public void ShallowCopy_UsesMemberwiseClone_WhenTypeIsRecord()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            [ShallowCopy]
            public partial record Person(string Name, int Age);
            """).GeneratedFile("Sample_Person_ShallowCopy.g.cs");

        Assert.Contains("public partial record Person", generated);
        Assert.Contains("public Person ShallowCopy()", generated);
        Assert.Contains("var copy = (Person)this.MemberwiseClone();", generated);
        Assert.Contains("return copy;", generated);
    }

    [Fact]
    public void ShallowCopy_ReturnsThis_WhenTypeIsStruct()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            public partial struct Money : IShallowCopy
            {
                public decimal Amount { get; set; }
                public string? Currency { get; set; }
            }
            """).GeneratedFile("Sample_Money_ShallowCopy.g.cs");

        Assert.Contains("public partial struct Money", generated);
        Assert.Contains("public Money ShallowCopy()", generated);
        Assert.Contains("return this;", generated);
        Assert.DoesNotContain("MemberwiseClone", generated);
    }

    [Fact]
    public void ShallowCopy_UsesMemberwiseClone_WhenTypeIsRecordStruct()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            public partial record struct Coordinate(double Latitude, double Longitude) : IShallowCopy;
            """).GeneratedFile("Sample_Coordinate_ShallowCopy.g.cs");

        Assert.Contains("public partial record struct Coordinate", generated);
        Assert.Contains("public Coordinate ShallowCopy()", generated);
        Assert.Contains("var copy = (Coordinate)this.MemberwiseClone();", generated);
    }

    [Fact]
    public void ShallowCopy_Generates_WhenClassOptsInViaInterface()
    {
        var generated = GeneratorHarness.Run("""
            namespace Sample;

            public partial class ViaInterface : IShallowCopy
            {
                public int Id { get; set; }
            }
            """).GeneratedFile("Sample_ViaInterface_ShallowCopy.g.cs");

        Assert.Contains(GeneratedCodeAttribute, generated);
        Assert.Contains("public partial class ViaInterface", generated);
        Assert.Contains("public ViaInterface ShallowCopy()", generated);
        Assert.Contains("var copy = (ViaInterface)this.MemberwiseClone();", generated);
    }
}
