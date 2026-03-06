// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.Copy.UnitTest;

public class DeepCopyTest
{
    // ─── Primitive & value-type properties ───────────────────────────────────

    [Fact]
    public void DeepCopy_CopiesCorrectly_WhenGivenPrimitiveValues()
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

    // ─── Reference independence ───────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_CreatesNewInstance_WhenCopyingRoot()
    {
        var original = new Order { Id = 1 };

        var clone = original.DeepCopy();

        Assert.False(ReferenceEquals(original, clone));
    }

    [Fact]
    public void DeepCopy_CreatesNewInstance_WhenCopyingNestedObject()
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
    public void DeepCopy_CreatesNewInstance_WhenCopyingListContainer()
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
    public void DeepCopy_CreatesNewInstances_WhenCopyingListElements()
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
    public void DeepCopy_IsolatesMutation_WhenModifyingClonePrimitives()
    {
        var original = new Order { Id = 1, CustomerName = "Alice" };

        var clone = original.DeepCopy();
        clone.Id = 999;
        clone.CustomerName = "Mutated";

        Assert.Equal(1, original.Id);
        Assert.Equal("Alice", original.CustomerName);
    }

    [Fact]
    public void DeepCopy_IsolatesMutation_WhenModifyingCloneNestedObject()
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
    public void DeepCopy_IsolatesMutation_WhenModifyingCloneListElement()
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
    public void DeepCopy_ContainsCorrectValues_WhenCopyingPrimitiveList()
    {
        var original = new Order { Tags = ["urgent", "fragile"] };

        var clone = original.DeepCopy();

        Assert.Equal(original.Tags, clone.Tags);
        Assert.False(ReferenceEquals(original.Tags, clone.Tags));
    }

    // ─── Null safety ──────────────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_RemainsNull_WhenCopyingNullNestedObject()
    {
        var original = new Order { Id = 1, ShippingAddress = null };

        var clone = original.DeepCopy();

        Assert.Null(clone.ShippingAddress);
    }

    [Fact]
    public void DeepCopy_RemainsNull_WhenCopyingNullList()
    {
        var original = new Order { Id = 1, Items = null };

        var clone = original.DeepCopy();

        Assert.Null(clone.Items);
    }

