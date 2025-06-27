using UtilityVerse.Copy.Test;
using UtilityVerse.Copy;

Console.WriteLine("Starting....");

ManualTesting();

Console.WriteLine(" =====  ");

AutomatedTesting();

Console.WriteLine("Ending....");


static void AutomatedTesting()
{
    var originalObject = new HelloWorld
    {
        Id = 1,
        Name = "Pritom",
        Addresses = new[] { new Address() { StreetNumber = "12" } }
    };

    var clonedObject = originalObject;
    Console.WriteLine("Before Cloning...");
    
    Console.WriteLine(
        $"OriginalObject Id: {originalObject.Id} Name: {originalObject.Name} Addresses: {originalObject.Addresses.First().StreetNumber}");
    Console.WriteLine(
        $"ClonedObject Id: {clonedObject.Id}   Name: {clonedObject.Name}    Addresses: {clonedObject.Addresses.First().StreetNumber}");


    originalObject.Id = 2;
    originalObject.Name = "Purkayasta";
    originalObject.Addresses.First().StreetNumber = "123";


    Console.WriteLine("After Cloning...");
    Console.WriteLine(
        $"OriginalObject Id: {originalObject.Id} Name: {originalObject.Name} Addresses: {originalObject.Addresses.First().StreetNumber}");
    Console.WriteLine(
        $"ClonedObject Id: {clonedObject.Id}   Name: {clonedObject.Name}    Addresses: {clonedObject.Addresses.First().StreetNumber}");
}

static void ManualTesting()
{
    var originalObject = new HelloWorld()
    {
        Id = 1,
        Name = "Pritom",
        Addresses = new[] { new Address() { StreetNumber = "12" } }
    };
    var clonedObject = originalObject.Clone();
    Console.WriteLine("Before Cloning...");
    Console.WriteLine(
        $"OriginalObject Id: {originalObject.Id} Name: {originalObject.Name} Addresses: {originalObject.Addresses.First().StreetNumber}");
    Console.WriteLine(
        $"ClonedObject Id: {clonedObject.Id}   Name: {clonedObject.Name}    Addresses: {clonedObject.Addresses.First().StreetNumber}");


    originalObject.Id = 2;
    originalObject.Name = "Purkayasta";
    originalObject.Addresses.First().StreetNumber = "123";

    Console.WriteLine("After Cloning...");
    Console.WriteLine(
        $"OriginalObject Id: {originalObject.Id} Name: {originalObject.Name} Addresses: {originalObject.Addresses.First().StreetNumber}");
    Console.WriteLine(
        $"ClonedObject Id: {clonedObject.Id}   Name: {clonedObject.Name}    Addresses: {clonedObject.Addresses.First().StreetNumber}");
}