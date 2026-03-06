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
/// depth=2: levels 0 and 1 are fully copied, level 2's Next = null.
/// </summary>
[DeepCopy(2)]
public partial class ChainNode
{
    public int Value { get; set; }
    public ChainNode? Next { get; set; }
}

// ─── ShallowCopy fixtures ─────────────────────────────────────────────────────

/// <summary>
/// depth=0: classic shallow copy — BillingAddress and list elements are SHARED references.
/// </summary>
[ShallowCopy(0)]
public partial class Invoice
{
    public int Id { get; set; }
    public string? Number { get; set; }
    public ShallowAddress? BillingAddress { get; set; }
    public List<ShallowLineItem>? Lines { get; set; }
}

/// <summary>
/// depth=1: BillingAddress is shallow-copied (new instance), its own complex
/// properties would be shared references at depth 2.
/// </summary>
[ShallowCopy(1)]
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

// ─── DeepCopy(0) fixture — verifies no exception and returns null ─────────────

/// <summary>
/// depth=0: DeepCopyInternal is called with currentDepth=0, maxDepth=0.
/// Guard fires immediately (0 >= 0) and returns null. No exception thrown.
/// </summary>
[DeepCopy(0)]
public partial class ZeroDepthItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// ─── High-depth fixture — verifies no upper-bound limit ───────────────────────

/// <summary>
/// depth=15 (previously capped at 10). Verifies the generator accepts any
/// non-negative depth and the copy works correctly within the chain length.
/// </summary>
[DeepCopy(15)]
public partial class HighDepthNode
{
    public int Value { get; set; }
    public HighDepthNode? Next { get; set; }
}

// ─── ShallowCopy collection fixtures ─────────────────────────────────────────

[ShallowCopy]
public partial class CollectionItem
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// depth=1: HashSet/Queue/Stack elements are within depth and get shallow-copied
/// (new element instances). The containers themselves are also new.
/// </summary>
[ShallowCopy(1)]
public partial class ShallowCollectionHolder
{
    public HashSet<CollectionItem>? ItemSet { get; set; }
    public Queue<CollectionItem>? ItemQueue { get; set; }
    public Stack<CollectionItem>? ItemStack { get; set; }
}

/// <summary>
/// depth=0: HashSet/Queue/Stack are at the boundary — the original collection
/// references are returned as-is (shared references).
/// </summary>
[ShallowCopy(0)]
public partial class ShallowCollectionBoundary
{
    public HashSet<CollectionItem>? ItemSet { get; set; }
    public Queue<CollectionItem>? ItemQueue { get; set; }
    public Stack<CollectionItem>? ItemStack { get; set; }
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
/// With depth=0, BestFriend is shared; but cycles should still be preserved
/// where objects reference each other.
/// </summary>
[ShallowCopy(0)]
public partial class ShallowFriend
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public ShallowFriend? BestFriend { get; set; }
}

// ─── Struct fixtures ──────────────────────────────────────────────────────────

/// <summary>
/// Simple value-type struct with primitives only.
/// Both DeepCopy and ShallowCopy apply: verifies the generator works for structs.
/// </summary>
[DeepCopy]
[ShallowCopy]
public partial struct Money
{
    public int Amount { get; set; }
    public string? Currency { get; set; }
}

/// <summary>
/// Struct that holds a reference-type property.
/// depth=2: level 0 = the struct itself (always copied), level 1 = Address (within depth, copied).
/// </summary>
[DeepCopy(2)]
public partial struct PackageDimensions
{
    public double Weight { get; set; }
    public Address? ReturnAddress { get; set; }
}

/// <summary>
/// Struct that holds a shallow-copyable reference-type property.
/// depth=1: the nested ShallowAddress is shallow-copied.
/// </summary>
[ShallowCopy(1)]
public partial struct ShallowPackageDimensions
{
    public double Weight { get; set; }
    public ShallowAddress? ReturnAddress { get; set; }
}

// ─── Record struct fixtures ───────────────────────────────────────────────────

/// <summary>
/// Positional record struct with primitive properties only.
/// Verifies the generator correctly handles the struct path (not the class/record path).
/// </summary>
[DeepCopy]
[ShallowCopy]
public partial record struct Point(int X, int Y);

/// <summary>
/// Record struct with a string tag — verifies strings are treated as primitive.
/// </summary>
[DeepCopy]
[ShallowCopy]
public partial record struct LabeledPoint(int X, int Y, string? Label);

// ─── Additional record class fixtures ────────────────────────────────────────

/// <summary>
/// Record class with mutable init properties and a nested reference.
/// depth=2: both the record and its nested object are copied.
/// </summary>
[DeepCopy(2)]
public partial record CustomerRecord(int Id, string? FullName, Address? HomeAddress, List<string>? Emails);
