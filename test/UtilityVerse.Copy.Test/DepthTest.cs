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

        // The copy is a faithful deep copy: a brand-new graph...
        Console.WriteLine($"Root is a new instance: {!ReferenceEquals(cefalo, cefaloCopied)}");

        // ...but the circular reference is preserved via the identity map:
        // copiedCompany.Single.Company should point back to copiedCompany.
        var cyclePreserved = cefaloCopied is not null
            && ReferenceEquals(cefaloCopied, cefaloCopied.Single?.Company);
        Console.WriteLine($"Cycle preserved (Single.Company == root): {cyclePreserved}");

        PrintGraph(cefaloCopied);

        System.Console.WriteLine("Now Json");
        PrintGraph(cefaloCopiedFromJson);

        Console.WriteLine("\n[OK] All depth tests passed!\n");
    }

    // Cycle-aware printer: stops when it revisits an already-seen instance so a
    // (correctly) circular copy does not cause infinite recursion.
    private static void PrintGraph(Company? c)
    {
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var level = 0;
        while (c is not null && seen.Add(c))
        {
            System.Console.WriteLine($"Level {level}: Company {c.Id}, Name: {c.Name}");
            var e = c.Single;
            if (e is null || !seen.Add(e)) break;
            System.Console.WriteLine($"Level {level + 1}: Employee {e.Name}");
            c = e.Company;
            level += 2;
        }

        if (c is not null)
            System.Console.WriteLine($"[cycle] reached an already-copied instance — stopping.");
    }

}

[DeepCopy(3)]
public partial class Company
{
    public int Id { get; set; }
    public string? Name { get; set; }

    public Employee? Single { get; set; }
}

[DeepCopy(3)]
public partial class Employee
{
    public string? Name { get; set; }
    public Company? Company { get; set; }
}