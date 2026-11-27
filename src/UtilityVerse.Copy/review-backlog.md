# Copy correctness review backlog

Reviewed on November 26, 2026. Goal: accurate deep and shallow copying for C#
classes, records, structs, and their supported data structures, using optimized
generated code. Work through the numbered findings individually, preserving these
numbers for follow-up conversations.

The initial review passed all 91 existing tests and ran 22 isolated adversarial
scenarios. Passing the original suite did not cover the failures below.

## Numbered findings

1. **Completed — P1 — Constructors can mutate the original graph.** Previously,
   generated constructors passed original mutable references into existing
   constructors, allowing a constructor that appends to its list argument to change
   the original list during DeepCopy. Ordinary class constructors are now bypassed; cycles, aliases, and
   readonly state remain supported. Primary constructors only receive matching
   stored immutable values without mutable references. Unsafe primary constructors,
   custom record copy constructors, and unsupported base-construction paths receive
   UV004. Supporting those rejected shapes remains future work. Field initializers,
   non-opted-in base constructors, and explicit parameterless struct constructors
   can still run; this fix does not suppress arbitrary user-code side effects.
   Seventeen added cases in ConstructorIsolationTests cover source mutation,
   constructor calls, inheritance, cycles, records, primary constructors, and
   rejection paths. Full scripts/build.sh validation passed: 145 tests, including
   108 Copy tests, with zero build warnings or errors.
2. **Completed — P1 — Dictionary and set insertion can precede state hydration.**
   Previously, a key whose hash depends on its copied owner's scalar state could
   be inserted too early, leaving the copied dictionary unable to find its key.
   The implementation now stages
   typed entries, defers insertion until object hydration completes, and validates
   standard hash-container lookups. Invalid buckets are rebuilt in place once;
   remaining unstable hash dependencies fail explicitly. Direct context/interface
   calls and recovery after failures are covered by 23 permanent regression cases
   in HashCollectionHydrationTests. Completion scopes are readonly structs; pending
   work uses typed buffers and is allocated only for nonempty hash containers.
   All existing public context signatures remain, with CopyGenerated added for
   direct generated-interface calls. Full scripts/build.sh validation passed:
   168 tests, including 131 Copy tests, with zero build warnings or errors.
   scripts/verify-packages.sh also passed fresh installed consumers on .NET 8 and
   .NET 11. Arbitrary comparer-dependency solving remains unsupported; see README.
3. **Completed — P1 — Virtual auto-properties can lose base storage.** Generated
   typed field accessors now preserve each physical property slot instead of
   dispatching virtual getters/setters. The backend supports .NET 8+ non-generic
   owners and .NET 9+ generic owners, including readonly properties, nested types,
   aliases, cycles, and computed overrides. Older targets retain UV004 when the
   required field access is unavailable. External bases require the generated
   physical-storage capability marker; old binaries must be rebuilt. Helpers
   preserve generic parameter order/constraints and live in partial enclosing
   types for inaccessible owners. Unsupported helper hosting is diagnosed.
   Runtime reflection and dynamic IL are not used. Thirty-eight regression cases
   cover physical slots, readonly aliases, generic parameter order and constraints,
   private/protected owners and property types, primary constructors, external-base
   capability checks, helper collisions, and framework fallbacks. Full
   scripts/build.sh validation passed: 206 tests, including 169 Copy tests, with
   zero build warnings or errors. Expanded scripts/verify-packages.sh consumers
   execute readonly backing-field access on .NET 8 and generic backing-field access
   on .NET 11. Existing public method and constructor signatures are preserved;
   generated constructor capability metadata is new.
