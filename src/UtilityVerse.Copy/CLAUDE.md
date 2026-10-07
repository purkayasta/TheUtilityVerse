# CLAUDE.md — UtilityVerse.Copy

Guidance for working on the `UtilityVerse.Copy` package (`src/UtilityVerse.Copy`) and its manual test harness (`tests/UtilityVerse.Copy.Test`). This project is a Roslyn source generator + analyzer and follows different rules from the rest of the repo (see the root `CLAUDE.md` for repo-wide conventions).
## What it is

A compile-time code generator. A consumer marks a `public partial` class/record/struct with an opt-in, and the generator emits a `DeepCopy()` or `ShallowCopy()` method into a partial of that type. An analyzer in the same assembly reports errors when the opt-in can't be honored. There is no runtime component: the attributes and marker interfaces are the only public API besides the generator/analyzer.

## Project constraints

- **Must target `netstandard2.0`** (Roslyn host requirement). Do not multi-target or raise it. `LangVersion` is `latest` (from the root props), so modern syntax is fine, but only netstandard2.0 BCL APIs are available.
- Depends on `Microsoft.CodeAnalysis.CSharp` 4.11.0 and `Microsoft.CodeAnalysis.Analyzers` 5.9.0 (build-time rule checks only; independent of the Roslyn version consumers need), both `PrivateAssets="all"` so the package declares no NuGet dependencies. A consumer's compiler must be at least Roslyn 4.11 (older compilers report CS9057 and skip the generator). Raise the version only when the code needs a newer Roslyn API. The unit test project pins Roslyn 4.11.0 too (`Basic.Reference.Assemblies.Net100` requires `Microsoft.CodeAnalysis.Common` 4.11 or newer, so tests cannot run on 4.10).
- `EnforceExtendedAnalyzerRules` is on: no file I/O, no banned APIs in generator/analyzer code.
- Inherits `TreatWarningsAsErrors`, `CheckForOverflowUnderflow`, `Nullable`, `ImplicitUsings` and `LangVersion` from the `Directory.Build.props` files; it does not generate XML docs.
- Packaging: the built DLL is packed under `analyzers/dotnet/cs`; `nuget.md` is the package readme (separate from `README.md`) and `Assets/copy.png` is the icon. Version lives in `UtilityVerse.Copy.csproj` (`<Version>`).
- Source files start with the repo's copyright header block.

## Layout

| Path | Role |
|---|---|
| `CopyGenerator.cs` | `IIncrementalGenerator` entry point: discovers opted-in types, dispatches to a mode generator, adds sources |
| `Generators/DeepCopyGenerator.cs` | Emits `DeepCopy()` |
| `Generators/ShallowCopyGenerator.cs` | Emits `ShallowCopy()` |
| `Helper.cs` | Shared type classification (primitive / built-in / collection), property enumeration, opt-in and `partial` checks |
| `Analyzers/ValidationAnalyzer.cs` | `DiagnosticAnalyzer` reporting UV001–UV003 |
| `Analyzers/Rules.cs` | `DiagnosticDescriptor`s |
| `Attributes/DeepCopy.cs`, `Attributes/ShallowCopy.cs` | `[DeepCopy]`, `[ShallowCopy]` (`AttributeTargets.Class`, not inherited) |
| `Interfaces/IDeepCopy.cs`, `Interfaces/IShallowCopy.cs` | Empty marker interfaces (inherited via `AllInterfaces`) |
| `AnalyzerReleases.{Shipped,Unshipped}.md` | Release tracking for diagnostic IDs (enforced by the build) |

## Pipeline: `CopyGenerator`

1. Syntax predicate: every `TypeDeclarationSyntax`.
2. Semantic transform (`GetSemanticTarget`): resolve the symbol; keep only `DeclaredAccessibility == Public`.
3. Opt-in detection by **fully qualified display-name string** (not symbol identity):
   - Deep: attribute `UtilityVerse.Copy.DeepCopy` or any interface in `AllInterfaces` named `UtilityVerse.Copy.IDeepCopy`.
   - Shallow: same with `ShallowCopy` / `IShallowCopy`.
   - **Deep wins** — a type opted into both only gets `DeepCopy()`.
4. Results are collected and grouped by `(Symbol, Mode)` so a multi-part partial type is generated once.
5. Each group calls the mode generator; empty/null source is skipped.
6. Hint name: fully qualified name with `global::` stripped and `.` → `_`, suffixed `_{Mode}Copy.g.cs` (e.g. `UtilityVerse_Copy_Test_HelloWorld_DeepCopy.g.cs`).

