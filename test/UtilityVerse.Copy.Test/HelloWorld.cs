using UtilityVerse.Copy.Attributes;

[DeepCopy]
public partial class HelloWorld
{
    public int Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<Address> Addresses { get; set; }

    public HelloWorld Clone()
    {
        return new HelloWorld
        {
            Id = Id,
            Name = Name,
            Addresses = Addresses.Select(x=> x.Clone()).ToList()
        };
    }
}

[DeepCopy]
public partial class Address
{
    public string? StreetNumber { get; set; }

    public Address Clone()
    {
        return new Address
        {
            StreetNumber = StreetNumber
        };
    }
}