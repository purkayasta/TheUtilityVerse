// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.Copy.UnitTest;

public class ShallowCopyTest
{
    // ─── Primitive & value-type properties ───────────────────────────────────
    // Primitives are always copied regardless of depth.

    [Fact]
    public void ShallowCopy_CopiesCorrectly_WhenGivenPrimitiveValues()
    {
        var original = new Invoice { Id = 42, Number = "INV-001" };

        var clone = original.ShallowCopy();

        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Number, clone.Number);
    }

    [Fact]
    public void ShallowCopy_IsolatesMutation_WhenModifyingClonePrimitives()
    {
        var original = new Invoice { Id = 1, Number = "INV-001" };

        var clone = original.ShallowCopy();
        clone.Id = 999;
        clone.Number = "MUTATED";

        Assert.Equal(1, original.Id);
        Assert.Equal("INV-001", original.Number);
    }

    // ─── Root reference ───────────────────────────────────────────────────────

    [Fact]
    public void ShallowCopy_CreatesNewInstance_WhenCopyingRoot()
    {
        var original = new Invoice { Id = 1 };

        var clone = original.ShallowCopy();

        Assert.False(ReferenceEquals(original, clone));
    }

    // ─── depth=0 (Invoice): nested object is a SHARED reference ──────────────
    // At the depth boundary the generator emits `this.Prop` — it shares the reference.
    // This is the definition of shallow copy for objects beyond the depth limit.

    [Fact]
    public void ShallowCopy_SharesReference_WhenNestedObjectIsAtDepthBoundary()
    {
        var original = new Invoice
        {
            BillingAddress = new ShallowAddress { Street = "42 Oak Ave" }
        };

        var clone = original.ShallowCopy();

        // depth=0: BillingAddress is exactly at the boundary → same reference
        Assert.True(ReferenceEquals(original.BillingAddress, clone.BillingAddress));
    }

    [Fact]
    public void ShallowCopy_AffectsBoth_WhenMutatingSharedNestedObject()
    {
        // Confirms reference sharing: a mutation via either handle is visible to both
        var original = new Invoice
        {
            BillingAddress = new ShallowAddress { Street = "42 Oak Ave" }
        };

        var clone = original.ShallowCopy();
        clone.BillingAddress!.Street = "Mutated via clone";

        Assert.Equal("Mutated via clone", original.BillingAddress!.Street);
    }

    [Fact]
    public void ShallowCopy_CreatesNewInstance_WhenListContainerIsAtDepthBoundary()
    {
        // Collections always get a new container — this is a shallow copy invariant
        var original = new Invoice
        {
            Lines = [new ShallowLineItem { Quantity = 1 }]
        };

        var clone = original.ShallowCopy();

        Assert.NotNull(clone.Lines);
        Assert.False(ReferenceEquals(original.Lines, clone.Lines));
    }

    [Fact]
    public void ShallowCopy_SharesReference_WhenListElementsAreAtDepthBoundary()
    {
        // depth=1: list elements are at the boundary → same object references
        var item = new ShallowLineItem { Quantity = 5, ProductName = "Pen" };
        var original = new Invoice { Lines = [item] };

        var clone = original.ShallowCopy();

        Assert.True(ReferenceEquals(original.Lines![0], clone.Lines![0]));
    }

    // ─── depth=1 (Receipt): nested object is shallow-copied (new instance) ────
    // Within depth the generator emits `ShallowCopyInternal(currentDepth+1, maxDepth)`.

    [Fact]
    public void ShallowCopy_CreatesNewInstance_WhenNestedObjectIsWithinDepth()
    {
        var original = new Receipt
        {
            BillingAddress = new ShallowAddress { Street = "42 Oak Ave", City = "Springfield" }
        };

        var clone = original.ShallowCopy();

        Assert.NotNull(clone.BillingAddress);
        // depth=1: BillingAddress is within depth → a new shallow-copied instance
        Assert.False(ReferenceEquals(original.BillingAddress, clone.BillingAddress));
    }

    [Fact]
    public void ShallowCopy_CopiesPrimitives_WhenNestedObjectIsWithinDepth()
    {
        var original = new Receipt
        {
            BillingAddress = new ShallowAddress { Street = "42 Oak Ave", City = "Springfield" }
        };

        var clone = original.ShallowCopy();

        Assert.Equal("42 Oak Ave", clone.BillingAddress!.Street);
        Assert.Equal("Springfield", clone.BillingAddress.City);
    }

    [Fact]
    public void ShallowCopy_IsolatesMutation_WhenModifyingNestedClonePrimitivesWithinDepth()
    {
        // BillingAddress is a new instance so mutations are isolated
        var original = new Receipt
        {
            BillingAddress = new ShallowAddress { Street = "42 Oak Ave" }
        };

        var clone = original.ShallowCopy();
        clone.BillingAddress!.Street = "999 Mutated Rd";

        Assert.Equal("42 Oak Ave", original.BillingAddress!.Street);
    }

    [Fact]
    public void ShallowCopy_CreatesNewInstances_WhenListElementsAreWithinDepth()
    {
        // depth=1: list elements are within depth → each element is a new shallow copy
        var item = new ShallowLineItem { Quantity = 5, ProductName = "Pen" };
        var original = new Receipt { Lines = [item] };

        var clone = original.ShallowCopy();

        Assert.False(ReferenceEquals(original.Lines![0], clone.Lines![0]));
    }

    [Fact]
    public void ShallowCopy_CopiesPrimitives_WhenListElementPrimitivesAreWithinDepth()
    {
        var original = new Receipt
        {
            Lines = [new ShallowLineItem { Quantity = 3, ProductName = "Stapler" }]
        };

        var clone = original.ShallowCopy();

        Assert.Equal(3, clone.Lines![0].Quantity);
        Assert.Equal("Stapler", clone.Lines[0].ProductName);
    }

    // ─── Null safety ──────────────────────────────────────────────────────────

    [Fact]
    public void ShallowCopy_RemainsNull_WhenCopyingNullNestedObject()
    {
        var original = new Invoice { Id = 1, BillingAddress = null };

        var clone = original.ShallowCopy();

        Assert.Null(clone.BillingAddress);
    }

    [Fact]
    public void ShallowCopy_RemainsNull_WhenCopyingNullList()
    {
        var original = new Invoice { Id = 1, Lines = null };

        var clone = original.ShallowCopy();

        Assert.Null(clone.Lines);
    }

    // ─── Circular Reference Tests ─────────────────────────────────────────────────
    // Tests for handling cyclic/circular object references in shallow copy.

    [Fact]
    public void ShallowCopy_DoesNotCauseStackOverflow_WhenHandlingCircularReference()
    {
        // Create a circular reference: A → B → A
        var alice = new ShallowFriend { Id = 1, Name = "Alice" };
        var bob = new ShallowFriend { Id = 2, Name = "Bob" };
        alice.BestFriend = bob;
        bob.BestFriend = alice;

        // This should NOT cause a stack overflow; depth guard should stop it
        var cloneAlice = alice.ShallowCopy();

        Assert.NotNull(cloneAlice);
        Assert.Equal(1, cloneAlice.Id);
        Assert.Equal("Alice", cloneAlice.Name);
    }

    [Fact]
    public void ShallowCopy_SharesReferences_WhenCircularReferenceIsAtDepthBoundary()
    {
        // ShallowCopy with depth=0: objects at boundary are shared
        var alice = new ShallowFriend { Id = 1, Name = "Alice" };
        var bob = new ShallowFriend { Id = 2, Name = "Bob" };
        alice.BestFriend = bob;
        bob.BestFriend = alice;

        var cloneAlice = alice.ShallowCopy();

        // At depth=0 boundary, BestFriend should be the SHARED original bob
        Assert.True(ReferenceEquals(bob, cloneAlice.BestFriend));
    }

    [Fact]
    public void ShallowCopy_SharesSelfReference_WhenSelfReferenceIsAtDepthBoundary()
    {
        // Self-reference at depth boundary
        var alice = new ShallowFriend { Id = 1, Name = "Alice" };
        alice.BestFriend = alice; // Points to itself

        var cloneAlice = alice.ShallowCopy();

        // At depth=0, self-reference should still point to the original alice
        Assert.True(ReferenceEquals(alice, cloneAlice.BestFriend));
    }

    [Fact]
    public void ShallowCopy_CreatesDistinctRootButSharedReferences_WhenHandlingMutualCircularReference()
    {
        // Mutual reference: A ↔ B, both at depth boundary
        var alice = new ShallowFriend { Id = 1, Name = "Alice" };
        var bob = new ShallowFriend { Id = 2, Name = "Bob" };
        alice.BestFriend = bob;
        bob.BestFriend = alice;

        var cloneAlice = alice.ShallowCopy();

        // Root is a new instance
        Assert.False(ReferenceEquals(alice, cloneAlice));

        // But BestFriend is the shared original bob (at depth boundary)
        Assert.True(ReferenceEquals(bob, cloneAlice.BestFriend));

        // And bob's BestFriend is the shared original alice (unchanged)
        Assert.True(ReferenceEquals(alice, bob.BestFriend));
    }
}