4. **Completed — P1 — Covariant collection aliases can throw.** Supported
   runtime sequences now dispatch to statically compiled copiers for their actual
   element type before allocating a destination. A List<Derived> shared through
   IEnumerable<Base> and List<Derived> remains one List<Derived> copy in either
   traversal order, including empty and null-only sequences. Typed adapters
   preserve nested aliases, cycles, comparer identity, queue/stack order, and
   caller-provided element delegates. Readonly struct adapters avoid per-container
   delegate-adaptation closures. Collection type identity metadata is cached weakly;
   model-member reflection, runtime generic method construction, and dynamic IL
   are not used. Generated registration covers declared sequence elements across
   the inheritance hierarchy and accessible non-generic opted-in source models;
   generic parameter views include visible source models too. External or otherwise
   unknown actual element types require CopyContext.RegisterSequenceType<T>()
   before copying; missing registration fails explicitly even for empty sequences.
   Collection subclasses are rejected rather than sliced. Twenty-nine new
   CovariantCollectionTests cases cover supported implementations, both traversal
   orders, empty/null-only state, nested and generic views, inherited state,
   delegate behavior, comparer identity, failure recovery, explicit registration,
   subclass rejection, and generated-name diagnostics. Full scripts/build.sh
   validation passed: 235 tests, including 198 Copy tests, with zero warnings or
   errors. Expanded scripts/verify-packages.sh Release consumers passed covariant
   alias/cycle checks on .NET 8 and .NET 11. Existing public context signatures
   remain intact; static RegisterSequenceType<T>() is additive. This does not
   resolve the separate configuration/backing-store findings (#8 and #9).
5. **Completed — P1 — Dictionary subclasses are sliced.** CopyMap verifies the
   runtime type returned by the factory before registering it or copying entries.
   Generated Dictionary and ConcurrentDictionary factories now fail explicitly
   on unsupported subclasses. Custom factories can supply the same runtime type
   and preserve subclass state. Five regressions failed on the baseline and six
   controls passed. All eleven passed unchanged after the fix.
6. **Completed — P1 — Boxed structs lose shared identity.** CopyContext.Copy
   tracks the original reference when a boxed struct is passed as an object or
   interface. Shared boxes stay shared and distinct boxes stay separate. When
   Unsafe.Unbox is available to the consumer, generated copiers register the new
   box before copying its contents, preserving cycles and deferred hash keys.
   Older generated code and targets without that API preserve acyclic aliases
   and fail explicitly on reentry into an unfinished box. Copy boxed roots via
   the context so it receives the original reference. Four regressions failed
   on the baseline and two controls passed. All six tests passed unchanged after
   the fix.
7. **Completed — P1 — Uri subclasses can retain mutable source state.** Generated
   copies route Uri through CopyContext, which shares exact Uri values and dispatches
   to custom copiers for supported subtypes. Unsupported runtime subtypes fail
   explicitly. An unopted struct containing Uri now receives UV002 to prevent
   bypassing the runtime check. Rebuild consumers to refresh generated assignments.
   Ten regressions failed on the baseline and the exact-type control passed.
   All eleven passed unchanged after the fix.
8. **Completed — P2 — Collection configuration is lost.** BindingList copies
   preserve AllowEdit, AllowRemove, RaiseListChangedEvents, and the effective
   AllowNew setting. Normal insertion wires copied items to the new list.
   External event subscribers and pending add-new transactions stay with the
   source. Six regressions failed on the baseline and one control passed.
   All seven passed unchanged after the fix.
9. **Completed — P2 — Wrapper backing-store aliases are lost.** Wrapper copies
   reject shared backing stores explicitly. Public synchronization roots detect
   a wrapper and its list or array in either traversal order, plus distinct
   wrappers sharing a root. Opaque roots and fixed-size or read-only backing for
   mutable wrappers require a custom copier. Repeated views of the same wrapper
   still share one destination. Nine regressions failed on the baseline and one
   control passed. All ten passed unchanged after the fix.
10. **Completed — P2 — Explicit-interface auto-properties emit invalid C#.**
    Deep-copy generation skips these shapes and reports UV004 with the property
    name and instructions to use an ordinary backing field or a custom copier.
    Computed explicit-interface properties over ordinary fields and shallow
    copies of auto-properties are supported. Two regressions failed with CS1061
    on the baseline and two controls passed. All four passed unchanged after the fix.
11. **Completed — P2 — Collision checks are incomplete and overbroad.** Generated
    methods are checked against non-method members, containing type and type
    parameter names, and conflicting method signatures. Duplicate explicit
    copier implementations report UV004. Valid generic overloads and graph
    constructor overloads with by-reference parameters coexist with generated
    members. Fourteen regressions failed on the baseline and one control
    passed. All fifteen passed unchanged after the fix.

Validation for #8–#11: `scripts/build.sh` passed all 299 tests, including 262
Copy tests, with zero build warnings or errors. `scripts/verify-packages.sh`
passed on .NET 8 and .NET 11. Additional smoke checks against the installed
package verified binding-list settings, wrapper alias rejection in both traversal
orders, and a valid generic overload on both runtimes.

12. **Completed — P1 — Recursive struct declarations can crash the compiler.**
    Scalar and constructor-state validation track the active value-type path.
    Roslyn can report an invalid recursive layout while the generator and
    analyzer continue running.
13. **Completed — P1 — File-local models emit code that cannot access the model.**
    Both opt-in styles report UV004 at the file-local declaration. File-local
    types are also excluded from generated sequence registration.
14. **Completed — P2 — Copy diagnostics can be silent or imprecise in editors.**
    UV004 points to unsupported stored members, UV005 warns about ignored
    policies, and UV001 tells users which declaration needs `partial` and
    which copy methods it enables. EditorDiagnosticsTests exercises diagnostic
    severity, source locations, and successful generation after adding
    `partial`. The installed-package check covers both opt-in styles and
    warning and error reporting. Full validation passed 347 tests, including
    310 Copy tests, and fresh package consumers passed on .NET 8 and .NET 11.
15. **Completed — P2 — Diagnostic messages lack repair guidance.** Active
    diagnostics have rule-specific help links and actionable messages. UV002
    includes a related source declaration. The consumer README documents a
    coding-agent workflow and SARIF export. Full validation passed 355 tests,
    including 318 Copy tests. The installed package emitted all four help links
    in the compiler's SARIF log and through the Roslyn language server. The
    tested language server did not forward the UV002 related location.

## Support and optimization work

- **Completed.** Emit both methods for dual opt-ins. Validate each mode
  independently and deduplicate partial declarations. Three regression cases
  failed on the baseline and passed unchanged with the fix.
- **Completed for sequences.** Generic state such as Holder<List<int>> uses
  typed runtime dispatch. Visible models and common scalars are registered
  automatically. Hidden element types use the existing explicit registration.
  Five regressions failed on the baseline and two controls passed.
- **Completed.** Preserve array type, rank, bounds, aliases, and cycles, including
  runtime System.Array views and non-vector rank-one arrays. Five regressions
  failed on the baseline and one vector control passed.
- **Supported within documented constructor limits.** Non-positional records
  can preserve private and readonly state through an opted-in base graph
  constructor. Positional records support accessible inherited state through
  a verified synthesized constructor chain.
- **Supported within documented shape limits.** Dictionary interfaces preserve
  supported concrete maps. Added sorted maps and sets, linked lists, selected
  immutable and frozen collections with stable scalar keys, and scalar support
  for Rune and TimeOnly.
- **Completed.** Added exact runtime type external copier registration and
  explicit Copy, Share, Reset, and Custom policies for stored fields and
  properties. Pointer and ref-like state still requires custom model handling.
- **Completed.** Guard nesting and available stack space. A rejected copy
  clears partial state, and a caught nested failure poisons its outer operation.
- **Completed.** Use a typed factory for direct struct copies, clone arrays of
  shareable values without a traversal, and preallocate list, queue, and stack
  destinations. Roslyn reuses unchanged output files after model or unrelated
  edits.
- **Measured.** BenchmarkDotNet 0.15.8 ShortRun on .NET 8 arm64, Apple M4 Pro.
  Runtime comparisons used saved Release builds immediately before and after
  the hot-path changes. Inputs were 4,096 integers for arrays and lists, and a
  generated struct holding one long.

  | Workload | Mean before → after | Allocated before → after |
  |---|---:|---:|
  | Generated integer array copy | 2.95 μs → 0.80 μs | 16,776 B → 16,776 B |
  | Integer list copy | 14.21 μs → 9.86 μs | 33,496 B → 16,784 B |
  | Generated typed struct copy | 43.23 ns → 26.37 ns | 280 B → 208 B |
  | Unrelated edit with 128 opted-in models | 1.844 ms → 1.859 ms | 2.57 MB → 2.64 MB |

  The generator comparison includes the new policy and collection checks. It
  showed unchanged file reuse in Roslyn tracking, similar elapsed time, and
  slightly higher allocations in the measured incremental run. Per-model
  semantic planning is still repeated.

Validation: `scripts/build.sh` passed 335 tests, including 298 Copy tests, with
zero build warnings or errors. `scripts/verify-packages.sh` and a separate
installed-package smoke test passed on .NET 8 and .NET 11. The smoke test exercised
dual-mode generation, generic lists, shaped arrays, map interfaces, immutable and
frozen collections, external policies, and record inheritance.

Acceptance criterion: every accepted shape preserves its documented state and
reference relationships, and every unsupported shape fails clearly. Add permanent
regressions for each completed finding. Do not treat completion of one finding as
completion of the broader support or performance work.
