# UtilityVerse.Copy instructions

Read the [root AGENTS.md](../../AGENTS.md) for repository-wide instructions,
project maps, build configuration, coding standards, testing, and branching.
These project-specific instructions cover `src/UtilityVerse.Copy/`,
`tests/UtilityVerse.Copy.UnitTest/`, and `tests/UtilityVerse.Copy.Test/`, including
code reviews. Paths below are relative to the repository root unless explicitly
linked relative to this file.

The root AGENTS.md readability and performance rules are mandatory
here too. Give generator implementations, emitted C#, and embedded test models
the same readable spacing and block layout. Prefer typed, allocation-conscious
runtime operations; use value types only where they preserve copying and graph
identity semantics. Formatting changes must preserve behavior.

## Constraints

The generator targets netstandard2.0 and uses Roslyn 4.11. Do not raise its target
or compiler dependency to implement consumer features. Analyzers enforce extended
rules, release tracking, warnings-as-errors, and checked arithmetic. Dependency
versions are central. The same DLL is packed as an analyzer and as a runtime library
for attributes, marker interfaces, CopyContext, and IDeepCopyable. No Roslyn API is
used by the runtime copy context. Preserve existing empty marker interfaces.

## Pipeline

CopyGenerator discovers opted-in named types through attributes or inherited marker
interfaces. Both methods are emitted when both modes are selected. Each mode is
validated independently. Symbols are deduplicated across partial declarations.
Incremental source output compares each generated file by hint name and text so
unchanged files can be reused after a model or compilation edit.
Hint names encode punctuation to avoid collisions and invalid
generic names. Validation.HasUnsupportedShape is shared by generator and analyzer;
unsupported shapes are diagnosed with UV004 and skipped by code generation.
Report unsupported members at their declarations and file-local models at the
declaration that uses `file`. UV005 warns when a copy policy on a copy model
cannot affect its generated copy. UV001 identifies the declaration that needs
`partial` and names the copy methods that will be generated.
Each active rule links to its own heading in the consumer README. Include the
affected symbol and a concrete correction in diagnostic messages. UV002 also
reports the related type declaration when it is in the current compilation.
Check the generated public methods and graph helper against same-type names and
method signatures. Method generic arity and parameter passing mode distinguish
valid overloads. Existing explicit implementations of the emitted deep-copy
interface methods conflict only when declared on the same type.

Helper emits matching namespace/containing-type/generic partial wrappers, classifies
immutable state, finds stored instance fields and auto-properties, and matches
safe primary-constructor arguments to stored state. Static, computed, and indexer
properties are not independently stored state. Explicit backing fields are copied
directly.

Deep copies diagnose explicit-interface auto-properties with UV004. An explicit
interface property implemented over an ordinary field uses the existing field
copy path. Shallow copying uses the usual whole-instance behavior.

StorageAccess selects a typed UnsafeAccessor field backend per consumer
Compilation: .NET 8+ for non-generic declaring types, .NET 9+ for generic declaring
types or generic enclosing types. Never raise the generator's own target or Roslyn
version to enable it. Unsupported older targets retain UV004 for unsafe override
chains. Inspect complete type/property hierarchies, including non-opted-in
intermediate classes. GetCopiedState must retain distinct virtual property slots
with equal names instead of deduplicating their backing storage.

Generated helper classes match the owning type's flattened generic parameter order
and constraints; do not place a generic field stub in an unrelated generic type
or specialize the stub to a closed generic owner. Helpers for inaccessible nested
owners live in partial enclosing types. Emit headers/usings only once per file.
Diagnose unsafe helper hosting or user-name collisions rather than emitting invalid
C#. Copy primary-constructor arguments through the same physical state access plan.

Graph copy constructors with physical own-property storage carry
GeneratedCode("UtilityVerse.Copy.PhysicalState", "1") as a capability marker. Check
this marker before trusting an external base to preserve overridden storage;
legacy bases must be rebuilt for their supported consumer target. Metadata may
omit private fields, so compiler-generated getter attributes identify stored
properties during validation. Do not mistake a base-type cast for nonvirtual
access. No runtime model reflection, dynamic IL, or reflection fallback is allowed.

