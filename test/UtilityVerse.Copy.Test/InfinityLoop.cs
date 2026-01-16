using System;
using Dumpper;
using Spectre.Console;

namespace UtilityVerse.Copy.Test;

public class InfinityLoop
{
    public static void Run()
    {
        Console.WriteLine("InfinityLoop test executed.");
        var (parent, child) = CreateParentWithChild();
        System.Console.WriteLine("Created Parent with Child.");

        
        System.Console.WriteLine("Established circular references.");

        var clonedResult = parent.DeepCopy();
        Console.WriteLine("Cloning completed successfully.");

        // clonedResult.Dump();
        Console.WriteLine($"Depth of references after DeepCopy: {CalculateDepth(clonedResult)}");
    }

    public static (Parent, Child) CreateParentWithChild()
    {
        var parent = new Parent
        {
            Id = Guid.CreateVersion7(),
            Name = "Parent"
        };

        var child = new Child
        {
            Id = Guid.CreateVersion7(),
            Name = "Child",
            Parent = parent
        };

        parent.Child = child;

        return (parent, child);
    }

    private static int CalculateDepth(Parent parent)
    {
        int level = 1;

        Parent? currentParent = parent;
        
        while (currentParent is not null && currentParent.Child is not null)
        {
            level++;
            currentParent = currentParent.Child.Parent;
        }

        return level;
    }
}
