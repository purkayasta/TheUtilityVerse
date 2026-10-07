// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class ShallowCopyRuntimeTests
{
    private const string Models = """
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
            public Address? ShipTo { get; set; }
            public Address[]? History { get; set; }
        }

        [ShallowCopy]
        public partial record Person(string Name, int Age);

        public partial struct Money : IShallowCopy
        {
            public decimal Amount { get; set; }
        }
        """;

    private static readonly Lazy<LoadedAssembly> Assembly = new(() => GeneratorHarness.Load(GeneratorHarness.Run(Models)));

    private static dynamic NewAddress(string street)
    {
        var address = Assembly.Value.Create("Sample.Address");
        address.Street = street;
        return address;
    }

    private static dynamic NewOrder()
    {
        var order = Assembly.Value.Create("Sample.Order");
        order.Id = 1;
        order.Name = "Original";
        order.ShipTo = NewAddress("dock");
        var history = Array.CreateInstance(Assembly.Value.GetType("Sample.Address"), 1);
        history.SetValue(NewAddress("old"), 0);
        order.History = (dynamic)history;
        return order;
    }

    [Fact]
    public void ShallowCopy_GeneratedCodeCompiles_WhenModelsUseSupportedPropertyTypes()
    {
        var run = GeneratorHarness.Run(Models);

        Assert.Empty(run.CompilationErrors);
    }

    [Fact]
    public void ShallowCopy_CopyKeepsScalars_WhenOriginalScalarsAreMutated()
    {
        var original = NewOrder();
        var copy = original.ShallowCopy();

        original.Id = 2;
        original.Name = "Changed";

        Assert.NotSame(original, copy);
        Assert.Equal(1, (int)copy.Id);
        Assert.Equal("Original", (string)copy.Name);
    }

    [Fact]
    public void ShallowCopy_SharesNestedObject_WhenPropertyIsReferenceType()
    {
        var original = NewOrder();
        var copy = original.ShallowCopy();

        original.ShipTo.Street = "changed";

        Assert.Same(original.ShipTo, copy.ShipTo);
        Assert.Equal("changed", (string)copy.ShipTo.Street);
    }

    [Fact]
    public void ShallowCopy_ClonesArrayButSharesElements_WhenPropertyIsArray()
    {
        var original = NewOrder();
        var copy = original.ShallowCopy();

        Assert.NotSame(original.History, copy.History);
        Assert.Same(original.History[0], copy.History[0]);
    }

    [Fact]
    public void ShallowCopy_ReturnsEqualButDistinctRecord_WhenTypeIsPositionalRecord()
    {
        var original = Assembly.Value.Create("Sample.Person", "Ada", 36);

        var copy = original.ShallowCopy();

        Assert.NotSame(original, copy);
        Assert.Equal((object)original, (object)copy);
    }

    [Fact]
    public void ShallowCopy_ReturnsValueCopy_WhenTypeIsStruct()
    {
        object original = Assembly.Value.Create("Sample.Money");
        original.GetType().GetProperty("Amount")!.SetValue(original, 3m);

        dynamic copy = ((dynamic)original).ShallowCopy();

        Assert.Equal(3m, (decimal)copy.Amount);
    }
}