DeepCopyGenerator emits a typed copy constructor, a public parameterless DeepCopy(),
an explicit IDeepCopyable implementation, and a virtual helper for class hierarchies.
The original implicit default constructor is preserved when adding generated ones.
The copy constructor bypasses ordinary class constructors, chains to the generated
base copy constructor or an accessible parameterless base constructor, registers
the destination, and hydrates stored state. Primary constructors can only
receive matching stored immutable values without mutable references; primary
constructors with generated deep bases are rejected for ordinary classes.
Non-positional records can chain to an opted-in base graph constructor. Other
supported reference records verify their synthesized constructor chain in source
and hydrate accessible inherited state. User-defined record copy constructors
are rejected.
Structs without primary constructors use this() initialization. Field initializers
and non-opted-in base constructors can still run. Generated base copy constructors
preserve inherited private/readonly state. Init-only/get-only properties and
readonly fields can be assigned in these constructors.

CopyContext uses reference equality to preserve aliases and mutable cycles.
CopyArray, CopySequence, CopyMap, and CopyImmutable implement container registration
and typed element delegates. Nonempty hash sets and maps stage copied entries in
typed buffers and populate only after the outermost operation hydrates object
state. Lookup validation rebuilds invalid standard hash containers in place once;
dependencies that remain unstable fail explicitly. CopyOperation is a readonly
struct scope with a shared depth/success marker, so nested calls do not allocate
scope objects. Pending work is allocated lazily; map entries are value-type
KeyValuePair values, not per-entry work objects. Keep the context itself a reference
type because graph identity and nesting state must be shared.

CopyMap checks the factory's result against the source's actual runtime type
before registering it or copying entries. Generated factories for standard maps
therefore reject unsupported subclasses. Custom factories can preserve a
subclass and are responsible for its additional state.

Generated IDeepCopyable.Copy routes through CopyContext.CopyGenerated with a static
factory so direct interface calls also establish a complete graph operation. Keep
existing public context signatures intact. Failed operations clear cached partial
destinations and pending work; suppressed nested failures poison the operation.
Collection comparers and queue/stack ordering are
preserved. Binding lists retain their editing flags and effective AllowNew setting.
Populate them through their normal insertion API to establish fresh item change
wiring. External subscribers and pending add-new transactions belong to the source.
Immutable tuple cycles are rejected explicitly. Generic parameter values
can use runtime dispatch to built-in sequences and arrays with registered element
types. Scalars and generated IDeepCopyable values continue to use their own paths.

Vector arrays use typed element access. Other array shapes clone the runtime
array, register it before copying children, and traverse the original bounds
with reusable index buffers. Generated typed array members still validate their
declared element types. System.Array members require registered runtime elements.
Generated arrays with only shareable element values clone the container directly.
Direct struct copy methods run a typed factory in a graph operation without boxing
the root. Keep the boxed-interface paths separate so they can preserve identity.

Sorted maps and sets defer insertion until the graph is hydrated. Dictionary
interface copies dispatch to supported concrete maps and preserve comparers.
Immutable and frozen reference collections use CopyImmutable, which explicitly
rejects cycles. Their keys or set elements must be supported immutable scalars.

Stored members can declare Copy, Share, Reset, or Custom. Custom selects an exact
runtime type registered in a weak external copier table. External copiers register
reference destinations early when supporting cycles. Keep these policies out of
primary-constructor argument handling unless the default policy applies.

Bound nested copy operations and check stack space. A rejected depth must poison
the active graph even if a custom copier catches the exception.

For reference-typed inputs, CopyContext.Copy tracks the original box of an
opted-in struct. Value-typed inputs have no source box to track. If the consumer
exposes Unsafe.Unbox, generated IBoxedDeepCopyable copiers register a fresh box
before copying children and write the constructed struct into it before hash
collection hydration. Never mutate the source box. Older generated copiers and
targets without Unbox cache completed boxes and reject reentry into an unfinished
box. Use CopyContext.Copy for boxed roots, since a direct IDeepCopyable.Copy call
on a struct receives its value. Keep IDeepCopyable compatible with old callers.

