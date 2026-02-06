namespace UtilityVerse.Copy.Test;

public class InfinityLoop
{
    public static void Run()
    {
        Console.WriteLine("InfinityLoop test executed.");
        var parent = CreateParentWithChild();
        System.Console.WriteLine("Created Parent with Child.");

        System.Console.WriteLine("Established circular references.");

        var clonedResult = parent.DeepCopy();
        Console.WriteLine("Cloning completed successfully.");

        // clonedResult.Dump();
        Console.WriteLine($"Depth of references after DeepCopy: {CalculateDepth(clonedResult)}");
    }

    public static Parent CreateParentWithChild()
    {
        var parent = new Parent
        {
            Id = Guid.CreateVersion7(),
            Name = "Parent"
        };

        var child = new Parent
        {
            Id = Guid.CreateVersion7(),
            Name = "Child"
        };

        parent.Child = child;
        child.Child = parent;

        return parent;
    }
    private static int CalculateDepth(Parent parent)
    {
        int depth = 1;
        Parent? current = parent;

        while (current?.Child is not null)
        {
            depth++;
            current = current.Child;

            // Stop if we detect a circular reference (same object being visited again)
            if (ReferenceEquals(parent, current))
            {
                break;
            }
        }

        System.Console.WriteLine($"Calculated depth: {depth}");

        return depth;
    }
    // private static int CalculateDepth(Parent parent)
    // {
    //     int level = 1;

    //     Parent? currentParent = parent;

    //     while (currentParent?.Child?.Parent != null)
    //     {
    //         if (ReferenceEquals(currentParent, currentParent.Child.Parent))
    //         {
    //             break;
    //         }
    //         level++;
    //         currentParent = currentParent.Child.Parent;
    //     }

    //     return level;
    // }
}