The same opt-in check exists in two places — inline in `CopyGenerator.GetSemanticTarget` and in `Helper.HasDeepCopyOptIn`/`HasShallowCopyOptIn` (used by the analyzer). Change both together or the generator and analyzer will disagree.

## Shared rules (`Helper`)

- **"Copy by value" types** — assigned directly, never recursed into: anything `IsTrulyPrimitive` (any value type — including every struct, enum and `Nullable<T>` — plus `string`) or `IsBuiltinType` (`DateTime`, `DateOnly`, `DateTimeOffset`, `Guid`, `TimeSpan`, `decimal`, `Uri`, `Version`, `BigInteger`, `Complex`, `Index`, `Range`, `Half`, `Int128`, `UInt128`). Consequence: a user-defined struct property is copied by value even if it holds reference-type members.
- **Collections** — `IsGenericCollection` maps the generic type's original definition (e.g. `System.Collections.Generic.List<T>`) to a kind: `List`, `Enumerable`, `HashSet`, `Dictionary`, `Queue`, `Stack`. Unlisted generic types are not treated as collections.
- **Properties** — `GetAllProperties` walks the type and its base types (stopping at `object`) and yields only properties with a setter (`init` counts). Get-only/computed properties are ignored by both generators and the analyzer.
- **`IsPartial`** — true if any declaring syntax reference has the `partial` modifier.

## `DeepCopy()` generation

Skipped (no source) if the type is abstract, not `partial`, or not a class/struct/record/record struct.

Emits `public partial {class|struct|record|record struct} {Name}` in the type's namespace (or none for the global namespace), with a `[GeneratedCode("DeepCopyGenerator", "1.0")]` method `public {Name} DeepCopy()`.

**Construction strategy:**
- **Has a parameterless ctor** (always true for structs): `return new T { Prop = <expr>, ... };` for every settable property.
- **No parameterless ctor:** pick the ctor with the most parameters; each parameter is matched to a settable property by case-insensitive name → `this.Prop` (by-value types) or `this.Prop?.DeepCopy()`; unmatched → `default(T)`. Properties not covered by the ctor are **not** copied in this path.
- No ctor at all → nothing generated.

**Per-property expression** (initializer path):

| Property type | Expression |
|---|---|
| By-value type (see above) | `this.P` |
| Array of by-value element | `this.P?.ToArray()` |
| Array of reference element | `this.P?.Select(x => x?.DeepCopy()).ToArray()` |
| Collection kind `HashSet` | `new HashSet<E>(this.P.Select(x => <e>).ToList())` (null-guarded) |
| Collection kind `Dictionary` | `this.P?.ToDictionary(x => <key>, x => <value>)` |
| Any other collection kind (`List`, `Enumerable`, `Queue`, `Stack`) | `this.P?.Select(x => <e>).ToList()` |
| `System.Tuple<...>` / `System.ValueTuple<...>` (reference tuple path) | `new TupleType(x.Item1 [?.DeepCopy()], ...)` |
| Anything else | `this.P?.DeepCopy()` |

`<e>`/`<key>`/`<value>` is `x` / `x.Key` / `x.Value` for by-value types, otherwise `?.DeepCopy()` on it.

The emitted expression's type is always `List<T>`, `HashSet<T>`, `Dictionary<K,V>` or `T[]`. A property whose declared type isn't assignable from that (e.g. `Queue<T>`, `Stack<T>`, `ImmutableList<T>`, `ObservableCollection<T>`, `ConcurrentDictionary<K,V>`, `ImmutableHashSet<T>`) produces generated code that **does not compile** in the consumer. Interface-typed properties (`IEnumerable<T>`, `IList<T>`, `IReadOnlyList<T>`, `ICollection<T>`) and the concrete `List`/`HashSet`/`Dictionary` types are safe.

## `ShallowCopy()` generation

Same skip rules and wrapper as DeepCopy, with `[GeneratedCode("ShallowCopyGenerator", "1.0")]`.

