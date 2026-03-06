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
        // depth=0: the list gets a new container (ToList()), but elements are the original shared references
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

    // ─── HashSet / Queue / Stack (non-primitive elements) ─────────────────────────

    [Fact]
    public void ShallowCopy_CreatesNewContainerAndNewElements_WhenHashSetIsWithinDepth()
    {
        // [ShallowCopy(1)]: elements at depth 1 → each gets a new ShallowCopy'd instance
        var item = new CollectionItem { Id = 1, Name = "A" };
        var original = new ShallowCollectionHolder { ItemSet = [item] };

        var clone = original.ShallowCopy();

        Assert.NotNull(clone.ItemSet);
        Assert.False(ReferenceEquals(original.ItemSet, clone.ItemSet));
        Assert.False(ReferenceEquals(item, clone.ItemSet!.Single()));
        Assert.Equal(1, clone.ItemSet.Single().Id);
    }

    [Fact]
    public void ShallowCopy_CreatesNewContainerWithSharedElements_WhenHashSetIsAtDepthBoundary()
    {
        // [ShallowCopy(0)]: 0 < 0 → false → new HashSet<T>(this.ItemSet)
        // Same pattern as List: a new container is always created, but elements are shared references.
        var item = new CollectionItem { Id = 1, Name = "A" };
        var original = new ShallowCollectionBoundary { ItemSet = [item] };

        var clone = original.ShallowCopy();

        Assert.False(ReferenceEquals(original.ItemSet, clone.ItemSet)); // new container
        Assert.True(ReferenceEquals(item, clone.ItemSet!.Single()));     // shared elements
    }

    [Fact]
    public void ShallowCopy_CreatesNewContainerAndNewElements_WhenQueueIsWithinDepth()
    {
        var item = new CollectionItem { Id = 2, Name = "B" };
        var original = new ShallowCollectionHolder { ItemQueue = new Queue<CollectionItem>([item]) };

        var clone = original.ShallowCopy();

        Assert.NotNull(clone.ItemQueue);
        Assert.False(ReferenceEquals(original.ItemQueue, clone.ItemQueue));
        Assert.False(ReferenceEquals(item, clone.ItemQueue!.Peek()));
        Assert.Equal(2, clone.ItemQueue.Peek().Id);
    }

    [Fact]
    public void ShallowCopy_CreatesNewContainerWithSharedElements_WhenQueueIsAtDepthBoundary()
    {
        // [ShallowCopy(0)]: 0 < 0 → false → new Queue<T>(this.ItemQueue) — new container, shared elements
        var item = new CollectionItem { Id = 2, Name = "B" };
        var original = new ShallowCollectionBoundary { ItemQueue = new Queue<CollectionItem>([item]) };

        var clone = original.ShallowCopy();

        Assert.False(ReferenceEquals(original.ItemQueue, clone.ItemQueue)); // new container
        Assert.True(ReferenceEquals(item, clone.ItemQueue!.Peek()));         // shared elements
    }

    [Fact]
    public void ShallowCopy_CreatesNewContainerAndNewElements_WhenStackIsWithinDepth()
    {
        var item = new CollectionItem { Id = 3, Name = "C" };
        var original = new ShallowCollectionHolder { ItemStack = new Stack<CollectionItem>([item]) };

        var clone = original.ShallowCopy();

        Assert.NotNull(clone.ItemStack);
        Assert.False(ReferenceEquals(original.ItemStack, clone.ItemStack));
        Assert.False(ReferenceEquals(item, clone.ItemStack!.Peek()));
        Assert.Equal(3, clone.ItemStack.Peek().Id);
    }

    [Fact]
    public void ShallowCopy_CreatesNewContainerWithSharedElements_WhenStackIsAtDepthBoundary()
    {
        // [ShallowCopy(0)]: 0 < 0 → false → new Stack<T>(this.ItemStack) — new container, shared elements
        var item = new CollectionItem { Id = 3, Name = "C" };
        var original = new ShallowCollectionBoundary { ItemStack = new Stack<CollectionItem>([item]) };

        var clone = original.ShallowCopy();

        Assert.False(ReferenceEquals(original.ItemStack, clone.ItemStack)); // new container
        Assert.True(ReferenceEquals(item, clone.ItemStack!.Peek()));         // shared elements
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

    // ─── Struct Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void ShallowCopy_CopiesCorrectly_WhenGivenStructWithPrimitiveValues()
    {
        var original = new Money { Amount = 75, Currency = "GBP" };

        var clone = original.ShallowCopy();

        Assert.Equal(75, clone.Amount);
        Assert.Equal("GBP", clone.Currency);
    }

    [Fact]
    public void ShallowCopy_ProducesIndependentValueCopy_WhenCopyingStruct()
    {
        // Struct is a value type; ShallowCopy returns a copy — mutations are isolated by design.
        var original = new Money { Amount = 200, Currency = "JPY" };

        var clone = original.ShallowCopy();
        clone.Amount = 0;
        clone.Currency = "XXX";

        Assert.Equal(200, original.Amount);
        Assert.Equal("JPY", original.Currency);
    }

    [Fact]
    public void ShallowCopy_SharesNestedReference_WhenStructHasReferenceProperty()
    {
        // Struct ShallowCopy returns `this` (value-type copy). Reference-type fields are always shared,
        // regardless of depth — this IS the correct shallow copy semantic for value types.
        var address = new ShallowAddress { Street = "3 Pine Rd", City = "Gotham" };
        var original = new ShallowPackageDimensions { Weight = 3.0, ReturnAddress = address };

        var clone = original.ShallowCopy();

        // struct is copied by value: Weight is an independent copy
        Assert.Equal(3.0, clone.Weight);

        // reference-type field is the SAME reference (shallow semantics for struct)
        Assert.NotNull(clone.ReturnAddress);
        Assert.True(ReferenceEquals(original.ReturnAddress, clone.ReturnAddress));
        Assert.Equal("3 Pine Rd", clone.ReturnAddress!.Street);
    }

    // ─── Record Struct Tests ──────────────────────────────────────────────────────

    [Fact]
    public void ShallowCopy_CopiesCorrectly_WhenGivenRecordStruct()
    {
        var original = new Point(X: 4, Y: 8);

        var clone = original.ShallowCopy();

        Assert.Equal(4, clone.X);
        Assert.Equal(8, clone.Y);
    }

    [Fact]
    public void ShallowCopy_CopiesAllValues_WhenRecordStructHasStringProperty()
    {
        var original = new LabeledPoint(X: 0, Y: 0, Label: "Zero");

        var clone = original.ShallowCopy();

        Assert.Equal(0, clone.X);
        Assert.Equal(0, clone.Y);
        Assert.Equal("Zero", clone.Label);
    }

    [Fact]
    public void ShallowCopy_ProducesIndependentCopy_WhenCopyingRecordStruct()
    {
        var original = new LabeledPoint(X: 1, Y: 1, Label: "One");

        var clone = original.ShallowCopy();
        clone = clone with { Label = "Mutated" };

        // original is unaffected — record struct is a value type
        Assert.Equal("One", original.Label);
    }
}
