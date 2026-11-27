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
original.Addresses[0].StreetNumber = "123";
Print("After mutating the original", original, copy);

static void Print(string title, HelloWorld original, HelloWorld copy)
{
    Console.WriteLine(title);
    Console.WriteLine($"  Original: Hash={original.GetHashCode()} Id={original.Id} Name={original.Name} Street={original.Addresses[0].StreetNumber}");
    Console.WriteLine($"  Copy:     Hash={copy.GetHashCode()} Id={copy.Id} Name={copy.Name} Street={copy.Addresses[0].StreetNumber}");
}