- **Class or any record:** `var copy = (T)this.MemberwiseClone();` then, for non-by-value settable properties:
  - Arrays → `copy.P = (T[])this.P.Clone()` (null-guarded).
  - Generic collections are intended to be re-wrapped (`ToList()` / `new HashSet<T>(...)` / `ToDictionary(...)`), **but** this branch looks up `OriginalDefinition.ConstructUnboundGenericType().ToDisplayString()` (e.g. `List<>`), which never matches `IsGenericCollection`'s keys (e.g. `List<T>`). In practice collections stay as the same shared reference from `MemberwiseClone`. Fixing this would also expose the same assignability problem described for DeepCopy.
  - Tuples/ValueTuples and everything else → left as the shared reference from `MemberwiseClone`.
  - `return copy;`
- **Non-record struct:** `return this;` (value copy).

## Analyzer: `ValidationAnalyzer`

Runs per named type (class/struct only), skipping types carrying `GeneratedCode`/`CompilerGenerated` attributes; generated code analysis is disabled.

| ID | Severity | Fires when |
|---|---|---|
| UV001 | Error | Type opts into Deep or Shallow but is not `partial` (location: type identifier) |
| UV002 | Error | Deep opt-in type has a settable property whose type (after unwrapping `Nullable<T>` and recursing into collection type arguments) is not by-value and not itself Deep-opted-in |
| UV003 | Error | Same as UV002 for Shallow |

Analyzer behavior to be aware of:
- Only `INamedTypeSymbol` property types are checked — **array element types are not validated**, so `Foo[]` where `Foo` lacks an opt-in passes the analyzer and then fails to compile at `x?.DeepCopy()`.
- `System.Tuple<...>` is a reference type and not a recognized collection, so a tuple property triggers UV002/UV003 against the tuple type itself. ValueTuples are value types and pass.
- The analyzer does **not** check accessibility; a non-public opted-in type gets no diagnostic and no generated code.
- `IsInsideCopyEnabledPartialParent` is currently unused.

## Known limitations (verify against these before claiming support)

- Attributes are `AttributeTargets.Class` only: `[DeepCopy]`/`[ShallowCopy]` on a `struct` or `record struct` is a CS0592 error. Structs must opt in via `IDeepCopy`/`IShallowCopy`.
- Nested types: the generator emits `public partial class Inner` directly in the namespace, not inside the containing type, so nested opted-in types produce broken code.
- Generic types: the hint name contains `<`/`>` (e.g. `Sample_Box<T>_DeepCopy.g.cs`), so `AddSource` throws, the generator fails with CS8785, and **no** code is generated for any type in that compilation.
- Nullable reference built-ins (`Uri?`, `Version?`) aren't recognized as built-in, so `?.DeepCopy()` is emitted on them (CS1061).
- `System.Tuple<...>` codegen: a nullable tuple emits `new Tuple<...>?(...)` (CS8628), and two tuple properties in one type redeclare the pattern variable `x` (CS0128).
- Nested collections (`List<List<Foo>>`) emit `x?.DeepCopy()` on the inner collection (CS1061); the analyzer stays silent.
- Generated files always contain `using System.Collections.Immutable;` and `using System.Collections.Frozen;`. Consumers must target a framework where these namespaces exist (`Frozen` is .NET 8+), otherwise the generated file fails to compile.
- Generated method is always `public` and non-virtual; a derived opted-in type shadows the base's `DeepCopy()`/`ShallowCopy()`.

## Making changes

- **New supported type/collection:** update `Helper` (`IsBuiltinType` or `IsGenericCollection`), then the matching emit branch in **both** generators, then make sure the analyzer's recursion still classifies it correctly. Check the emitted expression is assignable to the property's declared type.
- **New/changed diagnostic:** add the descriptor in `Analyzers/Rules.cs`, register it in `ValidationAnalyzer.SupportedDiagnostics`, and add a row to `AnalyzerReleases.Unshipped.md` (the build fails with RS2000-series errors otherwise). Never reuse or renumber a shipped ID.
- **Release:** bump `<Version>` in the csproj, move `AnalyzerReleases.Unshipped.md` entries into a new `## Release x.y.z` section in `AnalyzerReleases.Shipped.md`, and update `README.md`/`nuget.md` if user-visible behavior changed.
- Keep generator code allocation-light and deterministic; generator output must depend only on the symbol, not on ordering or environment.

## Verifying changes

Primary verification is the xUnit project `tests/UtilityVerse.Copy.UnitTest`. It runs `CopyGenerator` and `ValidationAnalyzer` in memory (net10 reference assemblies from `Basic.Reference.Assemblies.Net100`, no network needed), so no consumer project has to be rebuilt.

```bash
dotnet test tests/UtilityVerse.Copy.UnitTest
dotnet test tests/UtilityVerse.Copy.UnitTest --filter "KnownIssue=shallow-collections-not-rewrapped"   # one known issue
```

