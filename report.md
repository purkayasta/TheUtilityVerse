# UtilityVerse.Copy — Review & Fix Report

_Branch: `add-depth` · Scope: the `UtilityVerse.Copy` source generator (DeepCopy / ShallowCopy)._

---

## 1. Expected behavior (the contract)

Before listing findings, here is the behavior the generator is **expected** to produce. This is the
contract the fixes below were measured against. Where the original code and the docs disagreed, the
resolution adopted is stated explicitly.

### 1.1 Copy semantics

- **`DeepCopy()`** returns a brand-new object graph. Every reference-typed property is recursively
  copied; primitives, `string`, and built-in value types (`Guid`, `DateTime`, `decimal`, …) are copied
  by value. Mutating the copy must never affect the original.
- **`ShallowCopy()`** returns a new root (via `MemberwiseClone`); reference-typed members beyond the
  configured depth are **shared** with the original.
- **Structs / record structs** are value types: returning them already produces an independent copy.

### 1.2 Depth

- `[DeepCopy(n)]` / `[ShallowCopy(n)]` set a **hard recursion cap** (`n`, default **5**, no upper bound).
- For an **acyclic** chain longer than `n`, recursion stops at level `n`: deep copy sets the next level
  to `null`; shallow copy shares the next level's reference.
- `depth = 0` ⇒ deep copy returns `null`; shallow copy returns a classic shallow clone (all complex
  members shared).
- A **negative** depth is a compile-time error (UV004).

### 1.3 Circular / shared references (the headline requirement)

This is the problem the branch set out to solve. **Adopted contract: identity preservation (Contract A).**

- Copying must **never** infinite-loop or `StackOverflow`, no matter how the object graph cycles.
- An **identity map** (keyed on **reference identity**) tracks already-copied objects. When an object is
  revisited, the **same copy instance** is returned. Therefore:
  - A genuine cycle in the original (`A → B → A`) is reproduced as a genuine cycle in the copy
    (`A' → B' → A'`), and recursion terminates when the cycle closes — usually **before** the depth cap.
  - The same instance referenced from two places is copied **once** and shared in the copy.
  - Two **distinct but value-equal** objects (e.g. records) are **two** distinct copies — value equality
    must not collapse them.
- The depth cap is the universal backstop that guarantees termination even where identity tracking
  cannot help (see §4 limitations).

> **Doc reconciliation:** the old attribute XML said _"depth=N copies N levels; level N+1 is set to
> null"_ without qualification. That is only true for **acyclic** graphs. For cycles, the identity map
> closes the loop first. The attribute docs were updated to say exactly this.

### 1.4 Supported declarations

- Public, `partial` `class` / `struct` / `record` / `record struct`, with a parameterless constructor
  **or** a constructor whose parameters map (by name, case-insensitive) onto readable properties.
- Get-only, init-only, and settable properties must all round-trip without data loss or compile errors.
- **Open generic** top-level types (`Box<T>`) are supported; an open type parameter `T` is opaque and
  copied as-is.
- **Nested** types are supported, provided every enclosing type is `partial` (else **UV009** error).

### 1.5 Diagnostics

`UV001` non-partial · `UV002/3` nested-property type missing copy opt-in · `UV004` negative depth ·
`UV006` non-public · `UV008` abstract · `UV009` enclosing type of a nested copyable type not `partial`.

---

## 2. What was fixed in this pass

All fixes below are implemented, covered by tests, and the full suite is green
(**72 / 72** in `UtilityVerse.Copy.UnitTest`, demo program exits 0).

### 2.1 Top-priority correctness bugs

