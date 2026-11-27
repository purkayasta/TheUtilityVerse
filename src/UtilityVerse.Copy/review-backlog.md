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
4. **Open — P1 — Covariant collection aliases can throw.** A List<Derived> visited
   first through IEnumerable<Base> becomes List<Base>; a later List<Derived>
   reference throws InvalidCastException. Preserve a compatible runtime container
   independently of member traversal order.
5. **Open — P1 — Dictionary subclasses are sliced.** A subclass in a Dictionary
   member becomes an ordinary Dictionary and loses additional state. Apply a
   consistent runtime-type policy across all container adapters.
6. **Open — P1 — Boxed structs lose shared identity.** Two generic members pointing
   to one boxed opted-in struct produce separate destination boxes. Track boxed
   values separately from ordinary value copies.
7. **Open — P1 — Uri subclasses can retain mutable source state.** Uri is treated
   as immutable even when the runtime object is a mutable subclass. Audit both
   generated and runtime immutable classification.
8. **Open — P2 — Collection configuration is lost.** BindingList.AllowEdit,
   AllowRemove, and RaiseListChangedEvents reset to defaults. Define and preserve
   each supported collection's behavioral state and subscription policy.
9. **Open — P2 — Wrapper backing-store aliases are lost.** A List and its
   AsReadOnly view acquire separate copied backing lists. Preserve or explicitly
   reject unsupported backing-store relationships.
10. **Open — P2 — Explicit-interface auto-properties emit invalid C#.** An explicit
    I.Data property is emitted as ordinary member access and fails with CS1061.
    Add appropriate emission or an explanatory unsupported-shape diagnostic.
11. **Open — P2 — Collision checks are incomplete and overbroad.** A ShallowCopy
    field passes validation then causes CS0102; DeepCopy<T>() is incorrectly
    rejected even though it can coexist with DeepCopy(). Compare full signatures
    and all conflicting member kinds.

## Support and optimization work

- Generate both methods when both modes are requested; deep currently wins.
- Add supported runtime dispatch for generic state such as Holder<List<int>>.
- Expand arrays to multiple dimensions and nonzero lower bounds.
- Support record inheritance without losing inherited storage.
- Add dictionary interfaces, sorted and linked collections, immutable and frozen
  collections, and adapters for common external value types.
- Add external copier registration and explicit copy/share/reset/custom policies.
  Resources such as streams, locks, delegates, handles, and ref-like values need
  defined semantics rather than an unconditional universal-copy promise.
- Protect against excessive recursion in deep graphs.
- Optimize typed struct paths, allocations, collection capacities, array copying,
  and generator incrementality after correctness is established.
- Benchmark time, allocations, and incremental rebuild behavior before making
  performance claims.

Acceptance criterion: every accepted shape preserves its documented state and
reference relationships, and every unsupported shape fails clearly. Add permanent
regressions for each completed finding. Do not treat completion of one finding as
completion of the broader support or performance work.