| Path | Role |
|---|---|
| `Infrastructure/GeneratorHarness.cs` | `Run(source)` builds a nullable-enabled `CSharpCompilation` (net10 refs + the Copy assembly, global usings for `System`, `System.Collections.Generic`, `System.Linq`, `UtilityVerse.Copy`), runs the generator and returns a `GeneratorRun` (driver, output compilation, generated sources, `GeneratedText`, `HintNames`, `GeneratedFile(hintName)`, `CompilationErrorIds`). `Load(run)` emits it and loads it into a collectible `AssemblyLoadContext` (`LoadedAssembly`, use `dynamic` to call `DeepCopy()`/`ShallowCopy()`) |
| `Infrastructure/AnalyzerVerifier.cs` | Wraps `CSharpAnalyzerTest<ValidationAnalyzer, DefaultVerifier>`; markup `{\|UV002:Name\|}` marks the expected diagnostic span |
| `DeepCopyGenerationTests.cs`, `ShallowCopyGenerationTests.cs` | Generated-text assertions: fetch one emitted file with `run.GeneratedFile("<HintName>.g.cs")` (or `run.GeneratedText` / `run.HintNames` across all files) and `Assert.Contains`/`DoesNotContain` the exact lines that matter (per-property assignment, method signature, `partial` declaration, `MemberwiseClone`, namespace). One test per area (primitives, arrays, each collection kind, tuples, records, structs, ctor path, global namespace, deep+shallow, interface opt-in) |
| `SkippedTypeTests.cs` | Abstract / non-partial / non-public / not-opted-in types emit nothing |
| `DeepCopyRuntimeTests.cs`, `ShallowCopyRuntimeTests.cs` | Compile-and-run: assert no compile errors, call the generated method, mutate the original, assert the copy is (deep) or isn't (shallow) independent |
| `ValidationAnalyzerTests.cs` | UV001–UV003 positive cases and must-not-fire cases |
| `KnownIssuesTests.cs` | One test per known bug, asserting **today's broken behavior**, tagged `[Trait("KnownIssue", "<slug>")]` |

**Known issues:** each `KnownIssuesTests` test pins current buggy behavior so the suite stays green. A fix is expected to make its KnownIssue test fail — that is the signal. Then rewrite the test to assert the correct behavior, drop the `[Trait("KnownIssue", ...)]`, move it to the matching area test class (generation/runtime/analyzer), and update the limitation text above.

Test kinds:
- **Generated-text assertions** (`*GenerationTests`) pin the emitted shape. Copy expected lines verbatim from real generator output (inspect it via the console app below or by printing `run.GeneratedFile(...)` once); assert a few precise lines per test rather than whole files.
- **Compile-and-run** (`*RuntimeTests`) prove the output compiles and copies with the right independence.
- **Analyzer** (`ValidationAnalyzerTests`) cover diagnostics, positive and must-not-fire.
- **KnownIssues** (`KnownIssuesTests`) pin today's broken behavior, flipped as described above.

Checklist for any generator/analyzer change:
1. Add or update a generated-text test for new emitted shapes and a compile-and-run test when behavior (independence/sharing) matters; add analyzer tests (positive + must-not-fire) for diagnostic changes.
2. Run `dotnet test tests/UtilityVerse.Copy.UnitTest`; update the expected lines of any generated-text test whose output changed intentionally; flip any KnownIssue test the change fixes.
3. Confirm the emitted expression is assignable to the property's declared type — runtime tests assert `CompilationErrors` is empty.

Secondary: `tests/UtilityVerse.Copy.Test` is a small sample consumer (console app) that references the generator twice — as a normal reference (for the attributes/interfaces) and with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"` (to run the generator). `Program.cs` calls the generated `DeepCopy()` and prints that mutating the original doesn't affect the copy. Use it to check real-build integration or inspect emitted files:

```bash
dotnet build tests/UtilityVerse.Copy.Test -p:EmitCompilerGeneratedFiles=true
# inspect output:
#   tests/UtilityVerse.Copy.Test/obj/Debug/net10.0/generated/UtilityVerse.Copy/UtilityVerse.Copy.CopyGenerator/*.g.cs
dotnet run --project tests/UtilityVerse.Copy.Test
```

If the IDE shows stale generated code, rebuild from the CLI; Visual Studio caches loaded generator assemblies until restart.
