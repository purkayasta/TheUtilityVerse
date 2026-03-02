# UtilityVerse.Copy — Unit Tests

xUnit test suite for the `UtilityVerse.Copy` source generator covering both `[DeepCopy]` and `[ShallowCopy]` attributes.

---

## Project setup

The Copy library must be referenced **twice** in the `.csproj` — once as a runtime library and once as a compile-time source generator:

```xml
<!-- Runtime: attribute types + generated methods -->
<ProjectReference Include="..\..\src\UtilityVerse.Copy\UtilityVerse.Copy.csproj" />

<!-- Compile-time: runs the source generator -->
<ProjectReference Include="..\..\src\UtilityVerse.Copy\UtilityVerse.Copy.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Without the `Analyzer` entry the generator never runs and `DeepCopy()` / `ShallowCopy()` methods do not exist at compile time.

---

## Files

| File | Purpose |
|---|---|
| [`Fixtures.cs`](Fixtures.cs) | All partial class models decorated with copy attributes |
| [`DeepCopyTest.cs`](DeepCopyTest.cs) | 14 tests covering `[DeepCopy]` behaviour |
| [`ShallowCopyTest.cs`](ShallowCopyTest.cs) | 14 tests covering `[ShallowCopy]` behaviour |
| [`Usings.cs`](Usings.cs) | Global usings (`xunit`, `UtilityVerse.Copy`, `Fixtures`) |

---

## Test fixtures

Defined in [`Fixtures.cs`](Fixtures.cs). Each model has an explicit depth so the expected behaviour inside its tests is unambiguous.

### DeepCopy models

| Class | Attribute | Role |
|---|---|---|
| `Order` | `[DeepCopy(3)]` | Root entity — primitives, nested object, list of complex, list of strings |
| `Address` | `[DeepCopy]` | Nested type inside `Order.ShippingAddress` |
| `LineItem` | `[DeepCopy]` | Element type inside `Order.Items` |
| `ChainNode` | `[DeepCopy(2)]` | Self-referential node used to test depth truncation |

### ShallowCopy models

| Class | Attribute | Role |
|---|---|---|
| `Invoice` | `[ShallowCopy(1)]` | Depth=1 — nested object and list elements are **shared references** |
| `Receipt` | `[ShallowCopy(2)]` | Depth=2 — nested object is a **new instance**, list elements are new instances |
| `ShallowAddress` | `[ShallowCopy]` | Nested address type used by both `Invoice` and `Receipt` |
| `ShallowLineItem` | `[ShallowCopy]` | Element type used in collection tests |

---

## DeepCopy tests

Source: [`DeepCopyTest.cs`](DeepCopyTest.cs) — **14 tests**

### Primitive & value-type properties

| Test | What it verifies |
|---|---|
| `DeepCopy_PrimitiveValues_AreCopiedCorrectly` | `int`, `string`, `Guid`, `DateTime` values are equal after copy |

### Reference independence

| Test | What it verifies |
|---|---|
| `DeepCopy_Root_IsDifferentInstance` | The cloned root object is a new heap instance |
| `DeepCopy_NestedObject_IsDifferentInstance` | A nested complex object (`Address`) is a new instance |
| `DeepCopy_ListContainer_IsDifferentInstance` | The `List<T>` itself is a new instance, not the same reference |
| `DeepCopy_ListElements_AreDifferentInstances` | Each element inside the list is a new instance |

### Mutation isolation

| Test | What it verifies |
|---|---|
| `DeepCopy_MutatingClonePrimitive_DoesNotAffectOriginal` | Changing `clone.Id` / `clone.CustomerName` leaves original unchanged |
| `DeepCopy_MutatingCloneNestedObject_DoesNotAffectOriginal` | Changing `clone.ShippingAddress.Street` leaves original's address unchanged |
| `DeepCopy_MutatingCloneListElement_DoesNotAffectOriginal` | Changing a property on a cloned list element leaves the original element unchanged |

### Primitive collections

| Test | What it verifies |
|---|---|
| `DeepCopy_PrimitiveList_ContainsCorrectValues` | `List<string>` values are equal and the list itself is a new instance |

### Null safety

| Test | What it verifies |
|---|---|
| `DeepCopy_NullNestedObject_RemainsNull` | A `null` nested object property stays `null` in the clone |
| `DeepCopy_NullList_RemainsNull` | A `null` list property stays `null` in the clone |

### Depth enforcement

Uses `ChainNode` with `[DeepCopy(2)]`: levels 1 and 2 are fully deep-copied; level 2's `Next` is `null` because the guard `currentDepth < maxDepth` prevents recursion beyond the configured depth.

| Test | What it verifies |
|---|---|
| `DeepCopy_Chain_FullyCopiesWithinConfiguredDepth` | Levels 1 and 2 both exist with correct values and are distinct instances |
| `DeepCopy_Chain_TruncatesAtConfiguredDepth` | Level 2's `Next` is `null` — level 3 is never visited |
| `DeepCopy_Chain_NoSharedReferences_AtDepthBoundary` | The last copied node is a new instance, not the original object |

---

## ShallowCopy tests

Source: [`ShallowCopyTest.cs`](ShallowCopyTest.cs) — **14 tests**

The tests are split across two fixture types to contrast the two depth behaviours side-by-side.

### Primitive & value-type properties

Always copied regardless of depth — `MemberwiseClone` handles this.

| Test | Fixture | What it verifies |
|---|---|---|
| `ShallowCopy_PrimitiveValues_AreCopiedCorrectly` | `Invoice` | `int` and `string` values are equal after copy |
| `ShallowCopy_MutatingClonePrimitive_DoesNotAffectOriginal` | `Invoice` | Changing `clone.Id` / `clone.Number` leaves original unchanged |

### Root reference

| Test | Fixture | What it verifies |
|---|---|---|
| `ShallowCopy_Root_IsDifferentInstance` | `Invoice` | The cloned root object is a new heap instance |

### depth=1 — at the depth boundary (shared references)

`Invoice` has `[ShallowCopy(1)]`. At depth=1 the generator emits `this.Prop` for nested objects and `this.List?.ToList()` for collections, preserving references to the original elements.

| Test | What it verifies |
|---|---|
| `ShallowCopy_NestedObject_IsSharedReference_AtDepthBoundary` | `clone.BillingAddress` and `original.BillingAddress` are the **same object** |
| `ShallowCopy_MutatingSharedNestedObject_AffectsBothCloneAndOriginal` | A mutation through the clone is also visible on the original (confirms sharing) |
| `ShallowCopy_ListContainer_IsNewInstance_EvenAtDepthBoundary` | The `List<T>` container is always a new instance — this is the shallow copy invariant |
| `ShallowCopy_ListElements_AreSharedReferences_AtDepthBoundary` | List elements are the **same objects** as the originals |

### depth=2 — within depth (new instances)

`Receipt` has `[ShallowCopy(2)]`. Within depth the generator calls `ShallowCopyInternal(currentDepth + 1, maxDepth)` on each complex property, producing new shallow-copied instances.

| Test | What it verifies |
|---|---|
| `ShallowCopy_NestedObject_IsNewInstance_WithinDepth` | `clone.BillingAddress` is a **different instance** from `original.BillingAddress` |
| `ShallowCopy_NestedObjectPrimitives_AreCopied_WithinDepth` | Primitive values on the newly created nested instance are correct |
| `ShallowCopy_MutatingNestedClonePrimitive_DoesNotAffectOriginal_WithinDepth` | Mutating a primitive on the cloned nested object does not affect the original |
| `ShallowCopy_ListElements_AreNewInstances_WithinDepth` | List elements are new shallow-copied instances, not shared references |
| `ShallowCopy_ListElementPrimitives_AreCopied_WithinDepth` | Primitive values on cloned list elements are correct |

### Null safety

| Test | Fixture | What it verifies |
|---|---|---|
| `ShallowCopy_NullNestedObject_RemainsNull` | `Invoice` | A `null` nested object stays `null` in the clone |
| `ShallowCopy_NullList_RemainsNull` | `Invoice` | A `null` list stays `null` in the clone |

---

## Running the tests

```bash
dotnet test test/UtilityVerse.Copy.UnitTest/UtilityVerse.Copy.UnitTest.csproj
```

Expected output:

```
Total tests: 28
     Passed: 28
 Total time: ~0.3s
```
