namespace UtilityVerse.Copy.Test;

[DeepCopy]
public partial class Parent
{
	public Guid Id { get; set; }
	public string? Name { get; set; }

	public Parent? Child { get; set; }
}

[DeepCopy]
public partial class Child
{
	public Guid Id { get; set; }
	public string? Name { get; set; }
	public Parent? Parent { get; set; }
}
