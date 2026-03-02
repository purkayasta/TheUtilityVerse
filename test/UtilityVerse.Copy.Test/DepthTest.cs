namespace UtilityVerse.Copy.Test;

/// <summary>
/// A self-referential linked-list node used exclusively for depth-verification tests.
/// With [DeepCopy(2)] the generator bakes maxDepth=2 into the generated DeepCopyInternal call.
///
/// Traversal behaviour — depth is 1-indexed, guard lives at the call site (not the callee):
///   currentDepth 1 → full copy, check (1 < 2) = true  → recurse children at level 2
///   currentDepth 2 → full copy, check (2 < 2) = false → complex properties = null, no recursion
///
/// So a 5-node chain [1→2→3→4→5] becomes [1→2] after DeepCopy().
/// Node 2's Next is null. Nodes 3/4/5 are never visited. No partial objects.
/// </summary>
[DeepCopy(2)]
public partial class DepthNode
{
    public int Value { get; set; }
    public string? Label { get; set; }
    public DepthNode? Next { get; set; }
}

public static class DepthTest
{
    public static void Run()
    {
        Console.WriteLine("=== Depth Verification Tests ===\n");

        TestDepthTruncation();
        TestValueIndependence();
        TestReferenceIndependence();
        TestSingleNodeNoTruncation();
        TestDefaultDepthIsNotZero();

        Console.WriteLine("\n[OK] All depth tests passed!\n");
    }

    /// <summary>
    /// Core depth test: a 5-node chain with [DeepCopy(2)] must produce exactly 2 nodes.
    ///
    /// Guard lives at the call site — the parent checks (currentDepth &lt; maxDepth) before
    /// recursing. At level 2, the check is (2 &lt; 2) = false, so Next is set to null.
    /// Nodes 3/4/5 are never visited. No partial/hollow objects exist in the clone tree.
    /// </summary>
    private static void TestDepthTruncation()
    {
        // Build: 1 → 2 → 3 → 4 → 5
        var n5 = new DepthNode { Value = 5, Label = "L5" };
        var n4 = new DepthNode { Value = 4, Label = "L4", Next = n5 };
        var n3 = new DepthNode { Value = 3, Label = "L3", Next = n4 };
        var n2 = new DepthNode { Value = 2, Label = "L2", Next = n3 };
        var n1 = new DepthNode { Value = 1, Label = "L1", Next = n2 };

        var clone = n1.DeepCopy();

        // Level 1: fully copied
        Verify(clone.Value == 1 && clone.Label == "L1",   "Level 1 (root) fully copied");

        // Level 2: fully copied, but Next = null — clean cut, no partial object
        Verify(clone.Next is { Value: 2, Label: "L2" },   "Level 2 fully copied");
        Verify(clone.Next!.Next is null,                   "Level 2 Next is null — clean cut, nodes 3/4/5 never visited");

        Console.WriteLine("  [PASS] Depth truncation — 5-node chain produces exactly 2 nodes (depth=2, no partial objects)");
    }

    /// <summary>
    /// Mutating cloned objects must not affect the original graph.
    /// </summary>
    private static void TestValueIndependence()
    {
        var root = new DepthNode { Value = 10, Label = "Original" };
        root.Next = new DepthNode { Value = 20, Label = "Child" };

        var clone = root.DeepCopy();

        // Mutate the clone
        clone.Value = 99;
        clone.Label = "Mutated";
        clone.Next!.Value = 88;
        clone.Next.Label = "MutatedChild";

        Verify(root.Value == 10 && root.Label == "Original",
               "Original root unaffected after clone mutation");
        Verify(root.Next!.Value == 20 && root.Next.Label == "Child",
               "Original child unaffected after clone mutation");

        Console.WriteLine("  [PASS] Value independence — mutations on clone do not bleed into original");
    }

    /// <summary>
    /// Cloned nodes must be distinct object instances (not the same references).
    /// </summary>
    private static void TestReferenceIndependence()
    {
        var root = new DepthNode { Value = 1 };
        root.Next = new DepthNode { Value = 2 };

        var clone = root.DeepCopy();

        Verify(!ReferenceEquals(root, clone),           "Root and clone are different instances");
        Verify(!ReferenceEquals(root.Next, clone.Next), "root.Next and clone.Next are different instances");

        Console.WriteLine("  [PASS] Reference independence — cloned nodes are new heap objects");
    }

    /// <summary>
    /// A single isolated node (no Next) should clone cleanly without truncation.
    /// </summary>
    private static void TestSingleNodeNoTruncation()
    {
        var single = new DepthNode { Value = 42, Label = "Lone" };
        var clone = single.DeepCopy();

        Verify(clone.Value == 42 && clone.Label == "Lone", "Single node values cloned");
        Verify(clone.Next is null,                          "Single node Next stays null");
        Verify(!ReferenceEquals(single, clone),             "Single node clone is new instance");

        Console.WriteLine("  [PASS] Single-node clone — no truncation artifacts");
    }

    /// <summary>
    /// Smoke-check that the default [DeepCopy] (depth=5) allows reasonably deep chains.
    /// The existing Parent self-reference class uses default depth — it should survive at
    /// least 5 recursive levels before truncation.
    /// This is a sanity test, not a precision assertion on exact depth count.
    /// </summary>
    private static void TestDefaultDepthIsNotZero()
    {
        // Build a short Parent chain (depth well within default=5)
        var p3 = new Parent { Id = Guid.NewGuid(), Name = "P3" };
        var p2 = new Parent { Id = Guid.NewGuid(), Name = "P2", Child = p3 };
        var p1 = new Parent { Id = Guid.NewGuid(), Name = "P1", Child = p2 };

        var clone = p1.DeepCopy();

        Verify(clone.Name == "P1",              "Default depth: root cloned");
        Verify(clone.Child?.Name == "P2",       "Default depth: level-1 cloned");
        Verify(clone.Child?.Child?.Name == "P3","Default depth: level-2 cloned");

        Console.WriteLine("  [PASS] Default depth (5) — 3-level chain cloned fully");
    }

    private static void Verify(bool condition, string description)
    {
        if (!condition)
            throw new InvalidOperationException($"FAIL — {description}");
    }
}
