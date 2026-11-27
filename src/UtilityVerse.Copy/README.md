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
is ignored. Ordinary classes bypass their own existing constructors: the generated
copy constructor registers the destination and hydrates its stored state directly.
Constructor parameter names do not need to match state members. Field initializers
and parameterless constructors on non-opted-in base classes can still run; copying
does not suppress arbitrary side effects in that code.

A primary constructor must run under C# constructor-chaining rules. Its
arguments must match stored fields or auto-properties and contain only immutable
values with no mutable references, including references nested in structs. A primary
constructor cannot be combined with a generated base copy constructor. Reference
records use the compiler-synthesized copy constructor; custom record copy constructors
are rejected because they can mutate the source. Structs without primary constructors
initialize through `this()` before hydrating state; an explicitly declared
parameterless struct constructor can run.

One `CopyContext` tracks reference identity across the operation. Cycles through
opted-in classes and mutable collections are supported, and repeated references
point to the same copied instance. Derived runtime types must also opt in; an
unrecognized derived runtime type throws `NotSupportedException` rather than losing
its derived state. Opted-in base classes preserve their private and readonly state.
Abstract opted-in classes support generated concrete subclasses.

Overridden auto-properties preserve each declaring class's independent backing
storage through generated typed `UnsafeAccessor` field methods. Copying bypasses
virtual getters and setters, including computed overrides, and supports readonly,
getter-only, init-only, and shared or cyclic reference state.

The field-access backend requires .NET 8+ for non-generic declaring types and .NET
9+ for generic declaring types, including generic enclosing types. The generator
and runtime package still target netstandard2.0 and use Roslyn 4.11. Older consumers
retain ordinary copy support; shapes requiring unsupported physical field access
receive `UV004` rather than an inaccurate copy. Generic helpers preserve parameter
order and constraints, even when the derived type's parameters differ from the base.

Opted-in bases distributed in another assembly must be rebuilt with this field
backend for their applicable target. Generated copy constructors record a physical
storage capability marker; legacy bases without it receive `UV004` when an override
would otherwise lose stored state. Private/protected nested owners or property types use helpers in
partial owning/enclosing types; inaccessible storage whose owners cannot host a
safe helper (including shadowed enclosing generic parameter names) are diagnosed.

An explicit backing field remains a portable alternative on older targets.
Overrides of abstract properties need no base storage access. Shallow copies use
`MemberwiseClone` and preserve both backing fields on all supported targets. No
runtime model-member reflection or dynamically emitted code is used.

Dictionary entries and hash-set elements are copied into typed temporary buffers.
Their destination containers are registered immediately, preserving cycles and
aliases, but insertion waits until the outermost graph operation has hydrated
object state. This allows a key's hash or equality to depend on its owner's copied
fields, including fields declared after the collection.

Once every pending container is populated, standard dictionary and set lookups are
checked. Containers with invalid buckets are rebuilt in place once, preserving
their identity and comparer, then checked again. Hash/equality dependencies that
remain unstable throw `NotSupportedException`; duplicate copied keys or elements
also throw rather than being discarded. This is not a general dependency solver
for arbitrary comparers. Hash/equality implementations must be side-effect-free
and stable on the completed graph; custom copy callbacks must not depend on pending
containers already being populated during hydration.

All public `CopyContext` copy entry points and generated `IDeepCopyable.Copy` calls
complete pending work before the outermost call returns. Generated implementations
use `CopyGenerated<T>(T, Func<T, CopyContext, object>)` to establish that boundary
even when called through the interface directly. Existing context entry-point
signatures remain unchanged. If an operation fails, cached destinations and pending
work are cleared so the context cannot return partial copies on a later call. A
custom copier must propagate nested failures; suppressing one still causes the
enclosing operation to fail.

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

Covariant sequence views preserve the exact supported runtime container and its
actual element type. For example, a `List<Derived>` shared by `IEnumerable<Base>`
and `List<Derived>` members produces one `List<Derived>` copy in either traversal
order. This also applies to empty and null-only collections, nested sequences,
queue/stack order, and set comparers. `IEnumerable<T>` describes enumeration only;
construction and insertion still use typed adapters for each supported container.

Generated code registers declared sequence element types across the model's
inheritance hierarchy, plus accessible non-generic opted-in source models assignable
to those element views. Immutable reference scalars (`string`, `Uri`, `Version`)
are registered by the runtime. When an actual element type cannot be determined
from that source context (for example, an external subtype or a closed generic
subtype visible only through a base view), register it before copying:

```csharp
CopyContext.RegisterSequenceType<ExternalDerived>();

var copy = model.DeepCopy();
```

Direct `CopyContext.CopySequence` callers use the same registration API. A known
container with an unregistered actual element type throws `NotSupportedException`,
including when empty; it never silently changes that type. Registered adapters
honor the caller's element-copy delegate. The delegate must return values assignable
to the actual element type. Registration does not make an unsupported model or
custom collection implementation copyable. Collection type metadata is inspected
once per runtime container type and cached weakly; copying uses statically compiled
generic methods, without model-member reflection or dynamic code generation.

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
- Primary constructors with unmatched or potentially mutable arguments,
  primary constructors requiring generated base copying, custom record copy
  constructors, captured primary-constructor state, and event subscriptions.
- Non-opted-in base classes with no accessible parameterless constructor.
- Inaccessible, hidden, or readonly inherited state when the base type has no deep opt-in; external bases must provide the current graph-copy support.
- Stored-property override chains requiring field access unavailable on the
  consumer framework, legacy external bases without physical-storage capability,
  or inaccessible nested owners that cannot host a safe generated helper.
- Deep-copy record inheritance, multidimensional arrays, pointer state, and
  immutable/frozen collection implementations.

Use a custom copy implementation for those shapes. Cycles through immutable
reference tuples throw `NotSupportedException`; mutable object cycles are supported.
Keys and set elements must retain stable hash/equality behavior on the completed
graph, just as they must in the original collection. Copies are snapshots of a quiescent graph;
concurrent mutation during copying is not synchronized.

## Validation

`tests/UtilityVerse.Copy.UnitTest` checks generated compilation, analyzer results,
and runtime behavior, including .NET Standard 2.0 and .NET 6 consumer compilation.
`bash scripts/verify-packages.sh` packs Release artifacts and restores a fresh
consumer from the local packages, then runs it on .NET 8 and .NET 11. CI runs this check as well.