Covariant sequence copying dispatches to the actual supported container element
signature before allocating or registering a destination. CopyContext.Sequences
keeps weak type/copier caches so collectible model assemblies are not rooted.
Only collection type identity metadata is inspected; no model-member reflection,
MakeGenericMethod, reflection invocation, or dynamic IL is permitted. Exact generic
and array paths remain typed. Element adapters are readonly structs passed through
constrained generic methods; preserve caller delegates, set hydration, ordering,
and comparers. Unsupported collection subclasses must fail explicitly.

CopyContext snapshots collection wrappers only when their public synchronization
root identifies a built-in list or array. Track root identity to reject backing
stores shared with another copied collection in either traversal order. Never
use the synchronization root as a replacement for the immediate backing list.
Opaque roots and nonresizable backing for mutable wrappers require a custom copier.

SequenceRegistration gathers declared element types across the complete model
hierarchy and accessible closed non-generic opted-in source models assignable to
those views. Generated lazy static registration runs before CopyGenerated enters
any graph constructor. Reserve the registration member names in validation. Closed
generic elements explicitly present in declared state are registered too. Hidden
runtime element types need public CopyContext.RegisterSequenceType<T>() before
copying; missing registration must throw even for empty collections, never fall
back to an incompatible List of the declared base element type. Immutable reference
scalars have built-in registration. Keep existing public context signatures intact.

ShallowCopyGenerator emits MemberwiseClone for reference types and return-this for
value types. Every reference, including arrays, is shared. Nested references do not
need shallow opt-ins; UV003 remains tracked but no longer fires for them.

## Supported boundary

Generated deep copies route Uri through CopyContext, even inside a struct,
tuple, or container. Only the exact runtime Uri type is immutable. Allow Uri
as a declared member without an opt-in, but do not classify a containing struct
as trivially copyable on that basis. Runtime subclasses must implement
IDeepCopyable or fail explicitly.

[README.md](README.md) documents the complete consumer contract and explicit
unsupported shapes. Do not silently skip inaccessible state, classify every struct as immutable, drop
constructor arguments, slice runtime derived types, or produce incompatible
collection assignments. New support must update classification, emission, validation,
consumer documentation, and tests together.

## Tests and checks

The test-first workflow is mandatory for Copy implementation work, including
review findings, bug fixes, new support, and performance changes.

1. Define the claim and the expected behavior before writing tests. Identify the
   relevant edge cases, failure paths, and existing behavior that must continue
   to work. For example, consider empty and null inputs, aliases, cycles,
   different traversal orders, and framework differences where they apply.
2. Write the full set of tests before touching the implementation. Run them on
   the current code and confirm that the new regression tests fail for the
   expected reason and that control cases pass. If the claim cannot be reproduced
   or the failure has a different cause, verify the premise before implementing
   a fix.
3. Once the tests establish the baseline, keep them unchanged. Implement the
   fix and rerun the same tests with the same inputs, assertions, and expected
   results. Never rewrite, weaken, skip, or remove a test to make it pass. Fix the
   implementation if verification fails. Run the relevant broader checks after
   the original tests pass.

- DeepCopyGenerationTests compiles original model scenarios and checks analyzer
  results, generated presence, and assembly emission.
- DeepCopyRuntimeTests and ShallowCopyRuntimeTests assert copying behavior.
- CopyModeTests covers dual opt-in across partial declarations and inherited
  markers, and independent validation of both modes.
- GenericSequenceCopyTests covers runtime generic sequences, registration,
  comparers, ordering, aliases, and cycles.
- ShapedArrayCopyTests covers nonzero bounds, multiple ranks, empty and null
  arrays, runtime views, aliases, cycles, and nested opt-in diagnostics.
