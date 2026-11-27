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
`IShallowCopy`. Attributes also support structs. Request both modes on one type
to generate both methods. A validation error in one mode only suppresses that
mode's method.

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
values with no mutable references, including references nested in structs. An ordinary
class primary constructor cannot be combined with a generated base copy constructor.
A non-positional derived record can chain to an opted-in base's generated constructor
and preserve its private and readonly state. Its own field initializers can run.
Other supported reference records use a chain of compiler-synthesized record copy
constructors and hydrate accessible inherited state. Positional records that need
inaccessible inherited state, custom record copy constructors, and unverified
external record-copy chains receive `UV004`.
Structs without primary constructors initialize through `this()` before hydrating
state. An explicitly declared parameterless struct constructor can run.

One `CopyContext` tracks reference identity across the operation. Cycles through
opted-in classes and mutable collections are supported, and repeated references
point to the same copied instance. Derived runtime types must also opt in; an
unrecognized derived runtime type throws `NotSupportedException` rather than losing
its derived state. Opted-in base classes preserve their private and readonly state.
Abstract opted-in classes support generated concrete subclasses.
Deep copying supports up to 512 nested copy operations and checks available stack
space periodically. Excessive nesting throws `NotSupportedException` and clears
the partial graph. A model can take more than one operation per layer.

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
  `Uri`, and `Version`. Only an exact `Uri` instance can be shared as immutable.
  A runtime subclass needs an `IDeepCopyable` implementation or copying throws
  `NotSupportedException`. A struct containing a `Uri` member needs a deep copy
  opt-in so the copier checks the member's runtime type. Rebuild previously
  generated consumers to replace assignments that shared `Uri` members directly.
- Arrays of any rank, including jagged arrays, nullable elements, and nonzero
  lower bounds. Copies keep the runtime array type, lengths, and bounds.
- `List<T>`, `LinkedList<T>`, `HashSet<T>`, `SortedSet<T>`, `Queue<T>`,
  `Stack<T>`, `Collection<T>`,
  `ReadOnlyCollection<T>`, `ObservableCollection<T>`, and `BindingList<T>`.
  Binding lists preserve `AllowEdit`, `AllowRemove`, `RaiseListChangedEvents`,
  and the current `AllowNew` value. Copies start with fresh change-notification
  wiring and a clean add-new transaction state. Reattach external subscribers to
  the copy, including any `AddingNew` handler that supplied new items.
- `IEnumerable<T>`, `IList<T>`, `ICollection<T>`, `IReadOnlyList<T>`, and
  `IReadOnlyCollection<T>`. Recognized runtime collection types are preserved;
  other enumerable sequences are materialized into lists. Unsupported collection
  subclasses are rejected rather than losing their additional state.
- `Dictionary<TKey,TValue>`, `SortedDictionary<TKey,TValue>`,
  `SortedList<TKey,TValue>`, and `ConcurrentDictionary<TKey,TValue>` on
  frameworks exposing its public `Comparer`. `IDictionary<TKey,TValue>` and
  `IReadOnlyDictionary<TKey,TValue>` views preserve these concrete types. An
  interface view can copy a concurrent dictionary when the declared key type
  satisfies the concurrent dictionary's nonnullable key constraint.
  Dictionary and set comparers are retained.
  Generated copies throw `NotSupportedException` for runtime subclasses of these
  maps. A custom `CopyContext.CopyMap` factory can handle a subclass by returning
  the same runtime type and preserving its additional state.
- Reference tuples and value tuples; their mutable children are copied recursively.
- `ImmutableList<T>`, `ImmutableHashSet<T>`, `ImmutableSortedSet<T>`,
  `ImmutableDictionary<TKey,TValue>`, `ImmutableSortedDictionary<TKey,TValue>`,
  `FrozenDictionary<TKey,TValue>`, and `FrozenSet<T>`. Immutable and frozen maps
  and sets require immutable scalar keys or elements. Map values and immutable list
  items can be mutable and are copied recursively. `ImmutableArray<T>` with
  immutable scalar elements shares its immutable backing.
- Nested collections and opted-in mutable structs. A struct containing mutable
  references must opt in; it is never silently treated as an immutable value.

Covariant sequence views preserve the exact supported runtime container and its
actual element type. For example, a `List<Derived>` shared by `IEnumerable<Base>`
and `List<Derived>` members produces one `List<Derived>` copy in either traversal
order. This also applies to empty and null-only collections, nested sequences,
queue/stack order, and set comparers. `IEnumerable<T>` describes enumeration only;
construction and insertion still use typed adapters for each supported container.

