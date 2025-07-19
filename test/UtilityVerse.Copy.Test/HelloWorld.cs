namespace UtilityVerse.Copy.Test;

[DeepCopy]
public partial class HelloWorld
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public DateTime CreatedAt { get; set; }
	public IEnumerable<Address> Addresses { get; set; } = [];

	public HelloWorld Clone()
	{
		return new HelloWorld
		{
			Id = Id,
			Name = Name,
			Addresses = Addresses.Select(x => x.Clone()).ToList()
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


[DeepCopy]
public partial class Hello
{
	public Guid Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public Status Status { get; set; }
	public DateOnly CreatedDateOnly { get; set; }

	public Hello2[]? Hello2s { get; set; }
}

[DeepCopy]
public partial record Hello2(Guid Id);

public enum Status
{
	Ready,
	Finished
}