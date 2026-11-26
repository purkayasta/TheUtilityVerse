// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using UtilityVerse.Copy.UnitTest.Infrastructure;

namespace UtilityVerse.Copy.UnitTest;

public class DeepCopyRuntimeTests
{
    private const string Models = """
        namespace Sample;

        [DeepCopy]
        public partial class Address
        {
            public string? Street { get; set; }
        }

        [DeepCopy]
        public partial class Person
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public DateTime BornAt { get; set; }
            public Address? Home { get; set; }
            public List<Address>? Addresses { get; set; }
            public Address[]? History { get; set; }
            public int[]? Scores { get; set; }
            public HashSet<int>? Tags { get; set; }
            public Dictionary<string, Address>? ByName { get; set; }
            public IReadOnlyList<Address>? ReadOnlyAddresses { get; set; }
        }

        [DeepCopy]
        public partial record Settings
        {
            public string? Theme { get; init; }
            public Address? Office { get; init; }
        }

        [DeepCopy]
        public partial class Customer
        {
            public Customer(int id, Address? home)
            {
                Id = id;
                Home = home;
            }

            public int Id { get; set; }
            public Address? Home { get; set; }
        }

        public partial struct Money : IDeepCopy
        {
            public decimal Amount { get; set; }
            public string? Currency { get; set; }
        }
        """;

    private static readonly Lazy<LoadedAssembly> Assembly = new(() => GeneratorHarness.Load(GeneratorHarness.Run(Models)));

    private static dynamic NewAddress(string street)
    {
        var address = Assembly.Value.Create("Sample.Address");
        address.Street = street;
        return address;
    }

    private static dynamic NewPerson()
    {
        var person = Assembly.Value.Create("Sample.Person");
        person.Id = 1;
        person.Name = "Original";
        person.BornAt = new DateTime(2000, 1, 1);
        person.Home = NewAddress("home");
        person.Addresses = Assembly.Value.CreateList("Sample.Address", NewAddress("first"));
        person.History = ToAddressArray(NewAddress("old"));
        person.Scores = new[] { 1, 2 };
        person.Tags = new HashSet<int> { 7 };
        person.ByName = ToAddressDictionary("work", NewAddress("office"));
        person.ReadOnlyAddresses = Assembly.Value.CreateList("Sample.Address", NewAddress("readonly"));
        return person;
    }

    private static Array ToAddressArray(params object[] addresses)
    {
        var array = Array.CreateInstance(Assembly.Value.GetType("Sample.Address"), addresses.Length);
        Array.Copy(addresses, array, addresses.Length);
        return array;
    }

    private static object ToAddressDictionary(string key, object address)
    {
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), Assembly.Value.GetType("Sample.Address"));
        var dictionary = (System.Collections.IDictionary)Activator.CreateInstance(dictionaryType)!;
        dictionary.Add(key, address);
        return dictionary;
    }

    [Fact]
    public void DeepCopy_GeneratedCodeCompiles_WhenModelsUseSupportedPropertyTypes()
    {
        var run = GeneratorHarness.Run(Models);

        Assert.Empty(run.CompilationErrors);
    }

    [Fact]
    public void DeepCopy_ReturnsNewInstanceWithSameScalars_WhenCalled()
    {
        var original = NewPerson();

        var copy = original.DeepCopy();

        Assert.NotSame(original, copy);
        Assert.Equal(1, (int)copy.Id);
        Assert.Equal("Original", (string)copy.Name);
        Assert.Equal(new DateTime(2000, 1, 1), (DateTime)copy.BornAt);
    }

    [Fact]
    public void DeepCopy_CopyKeepsScalars_WhenOriginalScalarsAreMutated()
    {
        var original = NewPerson();
        var copy = original.DeepCopy();

        original.Id = 2;
        original.Name = "Changed";

        Assert.Equal(1, (int)copy.Id);
        Assert.Equal("Original", (string)copy.Name);
    }

    [Fact]
    public void DeepCopy_CopyKeepsNestedObject_WhenOriginalNestedObjectIsMutated()
    {
        var original = NewPerson();
        var copy = original.DeepCopy();

        original.Home.Street = "changed";

        Assert.NotSame(original.Home, copy.Home);
        Assert.Equal("home", (string)copy.Home.Street);
    }

    [Fact]
    public void DeepCopy_CopyKeepsListItems_WhenOriginalListItemIsMutatedOrListGrows()
    {
        var original = NewPerson();
        var copy = original.DeepCopy();

        original.Addresses[0].Street = "changed";
        original.Addresses.Add(NewAddress("second"));

        Assert.NotSame(original.Addresses, copy.Addresses);
        Assert.Equal(1, (int)copy.Addresses.Count);
        Assert.Equal("first", (string)copy.Addresses[0].Street);
    }

    [Fact]
    public void DeepCopy_CopyKeepsArrayItems_WhenOriginalArrayItemIsMutated()
    {
        var original = NewPerson();
        var copy = original.DeepCopy();

        original.History[0].Street = "changed";
        original.Scores[0] = 99;

        Assert.NotSame(original.History, copy.History);
        Assert.Equal("old", (string)copy.History[0].Street);
        Assert.Equal(1, (int)copy.Scores[0]);
    }

    [Fact]
    public void DeepCopy_CopyKeepsSetAndDictionary_WhenOriginalSetAndDictionaryAreMutated()
    {
        var original = NewPerson();
        var copy = original.DeepCopy();

        original.Tags.Add(8);
        original.ByName["work"].Street = "changed";

        Assert.Equal(new HashSet<int> { 7 }, (HashSet<int>)copy.Tags);
        Assert.Equal("office", (string)copy.ByName["work"].Street);
    }

    [Fact]
    public void DeepCopy_CopyKeepsInterfaceTypedCollection_WhenOriginalItemIsMutated()
    {
        var original = NewPerson();
        var copy = original.DeepCopy();

        original.ReadOnlyAddresses[0].Street = "changed";

        Assert.Equal("readonly", (string)copy.ReadOnlyAddresses[0].Street);
    }

    [Fact]
    public void DeepCopy_CopiesInitPropertiesAndNestedObject_WhenTypeIsRecord()
    {
        var original = Assembly.Value.Create("Sample.Settings");
        original.GetType().GetProperty("Theme").SetValue(original, "dark");
        original.GetType().GetProperty("Office").SetValue(original, NewAddress("hq"));

        var copy = original.DeepCopy();
        original.Office.Street = "changed";

        Assert.NotSame(original, copy);
        Assert.Equal("dark", (string)copy.Theme);
        Assert.Equal("hq", (string)copy.Office.Street);
    }

    [Fact]
    public void DeepCopy_DeepCopiesConstructorArguments_WhenTypeHasNoParameterlessConstructor()
    {
        var original = Assembly.Value.Create("Sample.Customer", 5, NewAddress("home"));

        var copy = original.DeepCopy();
        original.Home.Street = "changed";

        Assert.Equal(5, (int)copy.Id);
        Assert.Equal("home", (string)copy.Home.Street);
    }

    [Fact]
    public void DeepCopy_CopiesValues_WhenTypeIsStructOptedInViaInterface()
    {
        object original = Assembly.Value.Create("Sample.Money");
        original.GetType().GetProperty("Amount")!.SetValue(original, 12.5m);
        original.GetType().GetProperty("Currency")!.SetValue(original, "EUR");

        dynamic copy = ((dynamic)original).DeepCopy();

        Assert.Equal(12.5m, (decimal)copy.Amount);
        Assert.Equal("EUR", (string)copy.Currency);
    }
}
