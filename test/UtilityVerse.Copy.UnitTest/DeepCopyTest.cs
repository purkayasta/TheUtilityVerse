// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.Copy.UnitTest;

public class DeepCopyTest
{
    // ─── Primitive & value-type properties ───────────────────────────────────

    [Fact]
    public void DeepCopy_PrimitiveValues_AreCopiedCorrectly()
    {
        var original = new Order
        {
            Id = 1,
            CustomerName = "Alice",
            Reference = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };

        var clone = original.DeepCopy();

        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.CustomerName, clone.CustomerName);
        Assert.Equal(original.Reference, clone.Reference);
        Assert.Equal(original.CreatedAt, clone.CreatedAt);
    }

    // ─── Reference independence ───────────────────────────────────────────────

    [Fact]
    public void DeepCopy_Root_IsDifferentInstance()
    {
        var original = new Order { Id = 1 };

        var clone = original.DeepCopy();

        Assert.False(ReferenceEquals(original, clone));
    }

    [Fact]
    public void DeepCopy_NestedObject_IsDifferentInstance()
    {
        var original = new Order
        {
            ShippingAddress = new Address { Street = "123 Main St", City = "Springfield" }
        };

        var clone = original.DeepCopy();

        Assert.NotNull(clone.ShippingAddress);
        Assert.False(ReferenceEquals(original.ShippingAddress, clone.ShippingAddress));
    }

    [Fact]
    public void DeepCopy_ListContainer_IsDifferentInstance()
    {
        var original = new Order
        {
            Items = [new LineItem { Quantity = 1, ProductName = "Widget" }]
        };

        var clone = original.DeepCopy();

        Assert.NotNull(clone.Items);
        Assert.False(ReferenceEquals(original.Items, clone.Items));
    }

    [Fact]
    public void DeepCopy_ListElements_AreDifferentInstances()
    {
        var original = new Order
        {
            Items = [new LineItem { Quantity = 2, ProductName = "Widget" }]
        };

        var clone = original.DeepCopy();

        Assert.False(ReferenceEquals(original.Items![0], clone.Items![0]));
    }

    // ─── Mutation isolation ───────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_MutatingClonePrimitive_DoesNotAffectOriginal()
    {
        var original = new Order { Id = 1, CustomerName = "Alice" };

        var clone = original.DeepCopy();
        clone.Id = 999;
        clone.CustomerName = "Mutated";

        Assert.Equal(1, original.Id);
        Assert.Equal("Alice", original.CustomerName);
    }

    [Fact]
    public void DeepCopy_MutatingCloneNestedObject_DoesNotAffectOriginal()
    {
        var original = new Order
        {
            ShippingAddress = new Address { Street = "123 Main St", City = "Springfield" }
        };

        var clone = original.DeepCopy();
        clone.ShippingAddress!.Street = "999 Mutated Rd";
        clone.ShippingAddress.City = "Nowhere";

        Assert.Equal("123 Main St", original.ShippingAddress!.Street);
        Assert.Equal("Springfield", original.ShippingAddress.City);
    }

    [Fact]
    public void DeepCopy_MutatingCloneListElement_DoesNotAffectOriginal()
    {
        var original = new Order
        {
            Items = [new LineItem { Quantity = 2, ProductName = "Widget", UnitPrice = 9.99m }]
        };

        var clone = original.DeepCopy();
        clone.Items![0].ProductName = "Mutated";
        clone.Items[0].Quantity = 999;

        Assert.Equal("Widget", original.Items![0].ProductName);
        Assert.Equal(2, original.Items[0].Quantity);
    }

    // ─── Primitive collection correctness ─────────────────────────────────────

    [Fact]
    public void DeepCopy_PrimitiveList_ContainsCorrectValues()
    {
        var original = new Order { Tags = ["urgent", "fragile"] };

        var clone = original.DeepCopy();

        Assert.Equal(original.Tags, clone.Tags);
        Assert.False(ReferenceEquals(original.Tags, clone.Tags));
    }

    // ─── Null safety ──────────────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_NullNestedObject_RemainsNull()
    {
        var original = new Order { Id = 1, ShippingAddress = null };

        var clone = original.DeepCopy();

        Assert.Null(clone.ShippingAddress);
    }

    [Fact]
    public void DeepCopy_NullList_RemainsNull()
    {
        var original = new Order { Id = 1, Items = null };

        var clone = original.DeepCopy();

        Assert.Null(clone.Items);
    }

    // ─── Depth enforcement ───────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_Chain_FullyCopiesWithinConfiguredDepth()
    {
        // [DeepCopy(2)]: levels 1 and 2 are fully deep-copied
        var n2 = new ChainNode { Value = 2 };
        var n1 = new ChainNode { Value = 1, Next = n2 };

        var clone = n1.DeepCopy();

        Assert.Equal(1, clone.Value);
        Assert.NotNull(clone.Next);
        Assert.Equal(2, clone.Next!.Value);
        Assert.False(ReferenceEquals(n1, clone));
        Assert.False(ReferenceEquals(n2, clone.Next));
    }

    [Fact]
    public void DeepCopy_Chain_TruncatesAtConfiguredDepth()
    {
        // [DeepCopy(2)]: level 2's Next = null; n3 is never copied
        var n3 = new ChainNode { Value = 3 };
        var n2 = new ChainNode { Value = 2, Next = n3 };
        var n1 = new ChainNode { Value = 1, Next = n2 };

        var clone = n1.DeepCopy();

        Assert.Equal(2, clone.Next!.Value);
        Assert.Null(clone.Next.Next);
    }

    [Fact]
    public void DeepCopy_Chain_NoSharedReferences_AtDepthBoundary()
    {
        // n2 is within depth and must be a new instance, not the original
        var n2 = new ChainNode { Value = 2 };
        var n1 = new ChainNode { Value = 1, Next = n2 };

        var clone = n1.DeepCopy();

        Assert.False(ReferenceEquals(n2, clone.Next));
    }

    // ─── Constructor-based initialization ────────────────────────────────────────
    // Tests for record types and classes with explicit constructors.

    [Fact]
    public void DeepCopy_RecordType_AllPropertiesAreCopied()
    {
        var category = new ProductCategory { CategoryId = 10, CategoryName = "Electronics" };
        var tags = new List<string> { "sale", "featured" };
        var original = new ProductRecord(
            Id: 1,
            Name: "Laptop",
            Price: 999.99m,
            Category: category,
            Tags: tags
        );

        var clone = original.DeepCopy();

        // Primitives copied
        Assert.Equal(1, clone.Id);
        Assert.Equal("Laptop", clone.Name);
        Assert.Equal(999.99m, clone.Price);

        // Nested object is a new instance (depth=2, within depth)
        Assert.NotNull(clone.Category);
        Assert.False(ReferenceEquals(original.Category, clone.Category));
        Assert.Equal("Electronics", clone.Category.CategoryName);

        // List is a new instance
        Assert.NotNull(clone.Tags);
        Assert.False(ReferenceEquals(original.Tags, clone.Tags));
        Assert.Equal(2, clone.Tags.Count);
        Assert.Equal("sale", clone.Tags[0]);
    }

    [Fact]
    public void DeepCopy_RecordType_MutatingClone_DoesNotAffectOriginal()
    {
        var category = new ProductCategory { CategoryId = 10, CategoryName = "Electronics" };
        var original = new ProductRecord(1, "Laptop", 999.99m, category, null);

        var clone = original.DeepCopy();
        clone.Category!.CategoryName = "Modified";

        Assert.Equal("Electronics", original.Category!.CategoryName);
    }

    [Fact]
    public void DeepCopy_ConstructorInitializedClass_AllPropertiesAreCopied()
    {
        var details = new OrderDetails { OrderDate = DateTime.UtcNow, ShippingMethod = "Express" };
        var items = new List<OrderItem>
        {
            new OrderItem { Sku = "SKU001", Quantity = 2 },
            new OrderItem { Sku = "SKU002", Quantity = 1 }
        };
        var original = new ConstructorInitializedOrder(42, "John Doe", details, items);

        var clone = original.DeepCopy();

        // Constructor params are copied correctly
        Assert.Equal(42, clone.OrderId);
        Assert.Equal("John Doe", clone.CustomerName);

        // Nested object is a new instance (depth=2, within depth)
        Assert.NotNull(clone.Details);
        Assert.False(ReferenceEquals(original.Details, clone.Details));
        Assert.Equal("Express", clone.Details.ShippingMethod);

        // List and its elements are new instances
        Assert.NotNull(clone.Items);
        Assert.False(ReferenceEquals(original.Items, clone.Items));
        Assert.Equal(2, clone.Items!.Count);
        Assert.False(ReferenceEquals(original.Items![0], clone.Items[0]));
        Assert.Equal("SKU001", clone.Items[0].Sku);
    }

    [Fact]
    public void DeepCopy_ConstructorInitializedClass_NullNestedProperties_RemainNull()
    {
        var original = new ConstructorInitializedOrder(42, "Jane Doe", details: null, items: null);

        var clone = original.DeepCopy();

        Assert.Null(clone.Details);
        Assert.Null(clone.Items);
        Assert.Equal(42, clone.OrderId);
        Assert.Equal("Jane Doe", clone.CustomerName);
    }

    [Fact]
    public void DeepCopy_ConstructorInitializedClass_MutatingNestedClone_DoesNotAffectOriginal()
    {
        var details = new OrderDetails { OrderDate = DateTime.UtcNow, ShippingMethod = "Standard" };
        var original = new ConstructorInitializedOrder(42, "Bob", details, null);

        var clone = original.DeepCopy();
        clone.Details!.ShippingMethod = "Overnight";

        Assert.Equal("Standard", original.Details!.ShippingMethod);
    }

    // ─── Circular Reference Tests ─────────────────────────────────────────────────
    // Tests for handling cyclic/circular object references.

    [Fact]
    public void DeepCopy_CircularReference_DoesNotCauseStackOverflow()
    {
        // Create a circular reference: A → B → A
        var alice = new Friend { Id = 1, Name = "Alice" };
        var bob = new Friend { Id = 2, Name = "Bob" };
        alice.BestFriend = bob;
        bob.BestFriend = alice;

        // This should NOT cause a stack overflow; depth guard should stop it
        var cloneAlice = alice.DeepCopy();

        Assert.NotNull(cloneAlice);
        Assert.Equal(1, cloneAlice.Id);
        Assert.Equal("Alice", cloneAlice.Name);
    }

    [Fact]
    public void DeepCopy_CircularReference_PreservesReferentialIntegrity()
    {
        // Create a circular reference: A → B → A
        var alice = new Friend { Id = 1, Name = "Alice" };
        var bob = new Friend { Id = 2, Name = "Bob" };
        alice.BestFriend = bob;
        bob.BestFriend = alice;

        var cloneAlice = alice.DeepCopy();

        // With referential integrity preserved via identity map, cloneAlice.BestFriend.BestFriend should be cloneAlice
        Assert.NotNull(cloneAlice.BestFriend);
        var cloneBob = cloneAlice.BestFriend;

        // The cycle should be preserved: cloneBob.BestFriend should be cloneAlice (same instance)
        Assert.NotNull(cloneBob.BestFriend);
        Assert.Equal("Alice", cloneBob.BestFriend.Name);
        Assert.True(ReferenceEquals(cloneAlice, cloneBob.BestFriend)); // ✓ Identity map preserves referential equality
    }

    [Fact]
    public void DeepCopy_MutualCircularReference_AllCopiesAreDistinct()
    {
        // Mutual reference: A ↔ B (at same level)
        var alice = new Friend { Id = 1, Name = "Alice" };
        var bob = new Friend { Id = 2, Name = "Bob" };
        alice.BestFriend = bob;
        bob.BestFriend = alice;

        var cloneAlice = alice.DeepCopy();

        // Both should be new instances
        Assert.False(ReferenceEquals(alice, cloneAlice));
        Assert.NotNull(cloneAlice.BestFriend);
        Assert.False(ReferenceEquals(bob, cloneAlice.BestFriend));
    }

    [Fact]
    public void DeepCopy_SelfReference_HandlesSelfPointingObject()
    {
        // Self-reference: A → A
        var alice = new Friend { Id = 1, Name = "Alice" };
        alice.BestFriend = alice; // Points to itself

        var cloneAlice = alice.DeepCopy();

        Assert.NotNull(cloneAlice);
        Assert.NotNull(cloneAlice.BestFriend);
        Assert.False(ReferenceEquals(alice, cloneAlice));

        // With identity map, cloneAlice.BestFriend should be cloneAlice (self-reference preserved)
        Assert.True(ReferenceEquals(cloneAlice, cloneAlice.BestFriend)); // ✓ Self-reference preserved
    }
}
