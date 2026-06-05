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

// ─── Regression fixtures for the depth/identity-map fixes ─────────────────────

/// <summary>
/// Open generic type — verifies the generator emits the type-parameter list and treats
/// the type-parameter property as opaque (copied as-is, shared reference / value).
/// </summary>
[DeepCopy]
public partial class Box<T>
{
    public T? Value { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Value-equal record — two distinct instances compare equal by value. Used to verify the
/// identity map keys on reference identity, not value equality.
/// </summary>
[DeepCopy(3)]
public partial record Tag(string Name);

[DeepCopy(3)]
public partial class Article
{
    public Tag? First { get; set; }
    public Tag? Second { get; set; }
}

/// <summary>
/// Immutable class with get-only properties fed exclusively through the constructor.
/// Verifies constructor arguments match get-only properties (no data loss).
/// </summary>
[DeepCopy(2)]
public partial class ImmutablePoint
{
    public int X { get; }
    public string? Label { get; }

    public ImmutablePoint(int x, string? label)
    {
        X = x;
        Label = label;
    }
}

/// <summary>
/// Parameterless type with init-only properties — must be initialised via an object initializer.
/// </summary>
[DeepCopy(2)]
public partial class InitPerson
{
    public string? Name { get; init; }
    public Address? Home { get; init; }
}

/// <summary>
/// Constructor-initialised class with a settable property NOT supplied by the constructor.
/// The settable property must still be copied.
/// </summary>
[DeepCopy(2)]
public partial class OrderWithExtra
{
    public int Id { get; }
    public string? Note { get; set; }

    public OrderWithExtra(int id)
    {
        Id = id;
    }
}

/// <summary>
/// Constructor-initialised class with a settable self-referential property.
/// The cycle flows through the settable back-reference, which is assigned after the
/// instance is registered in the identity map — so the cycle is preserved.
/// </summary>
[DeepCopy(3)]
public partial class CtorFriend
{
    public string? Name { get; }
    public CtorFriend? Buddy { get; set; }

    public CtorFriend(string? name)
    {
        Name = name;
    }
}

/// <summary>
/// Opt-in via the marker interface instead of an attribute (default depth = 5).
/// </summary>
public partial class InterfaceDeep : IDeepCopy
{
    public int Id { get; set; }
    public Address? Addr { get; set; }
}

public partial class InterfaceShallow : IShallowCopy
{
    public int Id { get; set; }
    public ShallowAddress? Addr { get; set; }
}

// ─── Nested type fixtures ─────────────────────────────────────────────────────

/// <summary>
/// Copyable types nested one level inside a partial host.
/// </summary>
public partial class OuterHost
{
    [DeepCopy(2)]
    public partial class NestedItem
    {
        public int Id { get; set; }
        public Address? Addr { get; set; }
    }

    [ShallowCopy(1)]
    public partial class NestedShallowItem
    {
        public int Id { get; set; }
        public ShallowAddress? Addr { get; set; }
    }
}

/// <summary>
/// A copyable type nested two levels deep — both enclosing types are partial.
/// </summary>
public partial class Level1
{
    public partial class Level2
    {
        [DeepCopy(2)]
        public partial class DeeplyNested
        {
            public string? Name { get; set; }
            public Address? Addr { get; set; }
        }
    }
}

/// <summary>
/// A copyable type nested inside a generic host — the generated partial must reproduce the
/// host's type parameter list (<c>GenericHost&lt;THost&gt;</c>).
/// </summary>
public partial class GenericHost<THost>
{
    [DeepCopy]
    public partial class NestedInGeneric
    {
        public int Value { get; set; }
        public string? Tag { get; set; }
    }
}

// ─── Collection / array / dictionary / tuple coverage (deep copy) ─────────────

/// <summary>
/// Exercises the array, HashSet, Queue, Stack, Dictionary and tuple deep-copy code paths,
/// for both primitive and complex element types.
/// </summary>
[DeepCopy(3)]
public partial class CollectionBag
{
    public int[]? Numbers { get; set; }
    public LineItem[]? ItemArray { get; set; }

    public HashSet<int>? IntSet { get; set; }
    public HashSet<LineItem>? ItemSet { get; set; }

    public Queue<int>? IntQueue { get; set; }
    public Stack<int>? IntStack { get; set; }

    public Dictionary<string, int>? PrimitiveMap { get; set; }
    public Dictionary<string, LineItem>? ComplexMap { get; set; }

    public (int Number, string Text) Pair { get; set; }
}

// ─── Inheritance coverage ─────────────────────────────────────────────────────

public class BaseEntity
{
    public int BaseId { get; set; }
    public Address? BaseAddr { get; set; }
}

/// <summary>
/// Verifies that inherited public settable properties (walked up the base chain) are copied.
/// </summary>
[DeepCopy(2)]
public partial class DerivedEntity : BaseEntity
{
    public string? Extra { get; set; }
}
