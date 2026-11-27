# UtilityVerse.Copy examples

Use attribute or interface opt-ins for generated methods. Keep the runtime package
reference, make containing types partial, and give mutable stored child types
their own deep-copy support. These examples cover public copy entry points and
supported model and collection families.

Each C# block is an independent console program. Run blocks separately. They
include checks that throw if the illustrated behavior fails. Package verification
compiles and runs the blocks against installed packages. Sections identify
features that need framework-specific APIs.

[Opt-ins and graph identity](#opt-ins-and-graph-identity) · [Depth and failure recovery](#depth-and-failure-recovery) · [Stacks queues and collection views](#stacks-queues-and-collection-views) · [Supported container matrix](#supported-container-matrix) · [Arrays tuples and generic state](#arrays-tuples-and-generic-state) · [Records inheritance and nested models](#records-inheritance-and-nested-models) · [Boxed structs](#boxed-structs) · [Member policies and external copiers](#member-policies-and-external-copiers) · [Manual context entry points](#manual-context-entry-points) · [Diagnostics and consumer decisions](#diagnostics-and-consumer-decisions)

## Opt-ins and graph identity

Attribute and interface opt-ins generate the same methods. Use partial models
and partial enclosing types. Request both copy modes when needed. Deep copies
preserve aliases and mutable cycles; shallow copies share reference fields.

```csharp
using UtilityVerse.Copy;

var source = new Node { Name = "root" };
source.Next = source;
source.Children.Add(source);
var deep = source.DeepCopy();
var shallow = source.ShallowCopy();
new InterfaceModel().DeepCopy();
new InterfaceModel().ShallowCopy();

if (ReferenceEquals(source, deep) || !ReferenceEquals(deep, deep.Next) ||
    !ReferenceEquals(deep, deep.Children[0]) || !ReferenceEquals(source.Children, shallow.Children))
{
    throw new Exception("Graph identity failed.");
}

[DeepCopy]
[ShallowCopy]
public partial class Node
{
    public string Name { get; set; } = "";
    public Node? Next { get; set; }
    public List<Node> Children { get; } = new();
}

public partial class InterfaceModel : IDeepCopy, IShallowCopy
{
    public int Value { get; set; }
}
```

## Depth and failure recovery

The root is level 1. Defaults are 5, accepted settings are 1–100, and the root's
setting governs nested models. New models and containers add levels. Nulls,
scalars, and already-copied references consume no additional level. Invalid
configuration produces UV006 and falls back to 5. Avoid suppressing copy failures.
Available execution-stack space and internal-operation limits can stop a copy
before its configured limit. Compiler type traversal is independently capped at
100, with UV004 for excessive nesting.

```csharp
using UtilityVerse.Copy;

var root = new Node();
root.Next = root;
root.DeepCopy();
root.Next = new Node();
var context = new CopyContext();
var failed = false;

try
{
    context.Copy(root);
}
catch (NotSupportedException)
{
    failed = true;
}

root.Next = root;
var retry = context.Copy(root);
var configurable = new InterfaceModel { Next = new InterfaceModel() };
configurable.DeepCopy();

if (!failed || !ReferenceEquals(retry, retry.Next))
{
    throw new Exception("Depth rejection or recovery failed.");
}

[DeepCopy(depth: 1)]
public partial class Node
{
    public Node? Next { get; set; }
}

[CopyDepth(10)]
public partial class InterfaceModel : IDeepCopy
{
    public InterfaceModel? Next { get; set; }
}
```

## Stacks queues and collection views

A stack retains LIFO order; a queue retains FIFO order. Covariant views retain
the actual supported collection and element types. A root, its container, and a
new model element occupy levels 1, 2, and 3. Hidden external element types may
need explicit sequence registration, even for empty collections.

```csharp
using UtilityVerse.Copy;

CopyContext.RegisterSequenceType<Derived>();
var item = new Derived { Id = 7 };
var actual = new List<Derived> { item };
var source = new Work
{
    Wide = actual,
    Narrow = actual,
    Undo = new Stack<Derived>(new[] { item, new Derived { Id = 8 } }),
    Pending = new Queue<Derived>(new[] { item })
};
var copy = source.DeepCopy();

if (!ReferenceEquals(copy.Wide, copy.Narrow) || ReferenceEquals(copy.Narrow, actual) ||
    copy.Undo.Pop().Id != 8 || copy.Undo.Pop().Id != 7 || copy.Pending.Dequeue().Id != 7)
{
    throw new Exception("Collection views or ordering failed.");
}

[DeepCopy]
public partial class Base
{
    public int Id { get; set; }
}

[DeepCopy]
public partial class Derived : Base
{
}

[DeepCopy]
public partial class Work
{
    public IEnumerable<Base>? Wide { get; set; }
    public List<Derived>? Narrow { get; set; }
    public Stack<Derived> Undo { get; set; } = new();
    public Queue<Derived> Pending { get; set; } = new();
}
```

## Supported container matrix

This model lists every supported concrete collection family and interface view.
Frozen collections in this example require .NET 8 or newer.
Map and set comparers are retained. ConcurrentDictionary support requires a target
with its public Comparer. Immutable and frozen maps and sets require immutable
scalar keys or set elements. ImmutableArray with scalar elements shares immutable
backing. Mutable items in lists and map values can be copied recursively.
BindingList preserves editing and notification settings with fresh item wiring;
reattach external event subscribers to the copy. Wrapper copies require public
synchronization roots identifying a supported backing list or array. Graphs that
also copy that backing store, or distinct wrappers sharing it, are rejected.

```csharp
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Collections.Immutable;
using System.Collections.Frozen;
using System.ComponentModel;
using UtilityVerse.Copy;

var source = new Collections();
var copy = source.DeepCopy();

if (ReferenceEquals(source.Value0, copy.Value0) || copy.Value4.Dequeue() != 1 ||
    copy.Value5.Pop() != 1 || copy.Value10["KEY"] != 1 || copy.Value21[0] != 1)
{
    throw new Exception("Container copying failed.");
}

[DeepCopy]
public partial class Collections
{
    public List<int> Value0 { get; set; } = new() { 1 };

    public LinkedList<int> Value1 { get; set; } = new(new[] { 1 });

    public HashSet<int> Value2 { get; set; } = new() { 1 };

    public SortedSet<int> Value3 { get; set; } = new() { 1 };

    public Queue<int> Value4 { get; set; } = new(new[] { 1 });

    public Stack<int> Value5 { get; set; } = new(new[] { 1 });

    public Collection<int> Value6 { get; set; } = new() { 1 };

    public ReadOnlyCollection<int> Value7 { get; set; } = new(new[] { 1 });

    public ObservableCollection<int> Value8 { get; set; } = new() { 1 };

    public BindingList<int> Value9 { get; set; } = new() { 1 };

    public Dictionary<string, int> Value10 { get; set; } = new(StringComparer.OrdinalIgnoreCase) { ["key"] = 1 };

    public SortedDictionary<string, int> Value11 { get; set; } = new(StringComparer.OrdinalIgnoreCase) { ["key"] = 1 };

    public SortedList<string, int> Value12 { get; set; } = new(StringComparer.OrdinalIgnoreCase) { ["key"] = 1 };

    public ConcurrentDictionary<string, int> Value13 { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public ImmutableList<int> Value14 { get; set; } = ImmutableList.Create(1);

    public ImmutableHashSet<int> Value15 { get; set; } = ImmutableHashSet.Create(1);

    public ImmutableSortedSet<int> Value16 { get; set; } = ImmutableSortedSet.Create(1);

    public ImmutableDictionary<string, int> Value17 { get; set; } = ImmutableDictionary<string, int>.Empty.Add("key", 1);

    public ImmutableSortedDictionary<string, int> Value18 { get; set; } = ImmutableSortedDictionary<string, int>.Empty.Add("key", 1);

    public FrozenDictionary<string, int> Value19 { get; set; } = new Dictionary<string, int> { ["key"] = 1 }.ToFrozenDictionary();

    public FrozenSet<int> Value20 { get; set; } = new[] { 1 }.ToFrozenSet();

    public ImmutableArray<int> Value21 { get; set; } = ImmutableArray.Create(1);

    public IEnumerable<int> Value22 { get; set; } = new List<int> { 1 };

    public IList<int> Value23 { get; set; } = new List<int> { 1 };

    public ICollection<int> Value24 { get; set; } = new List<int> { 1 };

    public IReadOnlyList<int> Value25 { get; set; } = new List<int> { 1 };

    public IReadOnlyCollection<int> Value26 { get; set; } = new List<int> { 1 };

    public IDictionary<string, int> Value27 { get; set; } = new Dictionary<string, int> { ["key"] = 1 };

    public IReadOnlyDictionary<string, int> Value28 { get; set; } = new Dictionary<string, int> { ["key"] = 1 };
}
```

## Arrays tuples and generic state

Array copies preserve runtime type, rank, lengths, lower bounds, and aliases.
`System.Array` and generic runtime collection views may need element registration.
Reference tuples recursively copy children and reject cycles through unfinished
immutable tuples. Mutable structs need their own opt-in. Exact Uri instances and
Version values can be shared; Uri subclasses require supported runtime copying.

```csharp
using UtilityVerse.Copy;

CopyContext.RegisterSequenceType<int>();
var shaped = Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 5 });
shaped.SetValue(42, 5);
var source = new Shapes
{
    Matrix = new[,] { { 1, 2 } },
    Jagged = new[] { new[] { 3, 4 } },
    Shaped = shaped,
    Pair = Tuple.Create(new Item { Value = 7 }, "label"),
    ValuePair = (new Item { Value = 8 }, 9),
    Generic = new Holder<List<int>> { Value = new List<int> { 1, 2 } },
    Optional = 12,
    Address = new Uri("https://example.com"),
    Version = new Version(1, 0),
    Rune = new System.Text.Rune('A'),
    Time = new TimeOnly(12, 30)
};
var copy = source.DeepCopy();

if (ReferenceEquals(source.Matrix, copy.Matrix) || copy.Matrix[0, 1] != 2 ||
    ReferenceEquals(source.Jagged[0], copy.Jagged[0]) || copy.Shaped!.GetLowerBound(0) != 5 ||
    (int)copy.Shaped.GetValue(5)! != 42 || ReferenceEquals(source.Pair!.Item1, copy.Pair!.Item1) ||
    ReferenceEquals(source.Generic!.Value, copy.Generic!.Value) || copy.Optional != 12)
{
    throw new Exception("Array, tuple, or generic copying failed.");
}

[DeepCopy]
public partial class Item
{
    public int Value { get; set; }
}

[DeepCopy]
public partial class Holder<T>
{
    public T? Value { get; set; }
}

[DeepCopy]
public partial class Shapes
{
    public int[,] Matrix { get; set; } = new int[0, 0];
    public int[][] Jagged { get; set; } = Array.Empty<int[]>();
    public Array? Shaped { get; set; }
    public Tuple<Item, string>? Pair { get; set; }
    public (Item, int) ValuePair { get; set; }
    public Holder<List<int>>? Generic { get; set; }
    public int? Optional { get; set; }
    public Uri? Address { get; set; }
    public Version? Version { get; set; }
    public System.Text.Rune Rune { get; set; }
    public TimeOnly Time { get; set; }
}
```

## Records inheritance and nested models

Stored private, readonly, init-only, and getter-only state is copied. Computed
properties derive from that state. Virtual auto-property storage is preserved
per declaring type. Physical backing-field access requires .NET 8+ for nongeneric
owners and .NET 9+ for generic owners. External generated bases need current
physical-storage support. Primary constructors must match stored immutable state.
Ordinary constructor bodies are bypassed for supported classes, while field
initializers, struct constructors, and non-opted-in base constructors can run.

```csharp
using UtilityVerse.Copy;

var source = new Derived { Values = new List<int> { 2 } };
var copy = source.DeepCopy();
var record = new Label("ready").DeepCopy();
var point = new Point(1, 2).DeepCopy();
var nested = new Envelope.Entry { Value = 4 }.DeepCopy();

if (copy.BaseValue != 1 || copy.Values[0] != 2 || ReferenceEquals(source.Values, copy.Values) ||
    record.Text != "ready" || point.X != 1 || nested.Value != 4)
{
    throw new Exception("Model shape copying failed.");
}

[DeepCopy]
public partial class Base
{
    public virtual List<int> Values { get; init; } = new() { 1 };
    private readonly int baseValue = 1;

    public int BaseValue => baseValue;
}

[DeepCopy]
public partial class Derived : Base
{
    public override List<int> Values { get; init; } = new();
}

[DeepCopy]
public partial record Label(string Text);

[DeepCopy]
public partial record struct Point(int X, int Y);

public partial class Envelope
{
    [DeepCopy]
    public partial class Entry
    {
        public int Value { get; set; }
    }
}
```

## Boxed structs

Pass a boxed root through `CopyContext.Copy` so its original reference reaches the
context. Generic members provide runtime object/interface views; directly declared
object members require an explicit supported policy. Repeated references to one source box share a destination box. Equal,
separately boxed values retain separate identities. Cycles through boxed structs
require generated code targeting a framework with `Unsafe.Unbox<T>`; older
consumers preserve acyclic aliases and reject unfinished box cycles.

```csharp
using UtilityVerse.Copy;

object first = new Value { Number = 7 };
object second = new Value { Number = 7 };
var source = new Boxes<object> { First = first, Alias = first, Second = second };
var copy = source.DeepCopy();
var boxedRoot = new CopyContext().Copy(first);

if (!ReferenceEquals(copy.First, copy.Alias) || ReferenceEquals(copy.First, first) ||
    ReferenceEquals(copy.First, copy.Second) || ((Value)boxedRoot).Number != 7)
{
    throw new Exception("Box identity failed.");
}

[DeepCopy]
public partial struct Value
{
    public int Number { get; set; }
}

[DeepCopy]
public partial class Boxes<T>
{
    public T? First { get; set; }
    public T? Alias { get; set; }
    public T? Second { get; set; }
}
```

## Member policies and external copiers

Copy is the default. Share retains the source reference. Reset assigns default.
Custom dispatches to a copier registered for the exact runtime type. Register a
reference destination before traversing children when supporting cycles. Register
each subtype separately and use consistent ownership policies for shared values.
External callbacks must return a new instance of the same runtime type. Register
an external copier once for each runtime type.

```csharp
using UtilityVerse.Copy;

CopyContext.RegisterExternalCopier<Resource>(static (source, context) =>
{
    var destination = new Resource();
    context.Register(source, destination);
    destination.Owner = context.Copy(source.Owner);
    return destination;
});

using var stream = new MemoryStream();
var source = new Session { Stream = stream, Callback = static () => { } };
var resource = new Resource { Owner = source };
source.First = resource;
source.Second = resource;
var copy = source.DeepCopy();

if (!ReferenceEquals(copy.Stream, stream) || copy.Callback != null ||
    ReferenceEquals(copy.First, resource) || !ReferenceEquals(copy.First, copy.Second) ||
    !ReferenceEquals(copy, copy.First!.Owner))
{
    throw new Exception("Ownership policies failed.");
}

var direct = new CopyContext().CopyExternal(resource);
if (ReferenceEquals(direct, resource))
{
    throw new Exception("Direct external copying failed.");
}

public sealed class Resource
{
    public Session? Owner { get; set; }
}

[DeepCopy]
public partial class Session
{
    [CopyPolicy(CopyBehavior.Share)]
    public Stream? Stream { get; set; }

    [field: CopyPolicy(CopyBehavior.Reset)]
    public Action? Callback { get; set; }

    [CopyPolicy(CopyBehavior.Custom)]
    public Resource? First { get; set; }

    [CopyPolicy(CopyBehavior.Custom)]
    public Resource? Second { get; set; }
}
```

## Manual context entry points

Generated `DeepCopy()` is the usual model entry point. Manual copiers can use
these lower-level methods with one context per graph. `CopyGenerated` completes
pending work when the outermost operation finishes. Its reference factory must
register destinations early. `CopyValue` keeps a typed struct result. The custom
map factory must preserve the runtime map type and additional subclass state.

```csharp
using UtilityVerse.Copy;

var node = new ManualNode { Number = 7 };
node.Next = node;
var copiedNode = new CopyContext().Copy(node);
var array = new CopyContext().CopyArray(new[] { node }, static (value, context) => context.Copy(value));
var scalarArray = new CopyContext().CopyArrayContainer(new[] { 1, 2 });
var shaped = new CopyContext().CopyShapedArray<int>(new[,] { { 42 } }, static (value, _) => value);
var sequence = new CopyContext().CopySequence(new List<int> { 3 }, static (value, _) => value);
var valueCopy = new CopyContext().CopyValue(7, static (value, _) => value);
var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["key"] = 8 };
var map = new CopyContext().CopyMap<Dictionary<string, int>, string, int>(
    dictionary,
    static source => new Dictionary<string, int>(source.Comparer),
    static (destination, key, value) => destination.Add(key, value),
    static (key, _) => key,
    static (value, _) => value);
var view = new CopyContext().CopyDictionaryView<IDictionary<string, int>, string, int>(
    dictionary,
    static (key, _) => key,
    static (value, _) => value);
var immutableContext = new CopyContext();
var tuple = immutableContext.CopyImmutable(Tuple.Create(node), source => Tuple.Create(immutableContext.Copy(source.Item1)));

if (!ReferenceEquals(copiedNode, copiedNode.Next) || ReferenceEquals(array![0], node) ||
    scalarArray!.Length != 2 || (int)shaped!.GetValue(0, 0)! != 42 || sequence!.Single() != 3 ||
    valueCopy != 7 || map!["KEY"] != 8 || view!["KEY"] != 8 || ReferenceEquals(tuple!.Item1, node))
{
    throw new Exception("Context copying failed.");
}

public sealed class ManualNode : IDeepCopyable, ICopyDepthConfiguration
{
    public int Number { get; set; }
    public ManualNode? Next { get; set; }
    int ICopyDepthConfiguration.CopyDepth => 10;

    object IDeepCopyable.Copy(CopyContext context)
    {
        return context.CopyGenerated(this, static (source, graph) =>
        {
            var destination = new ManualNode { Number = source.Number };
            graph.Register(source, destination);
            destination.Next = graph.Copy(source.Next);
            return destination;
        });
    }
}
```

## Diagnostics and consumer decisions

| Situation | Consumer action |
| --- | --- |
| UV001, missing partial | Add partial to the highlighted declaration, including enclosing types. |
| UV002, unsupported child type | Opt in the owned model or choose an intentional Share, Reset, or Custom policy. |
| UV004, unsafe shape or excessive compiler traversal | Follow the diagnostic reason and simplify the stored shape or implement a custom model copier. |
| UV005, ignored policy | Put the policy on stored instance state in a deep-copy model. |
| UV006, invalid or ignored depth | Use 1–100 and one applicable configuration source. |
| Unknown runtime subtype | Opt in the actual type or register an exact external copier with Custom policy. |
| Unknown sequence element type | RegisterSequenceType<T> before copying, including empty collections. |
| Runtime map or collection subclass | Preserve its runtime type and additional state with a custom copier. |
| Shared wrapper backing | Supply an ownership-aware custom copier. |
| Unstable dictionary key hash | Keep hashing and equality stable and side-effect-free on the completed copied graph. |
| Concurrent mutation | Coordinate a quiescent source snapshot before copying. |
| Box cycle on an older target | Rebuild for a target exposing Unsafe.Unbox<T> or redesign the boxed graph. |

Dictionary and set insertion is deferred until object hydration completes. Custom
callbacks must allow those containers to remain pending during hydration. Failed
operations clear partial destinations and pending work. The context rejects a
suppressed nested failure. Cycles through immutable tuples and collections are
rejected. Generated code retains its runtime package reference.

For more constructor, inheritance, framework, and diagnostic details, use the
full Copy guide. [Copy reference](https://github.com/purkayasta/TheUtilityVerse/blob/development/src/UtilityVerse.Copy/README.md)
