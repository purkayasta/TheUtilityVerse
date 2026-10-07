using UtilityVerse.Copy.Test;

var original = new HelloWorld
{
    Id = 1,
    Name = "Pritom",
    Addresses = [new Address { StreetNumber = "12" }]
};

var copy = original.DeepCopy();
Print("Before mutation", original, copy);

original.Id = 2;
original.Name = "Purkayasta";
original.Addresses.First().StreetNumber = "123";
Print("After mutating the original", original, copy);

static void Print(string title, HelloWorld original, HelloWorld copy)
{
    Console.WriteLine(title);
    Console.WriteLine($"  Original: Id={original.Id} Name={original.Name} Street={original.Addresses.First().StreetNumber}");
    Console.WriteLine($"  Copy:     Id={copy.Id} Name={copy.Name} Street={copy.Addresses.First().StreetNumber}");
}
