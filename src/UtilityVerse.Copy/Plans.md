# UtilityVerse.Copy — Project Plans & Status

> A Roslyn-based C# source generator for automatic `DeepCopy()` and `ShallowCopy()` method generation with configurable depth control and circular reference handling.

**Last Updated:** March 2026
**Current Test Status:** ✅ 41/41 tests passing

---

## 📋 Table of Contents

1. [Completed Work](#-completed-work)
2. [Known Limitations & Edge Cases](#-known-limitations--edge-cases)
3. [Planned Improvements](#-planned-improvements)
4. [Architecture Notes](#-architecture-notes)

---

## ✅ Completed Work

### **Phase 1: Core Generator Implementation**

#### DeepCopy & ShallowCopy Generators
- ✅ Roslyn incremental source generator (`IIncrementalGenerator`)
- ✅ `[DeepCopy]` attribute with configurable depth (1-10)
- ✅ `[ShallowCopy]` attribute with configurable depth (1-10)
- ✅ 1-indexed depth semantics (depth=N means N full levels)
- ✅ Partial class requirement validation
- ✅ Abstract class filtering

#### Type Support
- ✅ **Primitives:** int, string, bool, char, byte, etc.
- ✅ **Built-in types:** DateTime, DateOnly, Guid, TimeSpan, Decimal, Uri, Version, BigInteger, etc.
- ✅ **Arrays:** Single-dimension T[]
- ✅ **Collections:**
  - List<T>, IList<T>, Collection<T>, ReadOnlyCollection<T>, ObservableCollection<T>
  - BindingList<T>, ImmutableList<T>, ReadOnlyList<T>
  - HashSet<T>, ImmutableHashSet<T>, ImmutableSortedSet<T>
  - Dictionary<TKey, TValue>, FrozenDictionary<TKey, TValue>, ConcurrentDictionary<TKey, TValue>
  - ImmutableDictionary<TKey, TValue>, ImmutableSortedDictionary<TKey, TValue>
  - Queue<T>, Stack<T>
  - IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
- ✅ **Tuples:** System.Tuple<...>, ValueTuple<...>
- ✅ **Generic Types:** Custom classes with [DeepCopy]/[ShallowCopy] attributes
- ✅ **Nullable Types:** Nullable<T>, T?

#### Depth Control & Boundary Semantics
- ✅ Depth parameter on attributes (DEFAULT=5, MAX=10)
- ✅ **DeepCopy Boundary:** Complex properties → `null` at depth boundary
- ✅ **ShallowCopy Boundary:** Complex properties → shared reference (original) at depth boundary
- ✅ Primitive collections always cloned (new container)
- ✅ Complex collection elements follow depth rules

#### Constructor Support
- ✅ Parameterless constructors (object initializer syntax)
- ✅ Non-parameterless constructors (parameter mapping)
- ✅ Property lookup caching (O(n) instead of O(n×m))
- ✅ Record types with auto-generated constructors

#### Circular Reference Handling
- ✅ **Identity Map pattern** (Dictionary<object, object>)
- ✅ Check map before copying (returns cached copy)
- ✅ Register object BEFORE setting properties (prevents infinite loops)
- ✅ Pass objectMap through all recursive calls
- ✅ Referential equality preserved (A'.Friend.Friend == A')
- ✅ Self-references work correctly (A'.Friend == A')
- ✅ Mutual circular references (A ↔ B)
- ✅ Complex cycles (A → B → C → A)

### **Phase 2: Code Quality Improvements**

#### Final Code Review Fixes
- ✅ Consistent return values (`null` instead of mixed `null`/`string.Empty`)
- ✅ No wasted StringBuilder work (constructor validation before allocation)
- ✅ O(n×m) → O(n+m) LINQ optimization (cached property dictionary)
- ✅ Removed duplicate Dictionary handling block
- ✅ Queue & Stack collection support added
- ✅ Removed unused `IsInsideCopyEnabledPartialParent()` method
- ✅ Made `Depth` property `public` on both attributes

#### Code Consolidation
- ✅ 200+ lines of duplicated logic extracted to Helper.cs
- ✅ `GenerateDeepCopyAssignment()` — Centralized deep-copy assignment logic
- ✅ `GenerateShallowCopyAssignment()` — Centralized shallow-copy assignment logic
- ✅ Helper methods for each collection type (HashSet, Dictionary, Queue, Stack, List, etc.)
- ✅ Generator code reduced from 300+ lines to ~120 lines (via Helper delegation)
- ✅ DRY principle applied across both generators

### **Phase 3: Test Coverage**

#### Original Test Suite (28 tests)
- ✅ **DeepCopyTest.cs** — 14 tests
  - Primitive values copy correctly
  - Root, nested objects, list containers are different instances
  - Mutation isolation (changes don't affect original)
  - Primitive collections (List<string>, etc.)
  - Null safety
  - Depth enforcement & truncation
- ✅ **ShallowCopyTest.cs** — 14 tests
  - Primitive values copy correctly
  - Root is different instance
  - Depth=1: shared references at boundary
  - Depth=2: new instances within depth
  - Null safety

#### Constructor Initialization Tests (5 tests)
- ✅ Record type constructor initialization
- ✅ Class with non-parameterless constructor
- ✅ Nested objects via constructor
- ✅ Collections via constructor
- ✅ Null nested properties

#### Circular Reference Tests (8 tests)
- ✅ Mutual circular references (A ↔ B) — no stack overflow
- ✅ Referential integrity preserved
- ✅ Self-references (A → A) handled correctly
- ✅ All copies are distinct instances
- ✅ ShallowCopy boundary semantics with cycles
- ✅ Complex cycles (3+ objects)

#### Test Infrastructure
- ✅ **Fixtures.cs** — 8 test classes with explicit depth settings
  - DeepCopy: Order, Address, LineItem, ChainNode, ProductRecord, ConstructorInitializedOrder
  - ShallowCopy: Invoice, Receipt, ShallowAddress, ShallowLineItem, ShallowFriend
  - Circular: Friend, ShallowFriend
- ✅ **Double project reference** setup (library + analyzer)
- ✅ **README.md** in test project documenting all test cases

### **Phase 4: Documentation**

#### Generated Code Documentation
- ✅ Auto-generated comment headers on all generated methods
- ✅ `[System.CodeDom.Compiler.GeneratedCode]` attributes
- ✅ Line comments explaining circular reference checks

#### Test Documentation
- ✅ **README.md** in test project with:
  - Project setup instructions (double reference requirement)
  - Fixture descriptions with depth configurations
  - Complete test catalog (28 + 8 = 36 documented tests)
  - Expected output format
  - Run instructions

#### Code Comments
- ✅ Boundary semantics comments in generators
- ✅ Collection handling documentation
- ✅ Generic type handling notes

---

## ⚠️ Known Limitations & Edge Cases

### **Unsupported Type Scenarios**

| Feature | Support | Notes |
|---------|---------|-------|
| **Jagged Arrays** (T[][]) | ❌ Not Tested | Would fail - generator assumes 1D arrays |
| **Multi-Dimensional Arrays** (T[,], T[,,]) | ❌ Not Tested | Would fail - IArrayTypeSymbol handling assumes 1D |
| **Get-Only Properties** | ⚠️ Partial | Properties skipped (no SetMethod) |
| **Init-Only Properties** | ⚠️ Unknown | May work in constructor path, untested |
| **Generic Type Parameters** | ❌ No | `class Wrapper<T> { T Value }` — can't assume T has DeepCopy |
| **Custom Collections** | ⚠️ Fallback | Types not in IsGenericCollection() → call DeepCopyInternal |
| **Span<T>** | ⚠️ N/A | Stack-only type, can't be stored in fields |
| **ReadOnlySpan<T>** | ⚠️ N/A | Stack-only type, can't be stored in fields |
| **IQueryable<T>** | ❌ No | LINQ query results not supported |
| **Lazy<T>** | ❌ No | Not handled explicitly |
| **WeakReference<T>** | ❌ No | Not handled explicitly |
| **Indexers/Index Properties** | ❌ No | Not supported |
| **Events** | ✅ Skipped | Correctly ignored (not properties) |
| **Delegates** | ✅ Skipped | Correctly ignored (not properties) |
| **Static Properties** | ✅ Skipped | By design (should be shared) |
| **Abstract Classes** | ✅ Filtered | Correctly rejected at generator start |

### **Performance Considerations**

| Scenario | Status | Notes |
|----------|--------|-------|
| **Large Collections** | ⚠️ Unknown | Not tested with 100K+ elements |
| **Very Deep Nesting** | ✅ Handled | Depth=10 max prevents runaway recursion |
| **Many Properties** | ⚠️ Unknown | Not stress-tested with 50+ properties |
| **Dictionary with Complex Keys** | ⚠️ Unknown | Keys are deep-copied, behavior untested |
| **Circular Refs with Large Graphs** | ⚠️ Unknown | Identity map overhead not profiled |

### **Edge Cases Untested**

| Case | Impact | Details |
|------|--------|---------|
| **Nullable<T> (System.Nullable)** | 🟠 Low | Handled by null-coalescing, but edge cases untested |
| **Complex Tuples** (nested ValueTuple) | 🟠 Low | Tuple handling works, but complex nesting untested |
| **Dictionary with Null Keys** | 🟠 Low | May cause issues with deep copy |
| **Collections with Null Elements** | 🟠 Low | Should work (null-coalescing), untested |
| **Empty Collections** | 🟠 Low | Should create new empty containers, untested |
| **Concurrent Collection Modifications** | 🔴 High | NOT thread-safe, could corrupt data |
| **Properties with Custom Setters** | 🟠 Low | Side effects not guaranteed to run |
| **Mixed Primitive/Complex Arrays** | 🟢 Low | Should work (per-element type checking) |

---

## 📋 Planned Improvements

### **High Priority (Should Do)**

#### 1. Support Jagged & Multi-Dimensional Arrays
**Impact:** 🔴 High | **Effort:** 🟡 Medium
**Description:** Arrays like `int[][]` and `int[,]` currently unsupported.

**Tasks:**
- [ ] Detect jagged arrays in generator
- [ ] Detect multi-dimensional arrays
- [ ] Generate appropriate `.Clone()` or rebuild logic
- [ ] Add test cases for both array types
- [ ] Document limitations (if any remain)

**Estimated Effort:** 2-3 hours

---

#### 2. Support Init-Only Properties (Modern C# Feature)
**Impact:** 🟡 Medium | **Effort:** 🟢 Low
**Description:** Properties with `{ get; init; }` should be copyable via constructor.

**Tasks:**
- [ ] Verify constructor path handles init-only properties
- [ ] Add test cases for init-only scenarios
- [ ] Document behavior
- [ ] Consider alternative syntax if constructor-only approach doesn't work

**Estimated Effort:** 1 hour

---

#### 3. Generic Type Parameter Support
**Impact:** 🟡 Medium | **Effort:** 🔴 High
**Description:** Support `class Wrapper<T> { T Value { get; set; } }` where T might have DeepCopy.

**Tasks:**
- [ ] Detect generic type parameters
- [ ] Generate code with runtime type checking (T is IDeepCopy?)
- [ ] OR: Document as unsupported and add validation error
- [ ] Add test cases if supported, or diagnostic message if not

**Estimated Effort:** 4-6 hours (high complexity)

---

#### 4. Thread Safety for Circular Reference Map
**Impact:** 🟡 Medium | **Effort:** 🟡 Medium
**Description:** Current identity map is not thread-safe for concurrent DeepCopy calls.

**Tasks:**
- [ ] Use `ConcurrentDictionary<object, object>` instead of `Dictionary<object, object>`
- [ ] OR: Document as not thread-safe (single-threaded use only)
- [ ] Add warning to generated code or attribute documentation
- [ ] Consider if thread-safety is truly needed

**Estimated Effort:** 1-2 hours

---

### **Medium Priority (Nice to Have)**

#### 5. Dictionary with Complex Keys
**Impact:** 🟠 Low | **Effort:** 🟡 Medium
**Description:** Keys should also be deep-copied, but currently may share references.

**Tasks:**
- [ ] Review current Dictionary handling
- [ ] Verify keys are deep-copied correctly
- [ ] Add test cases with complex keys (nested objects as keys)
- [ ] Fix if keys are not being cloned

**Estimated Effort:** 2-3 hours

---

#### 6. Custom Collection Type Support
**Impact:** 🟠 Low | **Effort:** 🟡 Medium
**Description:** Custom collection types fall through to generic DeepCopyInternal call.

**Tasks:**
- [ ] Identify common custom collection types
- [ ] Consider extending IsGenericCollection() or adding handler
- [ ] Add test cases for custom collections
- [ ] Document what's supported vs. not

**Estimated Effort:** 2-4 hours

---

#### 7. Performance Testing & Optimization
**Impact:** 🟠 Low | **Effort:** 🟡 Medium
**Description:** No performance baselines or stress tests exist.

**Tasks:**
- [ ] Create performance benchmark test
  - [ ] Large collections (10K, 100K, 1M elements)
  - [ ] Deep nesting (depth=10 with 50+ properties)
  - [ ] Circular reference graphs (100+ nodes)
- [ ] Profile identity map overhead
- [ ] Optimize if bottlenecks found
- [ ] Document performance characteristics

**Estimated Effort:** 3-4 hours

---

### **Low Priority (Nice Polish)**

#### 8. Better Error Messages & Diagnostics
**Impact:** 🟢 Low | **Effort:** 🟡 Medium
**Description:** Improve diagnostic messages for unsupported scenarios.

**Tasks:**
- [ ] Add diagnostic analyzer for unsupported types
- [ ] Provide actionable error messages
- [ ] Suggest workarounds
- [ ] Document all diagnostic codes

**Estimated Effort:** 2-3 hours

---

#### 9. Attribute Documentation
**Impact:** 🟢 Low | **Effort:** 🟢 Low
**Description:** Expand XML doc comments on attributes.

**Tasks:**
- [ ] Add detailed `<summary>` on [DeepCopy] and [ShallowCopy]
- [ ] Document depth parameter behavior
- [ ] Add `<example>` usage blocks
- [ ] Document unsupported scenarios

**Estimated Effort:** 1 hour

---

#### 10. Support for IDeepCopy & IShallowCopy Interfaces
**Impact:** 🟢 Low | **Effort:** 🟡 Medium
**Description:** Allow opting in via interfaces instead of just attributes.

**Tasks:**
- [ ] Define IDeepCopy { T DeepCopy(); }
- [ ] Define IShallowCopy { T ShallowCopy(); }
- [ ] Update generator to recognize interface implementation
- [ ] Add test cases
- [ ] Document interface-based approach

**Estimated Effort:** 2-3 hours

---

## 🏗️ Architecture Notes

### **Code Organization**

```
src/UtilityVerse.Copy/
├── Generators/
│   ├── DeepCopyGenerator.cs        (120 lines - simplified via Helper delegation)
│   ├── ShallowCopyGenerator.cs     (95 lines - simplified via Helper delegation)
│   └── CopyGenerator.cs            (IIncrementalGenerator entry point)
├── Helper.cs                        (~700 lines - centralized assignment logic)
├── Attributes/
│   ├── DeepCopy.cs                 (public int Depth property)
│   └── ShallowCopy.cs              (public int Depth property)
├── Analyzers/
│   ├── ValidationAnalyzer.cs       (Diagnostic validation)
│   └── Rules.cs                    (Diagnostic rule definitions)
└── Interfaces/ (Planned)
    ├── IDeepCopy.cs
    └── IShallowCopy.cs
```

### **Key Design Decisions**

1. **1-Indexed Depth:** `depth=N` means exactly N full levels (not 0-indexed), following domain semantics
2. **Identity Map for Circular References:** Preserves referential equality and handles cycles elegantly
3. **MemberwiseClone for ShallowCopy:** Base uses MemberwiseClone, then selectively overrides complex properties
4. **Boundary Semantics:**
   - DeepCopy: Sets to `null` at boundary
   - ShallowCopy: Shares reference at boundary
5. **Helper.cs Centralization:** All assignment generation logic in one place (DRY principle)
6. **No Constructor-Only Recursion:** Uses `currentDepth` parameter threading, not separate overloads

### **Performance Characteristics**

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| DeepCopy() call | O(n) | n = number of properties at all depths |
| ShallowCopy() call | O(n) | Same as DeepCopy (MemberwiseClone is O(1)) |
| Circular Ref Check | O(1) | Dictionary lookup in objectMap |
| Array Cloning | O(n) | n = array size |
| Collection Cloning | O(n) | n = collection size (plus element copy time) |
| **With Cycles** | O(n×d) | n = unique objects, d = avg depth |

---

## 📊 Test Coverage Matrix

| Category | DeepCopy | ShallowCopy | Constructors | Circular | Total |
|----------|----------|-------------|--------------|----------|-------|
| Primitives | ✅ | ✅ | ✅ | - | 3 |
| Collections | ✅ | ✅ | ✅ | ✅ | 10+ |
| Nested Objects | ✅ | ✅ | ✅ | ✅ | 8+ |
| Null Safety | ✅ | ✅ | ✅ | - | 4 |
| Constructors | - | - | ✅ | - | 3 |
| Circular Refs | ✅ | ✅ | - | ✅ | 8 |
| **Total** | **14** | **14** | **5** | **8** | **41** |

---

## 🎯 Success Criteria

- ✅ All 41 tests pass
- ✅ No compiler warnings
- ✅ Circular references handled correctly
- ✅ Code is DRY (no duplication between generators)
- ✅ Depth semantics correct (1-indexed)
- ⏳ (Planned) Jagged/multidimensional arrays supported
- ⏳ (Planned) Performance benchmarks established
- ⏳ (Planned) Thread safety verified or documented

---

## 🔗 Related Files

- **Test Project:** `/test/UtilityVerse.Copy.UnitTest/`
  - Fixtures: `Fixtures.cs`
  - Tests: `DeepCopyTest.cs`, `ShallowCopyTest.cs`
  - Readme: `README.md`
- **Documentation:** This file (`Plans.md`)
- **Recent Changes:** See git history on `add-depth` branch

---

**Last Updated:** March 2026
**Author:** Pritom Purkayasta
**Status:** ✅ Production Ready (with known limitations documented)