- RecordInheritanceCopyTests covers generated base chaining, accessible positional
  state, private and readonly base state, and unsafe constructor boundaries.
- AdditionalCollectionCopyTests and ImmutableCollectionCopyTests cover the supported
  map interfaces, linked and sorted sequences, immutable and frozen collections,
  comparers, aliases, and rejection paths.
- CopyPolicyTests and CopyDepthTests cover member ownership, external copier
  registration, depth rejection, and failure recovery.
- CopyIncrementalityTests checks which source outputs Roslyn reuses after related
  and unrelated edits.
- ConstructorIsolationTests covers source isolation, constructor execution, readonly
  state, cycles, primary constructors, and unsupported construction paths.
- HashCollectionHydrationTests covers hash/equality dependencies on later state,
  collection bucket rebuilding, direct context/interface entry points, duplicate
  rejection, unstable hashing, and context recovery after failures.
- MapRuntimeTypeTests covers subclass rejection for both built-in map adapters,
  null and exact-type maps, comparers, aliases, cycles, and retry with a custom
  subclass factory in the same context.
- BoxedStructIdentityTests covers shared and distinct boxes, typed and nullable
  struct values, readonly box cycles with deferred hash keys, and the fallback
  compiled against .NET Standard 2.0.
- UriRuntimeTypeTests covers subclass rejection through direct, generic runtime,
  and nested copy paths, custom copier dispatch with aliases and cycles, exact
  and null Uri values, and the opt-in diagnostic for a struct storing Uri state.
- BindingListCopyTests covers settings on direct and covariant copies, default
  flags, and the external-subscriber policy.
- CollectionWrapperCopyTests covers shared list, array, and wrapper backing
  stores, covariant views, opaque roots, fixed-size backing, and context recovery.
- ExplicitInterfaceCopyTests covers UV004 for explicit-interface auto-properties,
  deep copies of ordinary backing fields, and shallow copies of the same shape.
- CopyMemberCollisionTests covers declaration-space and signature collisions,
  explicit copier conflicts, and valid generic and by-reference overloads.
- VirtualPropertyStorageTests covers independent virtual slots, computed overrides,
  generic bases, cross-assembly metadata, and virtual/abstract/explicit-field/shallow
  behavior. PhysicalStorageAccessTests executes readonly aliases, nested generic
  constraints, private/protected owners, primary constructors, external bases, and
  .NET 8/.NET 9 capability selection. FrameworkCompatibilityTests checks diagnostics
  on older targets. The fresh-package consumer executes field access on .NET 8 and
  generic field access on .NET 11.
- CovariantCollectionTests covers both traversal orders for supported sequence
  implementations, empty/null-only lists, nested and inherited generic views,
  custom element delegates, comparers, explicit registration, failures/recovery,
  collection subclass rejection, and registration-name diagnostics. Installed
  package consumers exercise covariant aliases on .NET 8 and .NET 11.
- CopyRegressionTests replaces the old bug-pinning KnownIssuesTests with compilation,
  analyzer, and runtime regressions for fixed issues, plus unsupported-shape checks.
- ValidationAnalyzerTests covers positive and negative diagnostics.
- FrameworkCompatibilityTests compiles against netstandard2.0 and net6.0 references.
- GeneratorRun.AnalyzerErrors runs the analyzer on the final generated compilation.
  Validation must distinguish generated members from user members.
- scripts/verify-packages.sh verifies Release packing and a fresh installed consumer.

Run the repository build script for full validation. Add new diagnostic descriptors
to Rules, SupportedDiagnostics, and AnalyzerReleases.Unshipped.md; never reuse a
shipped diagnostic ID. Change source generator inputs rather than generated outputs.
Do not bump package versions or move unshipped rules into shipped releases unless
preparing an explicitly requested release.

## Review backlog

Track the numbered correctness findings and remaining support/optimization work in
[review-backlog.md](review-backlog.md). Keep its original finding numbers stable
across follow-up work. Update the relevant finding and validation evidence as work
is completed; do not treat one completed finding as completion of the whole goal.
