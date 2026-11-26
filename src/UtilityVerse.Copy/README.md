# UtilityVerse.Copy

A Roslyn source generator for typed `DeepCopy()` and `ShallowCopy()` methods on
partial classes, records, structs, and record structs. Public, internal, nested,
and generic types are supported. Containing types must also be partial.

Install with `dotnet add package UtilityVerse.Copy`. A compiler with Roslyn 4.11
or newer is required (.NET SDK 8.0.400+ or Visual Studio 2022 17.11+).

```csharp
using UtilityVerse.Copy;

[DeepCopy]
public partial class Node
{
    public string? Name { get; set; }
    public Node? Next { get; set; }
    public List<Node> Children { get; } = new();
}

var node = new Node { Name = "original" };
node.Next = node;
var copy = node.DeepCopy();
// copy != node, copy.Next == copy, and copy.Children is independent.
```

Use `[DeepCopy]` / `[ShallowCopy]`, or the empty marker interfaces `IDeepCopy` /
`IShallowCopy`. Attributes also support structs. When both modes are requested on
one type, deep copy takes precedence, as in earlier versions.

## Deep-copy contract

Generated constructors copy stored instance fields and auto-properties, including
private fields, readonly fields, init-only properties, and stored getter-only
properties. Computed properties are derived from that copied state. Static state
is ignored. Existing constructors may run before the stored state is hydrated;
constructor parameters must match readable properties or fields by name and a supported assignment-compatible type. No
unmatched argument is silently replaced by a default value.

One `CopyContext` tracks reference identity across the operation. Cycles through
opted-in classes and mutable collections are supported, and repeated references
point to the same copied instance. Derived runtime types must also opt in; an
unrecognized derived runtime type throws `NotSupportedException` rather than losing
its derived state. Opted-in base classes preserve their private and readonly state.
Abstract opted-in classes support generated concrete subclasses.

Supported state includes:

- Primitive values, enums, strings, immutable built-in values, nullable values,
  `Uri`, and `Version`.
- One-dimensional and jagged arrays, including nullable elements.
- `List<T>`, `HashSet<T>`, `Queue<T>`, `Stack<T>`, `Collection<T>`,
  `ReadOnlyCollection<T>`, `ObservableCollection<T>`, and `BindingList<T>`.
- `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, and
  `IReadOnlyCollection<T>`. Recognized runtime collection types are preserved;
  other enumerable sequences are materialized into lists. Unsupported collection
  subclasses are rejected rather than losing their additional state.
- `Dictionary<TKey,TValue>` and, on frameworks exposing its public `Comparer`,
  `ConcurrentDictionary<TKey,TValue>`. Dictionary and set comparers are retained.
- Reference tuples and value tuples; their mutable children are copied recursively.
- Nested collections and opted-in mutable structs. A struct containing mutable
  references must opt in; it is never silently treated as an immutable value.

Generic type parameters can contain immutable scalar values or generated
`IDeepCopyable` values. Other runtime generic values require a custom copy method
and throw `NotSupportedException`. There is no reflection over model members;
generated typed assignments perform the copy, with a small runtime context from
this package and no external runtime dependencies. Keep the package's runtime
reference when distributing consumers.

## Shallow-copy contract

A class or record gets a new outer instance via `MemberwiseClone()`. All reference
fields remain shared, including arrays, collections, and nested objects. Structs
and record structs return a value copy. Nested types do not need any copy opt-in.
This also works for init-only and getter-only properties.

**Behavior change:** earlier versions cloned array containers while sharing list
containers. Arrays now follow the same shallow-copy semantics as every other
reference field.

## Unsupported shapes and diagnostics

`UV001` reports a missing `partial` modifier. `UV002` identifies stored deep-copy
state whose mutable type has no opt-in, including array elements and struct state.
`UV003` remains reserved for compatibility; shallow references no longer require
nested opt-ins. `UV004` reports shapes that cannot safely be generated:

- Non-partial containing types, static/ref-like types, and reserved method or
  constructor collisions.
- Constructors with no usable state-name match, captured primary-constructor state,
  and event subscriptions.
- Inaccessible, hidden, or readonly inherited state when the base type has no deep opt-in; external bases must provide the current graph-copy support.
- Deep-copy record inheritance, multidimensional arrays, pointer state, and
  immutable/frozen collection implementations.

Use a custom copy implementation for those shapes. Cycles through immutable
reference tuples throw `NotSupportedException`; mutable object cycles are supported.
Keys and set elements must retain stable hash/equality behavior while stored, just
as they must in the original collection. Copies are snapshots of a quiescent graph;
concurrent mutation during copying is not synchronized.

## Validation

`tests/UtilityVerse.Copy.UnitTest` checks generated compilation, analyzer results,
and runtime behavior, including .NET Standard 2.0 and .NET 6 consumer compilation.
`bash scripts/verify-packages.sh` packs Release artifacts and restores a fresh
consumer from the local packages, then runs it on .NET 8 and .NET 11. CI runs this check as well.
