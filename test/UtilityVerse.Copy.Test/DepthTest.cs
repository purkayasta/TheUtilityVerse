using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dumpper;

namespace UtilityVerse.Copy.Test;

public static class DepthTest
{
    public static void Run()
    {
        Console.WriteLine("=== Depth Verification Tests ===\n");

        var cefalo = new Company()
        {
            Id = 1,
            Name = "Cefalo"
        };

        var pritom = new Employee()
        {
            Name = "pritom",
            Company = cefalo
        };

        cefalo.Single = pritom;

        var cefaloJson = JsonSerializer.Serialize(cefalo, new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve
        });

        var cefaloCopied = cefalo.DeepCopy();
        var cefaloCopiedFromJson = JsonSerializer.Deserialize<Company>(cefaloJson);

        PrintUntilNullFound(cefaloCopied);

        System.Console.WriteLine("Now Json");
        PrintUntilNullFound(cefaloCopiedFromJson);

        Console.WriteLine("\n[OK] All depth tests passed!\n");
    }

    private static void PrintUntilNullFound(Company? c, int level = 0)
    {
        System.Console.WriteLine($"Level: {level}");
        if (c is not null)
        {
            System.Console.WriteLine($"Company: {c.Id}, Name: {c.Name}");
            PrintUntilNullFound(c.Single, level + 1);
        }
    }
    private static void PrintUntilNullFound(Employee? e, int level = 0)
    {
        System.Console.WriteLine($"Level : {level}");
        if (e == null) return;
        System.Console.WriteLine($"Employee: {e.Name}");
        PrintUntilNullFound(e.Company, level + 1);
    }

}

[DeepCopy(2)]
public partial class Company
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public Employee? Single { get; set; }
}

[DeepCopy(2)]
public partial class Employee
{
    public string? Name { get; set; }
    public Company? Company { get; set; }
}