| # | Issue | Fix | Test |
|---|-------|-----|------|
| **#2** | Identity map used **value equality** (`new Dictionary<object,object>()`), so distinct value-equal records collided and were wrongly merged. | Both generators now emit `new Dictionary<object,object>(ReferenceEqualityComparer.Instance)`. | `DeepCopy_DoesNotMergeValueEqualRecords_…`, `DeepCopy_PreservesSharing_WhenSameInstanceReferencedTwice` |
| **#3a** | **Get-only** properties (set only via constructor) were excluded from ctor-argument matching → silently passed `default(T)` (data loss). | Constructor arguments are now matched against `GetReadableProperties` (includes get-only). | `DeepCopy_CopiesGetOnlyProperties_WhenSuppliedThroughConstructor` |
| **#3b** | **Init-only** properties on a parameterless type generated post-construction `copy.X = …` → **CS8852**, generated code did not compile. | Init-only properties are emitted in an **object initializer**; settable properties stay as post-construction assignments (so cycle registration via the identity map is preserved). | `DeepCopy_CopiesInitOnlyProperties_OnParameterlessType` |
| **#3c** | Constructor-initialized types **never copied settable properties that were not constructor parameters**. | After construction (and identity-map registration) the generator now assigns settable, non-ctor properties. **Side benefit:** cycles flowing through a settable back-reference on a ctor type are now caught by the identity map. | `DeepCopy_CopiesSettableProperty_WhenNotSuppliedByConstructor`, `DeepCopy_PreservesCycle_WhenCtorTypeHasSettableBackReference` |
| **#4** | **Nested** and **open generic** types produced broken or no code. | Generic types are fully supported (type-parameter list emitted; `T` treated as opaque). Nested types are skipped by the generator and reported via new **UV009**. The hint-name is now sanitized so `Box<T>` no longer crashes the generator. | `DeepCopy_SupportsGenericType_AndTreatsTypeParameterAsOpaque` |

> **On the original "#1" (ctor-path cycles):** the identity map is registered *after* construction for
> ctor types, so a cycle through an **immutable constructor parameter** still cannot be reproduced
> faithfully (it is impossible to hand a not-yet-constructed object to its own constructor). In
> practice such cycles are also impossible to *build* for immutable types. The common, buildable case —
> a cycle through a **settable** back-reference — is now handled correctly by fix #3c. Termination for
> the residual immutable-cycle case is still guaranteed by the depth cap.

### 2.2 Smaller issues / cleanups

- **Redundant guard removed / reordered.** The deep-copy guard was `if (currentDepth < maxDepth && objectMap.TryGetValue(...))`.
  The `currentDepth < maxDepth` prefix was dead-but-confusing. The guard now reads identity-first:
  `if (objectMap.TryGetValue(...)) return existing;` then `if (currentDepth >= maxDepth) return null!;`.
  This is clearer and slightly more correct (identity wins over the depth cap).
- **`IsTrulyPrimitive` simplified.** The large `SpecialType` switch was entirely subsumed by
  `IsValueType` (plus `string`). Reduced to `symbol.IsValueType || symbol.SpecialType == System_String`
  — **behavior-preserving**, dead code removed.