Collection wrappers are copied as standalone snapshots when their public
`ICollection.SyncRoot` identifies a supported list or array. A graph that copies
both a wrapper and its backing store, or distinct wrappers sharing one backing
store, throws `NotSupportedException`. Mutable wrappers with read-only or
fixed-size backing and wrappers with opaque synchronization roots also require a
custom copier. This includes frameworks that expose an opaque synchronization
root for their standard list implementation.

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

Generic type parameters can contain immutable scalars, `IDeepCopyable` values,
and supported runtime sequences such as `List<int>`. Members declared as
`System.Array` use the same runtime element dispatch. Common scalar element
types and `object` are registered automatically. Generated code also registers
visible opted-in model types for these runtime views. Use
`CopyContext.RegisterSequenceType<T>()` for other element types, including
closed generic or nested array types that the generator cannot discover.
Unsupported runtime values throw `NotSupportedException`.
There is no reflection over model members;
generated typed assignments perform the copy, with a small runtime context from
this package and no external runtime dependencies. Keep the package's runtime
reference when distributing consumers.

When a graph passes an opted-in struct through an object or interface reference,
repeated references to the same box share one destination box. Separate boxes
remain separate even if their values are equal. Use `CopyContext.Copy(boxedValue)`
to copy a boxed root so the context receives its original reference. Calling
`IDeepCopyable.Copy` directly on a struct copies its value.

Preserving cycles through a box requires generated code compiled against a
framework that exposes `Unsafe.Unbox<T>`. The copier registers the new box before
copying its children, then writes the copied value into it. Older generated
copiers and targets without that API preserve acyclic box aliases and throw
`NotSupportedException` when a box is revisited before its copy is ready.

## Member policies and external copiers

Apply `[CopyPolicy(CopyBehavior.Share)]` to keep a reference such as a stream or
shared lock. `Reset` assigns the type's default value, which can clear a callback
or release the destination's reference to a source-owned resource.
`Copy` is the default. `Custom` uses a copier registered with
`CopyContext.RegisterExternalCopier<T>()` for the value's exact runtime type.
Apply the policy to a stored property or field. An auto-property can also receive
the attribute on its backing field using the `field:` target.

An external copier returns a new instance of the same runtime type. It can call
`context.Register(source, destination)` before copying child references to
preserve cycles. Register each supported subtype separately. Boxed external
structs preserve completed aliases and reject cycles that require creating the
destination box before the copier returns. Keep the same policy on members that
should point to the same copied object. Ref-like and pointer state still requires
a custom model-level implementation.

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

- Non-partial containing types, static/ref-like types, and declarations that
  conflict with emitted members. A field, property, event, or nested type named
  `DeepCopy` or `ShallowCopy` conflicts with the corresponding generated method.
  Methods with distinct signatures, such as `DeepCopy<T>()`, can coexist.
- Primary constructors with unmatched or potentially mutable arguments,
  primary constructors requiring generated base copying, custom record copy
  constructors, captured primary-constructor state, and event subscriptions.
- Non-opted-in base classes with no accessible parameterless constructor.
- Inaccessible, hidden, or readonly inherited state when the base type has no deep opt-in; external bases must provide the current graph-copy support.
- Stored-property override chains requiring field access unavailable on the
  consumer framework, legacy external bases without physical-storage capability,
  or inaccessible nested owners that cannot host a safe generated helper.
- Explicit-interface auto-properties in deep-copy models. Implement the property
  with an ordinary backing field or supply a custom deep copier.
- Pointer state, unsupported immutable or frozen collection implementations,
  and mutable keys in immutable or frozen maps and sets.

Use a custom copy implementation for those shapes. Cycles through immutable
reference tuples and immutable or frozen collections throw
`NotSupportedException`. Mutable object cycles are supported.
Keys and set elements must retain stable hash/equality behavior on the completed
graph, just as they must in the original collection. Copies are snapshots of a quiescent graph;
concurrent mutation during copying is not synchronized.

## Validation

`tests/UtilityVerse.Copy.UnitTest` checks generated compilation, analyzer results,
and runtime behavior, including .NET Standard 2.0 and .NET 6 consumer compilation.
`bash scripts/verify-packages.sh` packs Release artifacts and restores a fresh
consumer from the local packages, then runs it on .NET 8 and .NET 11. CI runs this check as well.
