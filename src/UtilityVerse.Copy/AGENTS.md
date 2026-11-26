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
interfaces. Deep wins when both modes are selected. Symbols are deduplicated across
partial declarations. Hint names encode punctuation to avoid collisions and invalid
generic names. Validation.HasUnsupportedShape is shared by generator and analyzer;
unsupported shapes are diagnosed with UV004 and skipped by code generation.

Helper emits matching namespace/containing-type/generic partial wrappers, classifies
immutable state, finds stored instance fields and auto-properties, and matches
safe primary-constructor arguments to stored state. Static, computed, and indexer
properties are not independently stored state. Explicit backing fields are copied
directly.

DeepCopyGenerator emits a typed copy constructor, a public parameterless DeepCopy(),
an explicit IDeepCopyable implementation, and a virtual helper for class hierarchies.
The original implicit default constructor is preserved when adding generated ones.
The copy constructor bypasses ordinary class constructors, chains to the generated
base copy constructor or an accessible parameterless base constructor, registers
the destination, and hydrates stored state. Primary constructors can only
receive matching stored immutable values without mutable references; primary
constructors with generated deep bases are rejected. Reference records use only
synthesized record copy constructors; user-defined copy constructors are rejected.
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

Generated IDeepCopyable.Copy routes through CopyContext.CopyGenerated with a static
factory so direct interface calls also establish a complete graph operation. Keep
existing public context signatures intact. Failed operations clear cached partial
destinations and pending work; suppressed nested failures poison the operation.
Collection comparers and queue/stack ordering are
preserved. Immutable tuple cycles are rejected explicitly. Generic parameter values
must be supported immutable scalars or implement generated IDeepCopyable.

ShallowCopyGenerator emits MemberwiseClone for reference types and return-this for
value types. Every reference, including arrays, is shared. Nested references do not
need shallow opt-ins; UV003 remains tracked but no longer fires for them.

## Supported boundary

[README.md](README.md) documents the complete consumer contract and explicit
unsupported shapes. Do not silently skip inaccessible state, classify every struct as immutable, drop
constructor arguments, slice runtime derived types, or produce incompatible
collection assignments. New support must update classification, emission, validation,
consumer documentation, and tests together.

## Tests and checks

- DeepCopyGenerationTests compiles original model scenarios and checks analyzer
  results, generated presence, and assembly emission.
- DeepCopyRuntimeTests and ShallowCopyRuntimeTests assert copying behavior.
- ConstructorIsolationTests covers source isolation, constructor execution, readonly
  state, cycles, primary constructors, and unsupported construction paths.
- HashCollectionHydrationTests covers hash/equality dependencies on later state,
  collection bucket rebuilding, direct context/interface entry points, duplicate
  rejection, unstable hashing, and context recovery after failures.
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