- **Attribute XML docs corrected** to describe the identity-map cycle semantics (see §1.3 reconciliation).
- **Default-depth duplication** flagged with a sync comment on `Helper.DefaultDepth` (must match the
  attribute's `Depth = 5` default).
- **`DepthTest` demo made cycle-aware.** It previously produced a (correct) cyclic copy and then crashed
  its own naive printer with unbounded recursion. The printer now uses a reference-identity visited-set,
  verifies the cycle is preserved (`Single.Company == root`), and terminates cleanly.

### 2.3 Performance fixes (generator / compile-time)

The compile-time pipeline was the headline performance concern for a source generator. The pipeline in
`CopyGenerator.cs` was rewritten:

| Problem (before) | Fix (after) |
|------------------|-------------|
| **`ISymbol` flowed through the cached pipeline** (`CreateSyntaxProvider → Collect` carried `INamedTypeSymbol`). Symbols are not value-equatable and **root the `Compilation`** across edits — the canonical SG memory-leak / no-incrementality anti-pattern. | Each transform now produces an **equatable `(string HintName, string Source)`** value — generation runs inside the transform (which has semantic access) and only strings leave it. No symbol enters the cached pipeline, so the Compilation is not rooted and generation is cached per type, re-running only for types that actually changed. |
| **Predicate matched every type declaration**, then ran a semantic transform on all of them. | Attribute opt-in now uses **`ForAttributeWithMetadataName`** (Roslyn 4.3+, available at 4.14), which only visits types carrying `[DeepCopy]` / `[ShallowCopy]`. |
| **Interface opt-in** (`IDeepCopy`/`IShallowCopy`) had no efficient path. | A dedicated provider with a **cheap syntactic predicate** (`TypeDeclarationSyntax { BaseList: not null }`) keeps the semantic transform off the vast majority of types. |
| **`GroupBy((Symbol, Mode))`** used the default symbol comparer (RS1024) for de-duplication. | De-duplication is now done on the **hint-name string** (`HashSet<string>`), so no symbol comparison is involved. |

Behavior is identical — all existing tests pass — and two regression tests were added for the
(previously untested) interface opt-in path: `DeepCopy_WorksViaMarkerInterface`,
`ShallowCopy_WorksViaMarkerInterface`.

---

## 3. Runtime performance: evaluated, intentionally not changed

The two runtime ideas from the first review were investigated and **not** applied, because each would
break the §1 contract or the emission architecture. The reasoning is recorded here so the decision is
explicit rather than an omission.

### 3.1 Skipping the `Dictionary<object,object>` allocation — **unsafe, rejected**

The identity map inside a type's copy method is **not** only for that type's own recursion — it is what
lets a **parent** graph dedupe shared/cyclic instances of that type. Concretely,
`DeepCopy_PreservesSharing_WhenSameInstanceReferencedTwice` relies on `Tag` (a primitive-only record)
registering itself in the parent `Article`'s map so two references to the same `Tag` collapse to one
copy. Removing the map for "leaf" types would silently copy shared instances twice — a contract
violation (§1.3). Because any reference type can be shared by a parent, the allocation cannot be removed
based on local analysis; only a fragile whole-graph analysis could, which is not worth the risk.

### 3.2 Replacing LINQ collection copies with hand-rolled loops — **not surgical, rejected**

The copy of each property is emitted as a single **expression** (e.g.
`this.Items?.Select(x => x?.DeepCopyInternal(...)).ToList()`) because it must be usable as a
**constructor argument** and inside **object initializers** (`new T(<expr>)`, `new T { P = <expr> }`).
A pre-sized `foreach` loop is a statement, not an expression, so converting would require abandoning the
expression-based emission for ctor/initializer paths — a large, risky change for a modest allocation
saving. Primitive collections already use the minimal `ToList()` / `ToArray()` / `Clone()`.

> If a future change makes runtime allocation a priority, the right move is to emit dedicated private
> helper methods (statement bodies) and call them from the ctor-arg/initializer expressions — a deliberate
> architectural change, tracked separately.

---

## 3a. Nested-type support (added)

Nested types are now generated. The emitter wraps the generated members in a `partial` declaration for
each enclosing type (outermost first), reproducing any enclosing generic type parameters
(`partial class GenericHost<THost>`). Brace nesting is balanced; only cosmetic indentation is flat
(irrelevant to the compiler).

- **Requirement:** every enclosing type must be `partial`. If not, the type is skipped and **UV009**
  (now an *error*, repurposed from the old "nested not supported" warning) is reported:
  _"Type 'X' is nested inside non-partial type 'Y', which must be declared 'partial' …"_.
- Covered by tests: single-level nested, doubly-nested, and nested-in-generic (deep + shallow).

---

## 3b. Test-coverage audit

Code-coverage tools are not meaningful here: the generator runs at **compile time**, so runtime
coverage measures the *generated* code, not the generator's own branches. Coverage was therefore audited
against the generator's feature matrix. The audit found several **generator code paths with zero tests**;
the high-value ones were filled, and one uncovered path turned out to contain a **real bug**.

### Bug found and fixed: `Stack<T>` copies were reversed

`new Stack<T>(existingStack)` enumerates LIFO and pushes, so the copied stack came out **in reverse
order**. The existing Stack tests only used single-element stacks, so it was invisible. Fixed in both
`GenerateDeepCopyStackAssignment` and `GenerateShallowCopyStackAssignment` by reversing the source
(`.Reverse()`) before constructing, so the copy preserves the original top-to-bottom order. New tests
(`DeepCopy_CopiesQueueAndStack_…`, `ShallowCopy_PreservesStackOrder_…`) lock this in. `Queue<T>` was
verified correct (its constructor preserves order) — no change needed.

### Gaps now covered with new tests

| Path | Was tested? | Now |
|------|-------------|-----|
| Primitive & complex **arrays** (deep) | ❌ | ✅ |
| **HashSet / Queue / Stack** deep copy (multi-element) | ❌ (shallow only, single-element) | ✅ |
| **Dictionary** (primitive- and complex-valued) | ❌ | ✅ |
| **Value tuple** property | ❌ | ✅ |
| **Inheritance** (inherited base properties) | ❌ | ✅ |
| Marker-**interface** opt-in (`IDeepCopy`/`IShallowCopy`) | ❌ | ✅ |
| **Nested** / doubly-nested / nested-in-generic | ❌ | ✅ |

### Gaps still open (documented, not yet covered)

- **Analyzer diagnostics UV001–UV009 are entirely untested.** There is no analyzer test harness
  (e.g. `Microsoft.CodeAnalysis.Testing`). This is the largest remaining gap — every diagnostic's
  trigger condition and message is unverified. Recommend adding `*.Analyzer.Tests` with the Roslyn
  testing package.
- **Immutable collections** (`ImmutableList<T>`, `ImmutableArray<T>`, `ImmutableHashSet<T>`,
  `ImmutableDictionary<,>`, etc.) — mapped in `IsGenericCollection` but never exercised. They reuse the
  List/HashSet/Dictionary emission, but `ImmutableArray` is a struct and `Immutable*` are built via
  collection-initializer/`new`, so they deserve dedicated tests.
- **`IEnumerable<T>` / `IReadOnlyList<T>` / `ICollection<T>`** ("Enumerable" kind) properties — untested.
- **Nullable value tuple** (`(int, string)?`) — falls through to the default `DeepCopyInternal` path and
  would not compile; the tuple path only matches the non-nullable `ValueTuple`. Flagged as a latent bug
  (a non-nullable tuple is fine and is now tested).
- **`Tuple<...>` reference tuples** (vs `ValueTuple`) — the `System.Tuple` branch is untested.

---

## 4. Known limitations (by design, after the fixes)

These are inherent and are documented rather than fixed:

1. **Cycles through immutable constructor parameters** cannot be reproduced faithfully (you cannot pass a
   not-yet-built object to its own constructor). Termination is still guaranteed by the depth cap. Such
   cycles are generally impossible to construct for immutable types anyway.
2. **Cycles through init-only properties** on a parameterless type are likewise not closed by the
   identity map, because init-only values are computed inside the object initializer (before the copy is
   registered). Same fundamental constraint as #1.
3. **Open generic type parameters are opaque.** `Box<T>.Value` of type `T` is copied as-is (shared
   reference), because the generator cannot know whether `T` exposes a `DeepCopy` at generation time.
4. **Shallow copy of an init-only complex property** keeps the `MemberwiseClone`d (shared) reference even
   when within depth, since it cannot be reassigned. This is valid shallow-copy behavior but means the
   depth knob does not deepen those specific members.
5. **Nested types require every enclosing type to be `partial`** (the generated partial must nest in the
   same context). A non-partial enclosing type is reported as **UV009** and the type is skipped.

---

## 5. Verification

| Check | Result |
|-------|--------|
| `dotnet build src/UtilityVerse.Copy` | ✅ 0 errors, 0 warnings |
| `dotnet test test/UtilityVerse.Copy.UnitTest` | ✅ **88 / 88 passed** (65 original + 23 new regression tests) |
| `dotnet run test/UtilityVerse.Copy.Test` (demo) | ✅ exits 0; cycle preserved (`Single.Company == root`), printer terminates |
| Generated code inspected for `Box<T>`, `InitPerson`, `OrderWithExtra`, `CtorFriend`, nested + nested-in-generic | ✅ matches intended output |

### New regression tests added

- `DeepCopy_DoesNotMergeValueEqualRecords_WhenTheyAreDistinctInstances`
- `DeepCopy_PreservesSharing_WhenSameInstanceReferencedTwice`
- `DeepCopy_CopiesGetOnlyProperties_WhenSuppliedThroughConstructor`
- `DeepCopy_CopiesInitOnlyProperties_OnParameterlessType`
- `DeepCopy_CopiesSettableProperty_WhenNotSuppliedByConstructor`
- `DeepCopy_PreservesCycle_WhenCtorTypeHasSettableBackReference`
- `DeepCopy_SupportsGenericType_AndTreatsTypeParameterAsOpaque`
- `DeepCopy_WorksViaMarkerInterface`, `ShallowCopy_WorksViaMarkerInterface` (interface opt-in)
- Nested types: `DeepCopy_WorksForNestedType`, `DeepCopy_WorksForDoublyNestedType`,
  `DeepCopy_WorksForTypeNestedInGenericHost`, `ShallowCopy_WorksForNestedType`
- Collection/array/dictionary/tuple/inheritance coverage: `DeepCopy_CopiesPrimitiveArray_…`,
  `DeepCopy_CopiesComplexArray_…`, `DeepCopy_CopiesPrimitiveHashSet_…`, `DeepCopy_CopiesComplexHashSet_…`,
  `DeepCopy_CopiesQueueAndStack_…`, `DeepCopy_CopiesPrimitiveValuedDictionary_…`,
  `DeepCopy_CopiesComplexValuedDictionary_…`, `DeepCopy_CopiesValueTupleProperty`,
  `DeepCopy_CopiesInheritedProperties`, `ShallowCopy_PreservesStackOrder_…`

---

## 6. Files changed

**Generator (`src/UtilityVerse.Copy`)**
- `CopyGenerator.cs` — **pipeline rewrite** (see §2.3): `ForAttributeWithMetadataName` for attributes,
  cheap-predicate provider for interfaces, equatable `(hintName, source)` strings out of the transform,
  hint-name de-duplication. Hint-name sanitization for generic types retained.
- `Helper.cs` — simplified `IsTrulyPrimitive`; added `GetReadableProperties`,
  `GetTypeNameWithTypeParameters`, `GetContainingTypeChain`, `EnclosingTypesAreGeneratable`,
  `AppendEnclosingTypesOpen/Close`; type-parameter handling in both assignment generators; identity-first
  depth guard; default-depth sync comment; **`Stack` copy order fix (`.Reverse()`) in deep + shallow**.
- `Generators/DeepCopyGenerator.cs` — reference-equality map; generic name; **nested-type support**;
  readable-property ctor matching; init-only object-initializer partition; settable-non-ctor property copying.
- `Generators/ShallowCopyGenerator.cs` — reference-equality map; generic name; **nested-type support**;
  init-only override skip.
- `CopyGenerator.cs` — hint-name sanitization for generic types.
- `Analyzers/Rules.cs`, `Analyzers/ValidationAnalyzer.cs`, `AnalyzerReleases.Unshipped.md` — UV009
  repurposed to "enclosing type must be partial" (now an error).
- `Attributes/DeepCopy.cs` — accurate cycle/depth docs.

**Tests**
- `test/UtilityVerse.Copy.UnitTest/Fixtures.cs` — 8 new fixtures (incl. interface opt-in).
- `test/UtilityVerse.Copy.UnitTest/DeepCopyTest.cs` — 8 new tests.
- `test/UtilityVerse.Copy.UnitTest/ShallowCopyTest.cs` — 1 new interface-opt-in test.
- `test/UtilityVerse.Copy.Test/DepthTest.cs` — cycle-aware demo.