    // ─── Depth enforcement ───────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_FullyCopies_WhenChainIsWithinConfiguredDepth()
    {
        // [DeepCopy(2)]: currentDepth goes 0, 1; at currentDepth=2, check fails, recurse stops
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
    public void DeepCopy_Truncates_WhenChainExceedsConfiguredDepth()
    {
        // [DeepCopy(2)]: currentDepth 0, 1 are copied; at currentDepth=2, Next=null; n3 never copied
        var n3 = new ChainNode { Value = 3 };
        var n2 = new ChainNode { Value = 2, Next = n3 };
        var n1 = new ChainNode { Value = 1, Next = n2 };

        var clone = n1.DeepCopy();

        Assert.Equal(2, clone.Next!.Value);
        Assert.Null(clone.Next.Next);
    }

    [Fact]
    public void DeepCopy_CreatesNewInstance_WhenNestedObjectIsWithinDepth()
    {
        // n2 is within depth (currentDepth=1) and must be a new instance
        var n2 = new ChainNode { Value = 2 };
        var n1 = new ChainNode { Value = 1, Next = n2 };

        var clone = n1.DeepCopy();

        Assert.False(ReferenceEquals(n2, clone.Next));
    }

    // ─── Depth edge cases ─────────────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_ReturnsNull_WhenDepthIsZero()
    {
        // [DeepCopy(0)]: DeepCopyInternal(0, 0, map) → 0 >= 0 → return null
        // No exception should be thrown; caller receives null.
        var original = new ZeroDepthItem { Id = 1, Name = "test" };

        ZeroDepthItem? clone = original.DeepCopy();

        Assert.Null(clone);
    }

    [Fact]
    public void DeepCopy_WorksCorrectly_WhenDepthExceedsPreviousMaximum()
    {
        // [DeepCopy(15)] previously exceeded MAX_DEPTH=10 and would have thrown.
        // Now it must compile and copy correctly for chains shorter than 15.
        var n3 = new HighDepthNode { Value = 3 };
        var n2 = new HighDepthNode { Value = 2, Next = n3 };
        var n1 = new HighDepthNode { Value = 1, Next = n2 };

        var clone = n1.DeepCopy();

        Assert.NotNull(clone);
        Assert.Equal(1, clone.Value);
        Assert.NotNull(clone.Next);
        Assert.Equal(2, clone.Next!.Value);
        Assert.NotNull(clone.Next.Next);
        Assert.Equal(3, clone.Next.Next!.Value);
        Assert.False(ReferenceEquals(n1, clone));
        Assert.False(ReferenceEquals(n2, clone.Next));
        Assert.False(ReferenceEquals(n3, clone.Next.Next));
    }

    // ─── Constructor-based initialization ────────────────────────────────────────
    // Tests for record types and classes with explicit constructors.

    [Fact]
    public void DeepCopy_CopiesAllProperties_WhenCopyingRecordType()
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
    public void DeepCopy_IsolatesMutation_WhenModifyingCloneRecordType()
    {
        var category = new ProductCategory { CategoryId = 10, CategoryName = "Electronics" };
        var original = new ProductRecord(1, "Laptop", 999.99m, category, null);

        var clone = original.DeepCopy();
        clone.Category!.CategoryName = "Modified";

        Assert.Equal("Electronics", original.Category!.CategoryName);
    }

    [Fact]
    public void DeepCopy_CopiesAllProperties_WhenCopyingConstructorInitializedClass()
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
    public void DeepCopy_PreservesNull_WhenConstructorInitializedClassHasNullNestedProperties()
    {
        var original = new ConstructorInitializedOrder(42, "Jane Doe", details: null, items: null);

        var clone = original.DeepCopy();

        Assert.Null(clone.Details);
        Assert.Null(clone.Items);
        Assert.Equal(42, clone.OrderId);
        Assert.Equal("Jane Doe", clone.CustomerName);
    }

    [Fact]
    public void DeepCopy_IsolatesMutation_WhenModifyingCloneConstructorInitializedNestedObject()
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
    public void DeepCopy_DoesNotCauseStackOverflow_WhenHandlingCircularReference()
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
    public void DeepCopy_PreservesReferentialIntegrity_WhenHandlingCircularReference()
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
    public void DeepCopy_CreatesDistinctCopies_WhenHandlingMutualCircularReference()
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
    public void DeepCopy_PreservesSelfReference_WhenHandlingSelfPointingObject()
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

    // ─── Struct Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_CopiesCorrectly_WhenGivenStructWithPrimitiveValues()
    {
        var original = new Money { Amount = 100, Currency = "USD" };

        var clone = original.DeepCopy();

        Assert.Equal(100, clone.Amount);
        Assert.Equal("USD", clone.Currency);
    }

    [Fact]
    public void DeepCopy_ProducesIndependentCopy_WhenCopyingStruct()
    {
        // Structs are value types; the copy is always independent by nature.
        // Verify that DeepCopy() produces a logically distinct copy (not the same boxed reference).
        var original = new Money { Amount = 50, Currency = "EUR" };

        var clone = original.DeepCopy();
        clone.Amount = 999;
        clone.Currency = "XXX";

        Assert.Equal(50, original.Amount);
        Assert.Equal("EUR", original.Currency);
    }

    [Fact]
    public void DeepCopy_CopiesNestedClassReference_WhenStructHasReferenceProperty()
    {
        // PackageDimensions has depth=1: the nested Address gets deep-copied as a new instance.
        var original = new PackageDimensions
        {
            Weight = 2.5,
            ReturnAddress = new Address { Street = "10 Elm St", City = "Springfield" }
        };

        var clone = original.DeepCopy();

        Assert.NotNull(clone.ReturnAddress);
        Assert.False(ReferenceEquals(original.ReturnAddress, clone.ReturnAddress));
        Assert.Equal("10 Elm St", clone.ReturnAddress!.Street);
        Assert.Equal("Springfield", clone.ReturnAddress.City);
    }

    [Fact]
    public void DeepCopy_IsolatesNestedMutation_WhenStructHasReferenceProperty()
    {
        var original = new PackageDimensions
        {
            Weight = 1.0,
            ReturnAddress = new Address { Street = "10 Elm St" }
        };

        var clone = original.DeepCopy();
        clone.ReturnAddress!.Street = "Mutated";

        Assert.Equal("10 Elm St", original.ReturnAddress!.Street);
    }

    // ─── Record Struct Tests ──────────────────────────────────────────────────────

    [Fact]
    public void DeepCopy_CopiesCorrectly_WhenGivenRecordStruct()
    {
        var original = new Point(X: 3, Y: 7);

        var clone = original.DeepCopy();

        Assert.Equal(3, clone.X);
        Assert.Equal(7, clone.Y);
    }

    [Fact]
    public void DeepCopy_CopiesAllValues_WhenRecordStructHasStringProperty()
    {
        var original = new LabeledPoint(X: 1, Y: 2, Label: "Origin");

        var clone = original.DeepCopy();

        Assert.Equal(1, clone.X);
        Assert.Equal(2, clone.Y);
        Assert.Equal("Origin", clone.Label);
    }

    [Fact]
    public void DeepCopy_ProducesIndependentCopy_WhenCopyingRecordStruct()
    {
        // Record structs are value types; mutations to the clone do not affect the original.
        var original = new LabeledPoint(X: 5, Y: 10, Label: "A");

        var clone = original.DeepCopy();
        clone = clone with { Label = "Mutated" };

        Assert.Equal("A", original.Label);
    }

    // ─── Additional Record Class Tests ───────────────────────────────────────────

    [Fact]
    public void DeepCopy_CopiesAllProperties_WhenCopyingRecordClassWithNullNested()
    {
        var original = new CustomerRecord(Id: 1, FullName: "Alice", HomeAddress: null, Emails: null);

        var clone = original.DeepCopy();

        Assert.Equal(1, clone.Id);
        Assert.Equal("Alice", clone.FullName);
        Assert.Null(clone.HomeAddress);
        Assert.Null(clone.Emails);
    }

    [Fact]
    public void DeepCopy_CreatesNewInstance_WhenCopyingRecordClassWithNestedReference()
    {
        var original = new CustomerRecord(
            Id: 2,
            FullName: "Bob",
            HomeAddress: new Address { Street = "5 Main St", City = "Shelbyville" },
            Emails: ["bob@example.com"]);

        var clone = original.DeepCopy();

        Assert.False(ReferenceEquals(original, clone));
        Assert.False(ReferenceEquals(original.HomeAddress, clone.HomeAddress));
        Assert.False(ReferenceEquals(original.Emails, clone.Emails));
        Assert.Equal("5 Main St", clone.HomeAddress!.Street);
        Assert.Equal("bob@example.com", clone.Emails![0]);
    }

    [Fact]
    public void DeepCopy_IsolatesMutation_WhenModifyingCloneRecordClassNestedObject()
    {
        var original = new CustomerRecord(
            Id: 3,
            FullName: "Carol",
            HomeAddress: new Address { Street = "7 Oak Ave" },
            Emails: null);

        var clone = original.DeepCopy();
        clone.HomeAddress!.Street = "Mutated";

        Assert.Equal("7 Oak Ave", original.HomeAddress!.Street);
    }
}
