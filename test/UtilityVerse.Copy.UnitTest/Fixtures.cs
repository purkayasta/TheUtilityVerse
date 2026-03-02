// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.Copy.UnitTest.Fixtures;

// ─── DeepCopy fixtures ───────────────────────────────────────────────────────

/// <summary>
/// Root entity for DeepCopy tests. depth=3 so we can verify 3 full levels and
/// confirm the 4th level (if present) is truncated.
/// </summary>
[DeepCopy(3)]
public partial class Order
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
    public Guid Reference { get; set; }
    public DateTime CreatedAt { get; set; }
    public Address? ShippingAddress { get; set; }
    public List<LineItem>? Items { get; set; }
    public List<string>? Tags { get; set; }
}

[DeepCopy]
public partial class Address
{
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}

[DeepCopy]
public partial class LineItem
{
    public int Quantity { get; set; }
    public string? ProductName { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Self-referential node for depth-limit tests.
/// depth=2: levels 1 and 2 are fully copied, level 2's Next = null.
/// </summary>
[DeepCopy(2)]
public partial class ChainNode
{
    public int Value { get; set; }
    public ChainNode? Next { get; set; }
}

// ─── ShallowCopy fixtures ─────────────────────────────────────────────────────

/// <summary>
/// depth=1: classic shallow copy — BillingAddress and list elements are SHARED references.
/// </summary>
[ShallowCopy(1)]
public partial class Invoice
{
    public int Id { get; set; }
    public string? Number { get; set; }
    public ShallowAddress? BillingAddress { get; set; }
    public List<ShallowLineItem>? Lines { get; set; }
}

/// <summary>
/// depth=2: BillingAddress is shallow-copied (new instance), its own complex
/// properties would be shared references at depth 3.
/// </summary>
[ShallowCopy(2)]
public partial class Receipt
{
    public int Id { get; set; }
    public string? Number { get; set; }
    public ShallowAddress? BillingAddress { get; set; }
    public List<ShallowLineItem>? Lines { get; set; }
}

[ShallowCopy]
public partial class ShallowAddress
{
    public string? Street { get; set; }
    public string? City { get; set; }
}

[ShallowCopy]
public partial class ShallowLineItem
{
    public int Quantity { get; set; }
    public string? ProductName { get; set; }
}

// ─── Constructor-based initialization (record and class with ctor params) ──────

/// <summary>
/// Record type for DeepCopy — automatically generates a constructor with all properties.
/// Verifies that constructor-initialized properties are deep-copied correctly.
/// </summary>
[DeepCopy(2)]
public partial record ProductRecord(
    int Id,
    string Name,
    decimal Price,
    ProductCategory? Category,
    List<string>? Tags);

/// <summary>
/// Nested category used in ProductRecord.
/// </summary>
[DeepCopy]
public partial class ProductCategory
{
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
}

/// <summary>
/// Class with explicit constructor taking parameters for all properties.
/// Verifies that non-parameterless-ctor types copy correctly.
/// </summary>
[DeepCopy(2)]
public partial class ConstructorInitializedOrder
{
    public int OrderId { get; private set; }
    public string CustomerName { get; private set; }
    public OrderDetails? Details { get; set; }
    public List<OrderItem>? Items { get; set; }

    public ConstructorInitializedOrder(int orderId, string customerName, OrderDetails? details = null, List<OrderItem>? items = null)
    {
        OrderId = orderId;
        CustomerName = customerName;
        Details = details;
        Items = items;
    }
}

[DeepCopy]
public partial class OrderDetails
{
    public DateTime OrderDate { get; set; }
    public string? ShippingMethod { get; set; }
}

[DeepCopy]
public partial class OrderItem
{
    public string? Sku { get; set; }
    public int Quantity { get; set; }
}

// ─── Circular Reference Test Fixtures ────────────────────────────────────────

/// <summary>
/// Deep copy fixture for testing circular/cyclic references.
/// With referential integrity maintained, A.Friend.Friend == A after DeepCopy.
/// depth=3 allows us to test cycles within the depth boundary.
/// </summary>
[DeepCopy(3)]
public partial class Friend
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public Friend? BestFriend { get; set; }
}

/// <summary>
/// Shallow copy fixture for testing circular references.
/// With depth=1, BestFriend is shared; but cycles should still be preserved
/// where objects reference each other.
/// </summary>
[ShallowCopy(1)]
public partial class ShallowFriend
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ShallowFriend? BestFriend { get; set; }
}
