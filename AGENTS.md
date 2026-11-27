# Repository instructions

These instructions apply throughout TheUtilityVerse. Keep repository-wide guidance
and the project map here; keep project-specific details in the relevant nested
`AGENTS.md`. Update the appropriate instructions when architecture or workflows
change, and maintain one authoritative copy of each rule.

## Project map and required context

`UtilityVerse.slnx` groups three published C# utility packages and their repository
support projects:

| Path | Package or purpose | Targets | Context |
|---|---|---|---|
| `src/UtilityVerse/` | Core helpers, `Extensions/`, and `Contracts/`; no external dependencies beyond Shared | netstandard2.1; net6.0; net8.0; net9.0; net10.0; net11.0 | Core architecture below |
| `src/UtilityVerse.ASPNET/` | ASP.NET Core helpers using `Microsoft.AspNetCore.Http*` 2.3.x | Same multi-target list as core | Core architecture below |
| `src/UtilityVerse.Shared/` | Repository support for `UtilityVerseResult<T>` and `UtilityVerseException`; referenced by core and bundled inside its package, never published separately | netstandard2.1 | Core error convention below |
| `src/UtilityVerse.Copy/` | Roslyn incremental copy generator, analyzer, and runtime support; package artwork in `Assets/` | netstandard2.0 | [Copy instructions](src/UtilityVerse.Copy/AGENTS.md) |
| `tests/UtilityVerse.UnitTest/` | Core helper tests | net11.0 | Testing guidelines below |
| `tests/UtilityVerse.Copy.UnitTest/` | In-memory generator/analyzer compilation and runtime tests | net11.0 | [Copy instructions](src/UtilityVerse.Copy/AGENTS.md) |
| `tests/UtilityVerse.Copy.Test/` | Console sample consuming Copy as a real analyzer reference | net11.0 | [Copy instructions](src/UtilityVerse.Copy/AGENTS.md) |
| `assets/` | Repository artwork | — | Preserve existing assets |
| `scripts/` | Build and package verification automation | — | Build workflow below |

Before reviewing or changing the Copy generator, its tests, or its sample, read
`src/UtilityVerse.Copy/AGENTS.md`. This is required even when working only under
`tests/`, outside the nested source-directory instruction scope. Core helper error
and documentation conventions do not override the Copy project's conventions.

## Build, test, and development commands

Use the .NET 11 SDK (`11.0.100` with compatible patch roll-forward), as specified
in `global.json`. Run commands from the repository root.

- `./scripts/build.sh` (Linux/macOS) or `./scripts/build.ps1` (Windows): the full validation workflow. Deletes all `bin`/`obj` under `src/` and `tests/`, formats only changed C# files (modified and untracked), restores, builds, and tests. Run before every commit touching C# and include scoped formatting changes. Do not format untouched files or the whole solution.
- `dotnet build UtilityVerse.slnx`: build all projects and target frameworks.
- `dotnet test UtilityVerse.slnx`: run automated tests.
- `dotnet test tests/UtilityVerse.Copy.UnitTest/UtilityVerse.Copy.UnitTest.csproj --filter-class '*ConstructorIsolationTests'`: example focused test class.
- `dotnet run --project tests/UtilityVerse.Copy.Test`: run the generator sample.
- `dotnet format UtilityVerse.slnx --include <paths>`: format only specified changed files.
- `dotnet pack src/<Project> -c Release`: pack an individual package.
- `bash scripts/verify-packages.sh`: verify Release packages and a fresh installed consumer on .NET 8 and .NET 11.

CI in `.github/workflows/dotnet.yml` restores, builds, tests, and verifies Release
packages on pushes to `development` and PRs to `master` or `development`.
`.github/workflows/publish.yml` validates and publishes only UtilityVerse, Copy,
and ASPNET after pushes to `master`, using NuGet Trusted Publishing and the
`release` environment. Shared is nonpackable; core bundles its DLL and XML docs
for each target framework. Keep publishing setup in `CONTRIBUTING.md`.

## Build configuration and compatibility

Dependency versions are managed centrally in `Directory.Packages.props`;
`PackageReference` entries carry no `Version`. Shared settings are layered:

- Root `Directory.Build.props`: `LangVersion`, `Nullable`, `ImplicitUsings`, `LatestTargetFramework`, and common package metadata (authors, copyright, license, repository). Update `LatestTargetFramework` when intentionally moving to a new .NET version.
- `src/Directory.Build.props`: `LibraryTargetFrameworks` for core and ASPNET, `TreatWarningsAsErrors`, and `CheckForOverflowUnderflow`.
- `tests/Directory.Build.props`: `TargetFramework` from `LatestTargetFramework` and `IsPackable=false`.

Each published package ships an `examples.md` at its root and XML documentation
beside its assemblies. Keep public method summaries and runnable examples aligned
with API behavior. Package verification compiles every C# block in the packaged
example documents on .NET 8 and .NET 11.

Keep project files limited to project-specific settings. Preserve compatibility
across target frameworks; core targets down to netstandard2.1, while Copy targets
netstandard2.0. Avoid unavailable APIs or guard target-specific code with `#if`.

`UtilityVerse`, `UtilityVerse.Shared`, and `UtilityVerse.UnitTest` intentionally
set `Nullable` to `disable`. Do not enable it casually: doing so changes public API
annotations and breaks the build.

`global.json` selects Microsoft.Testing.Platform for `dotnet test` on .NET 10+
SDKs, as required by xUnit v3 4.x. Test output can create a gitignored
`TestResults/` directory containing reports, TRX, coverage, or dumps. Use
`--results-directory <path>` to redirect it. Empty leftover result directories are
safe to remove.

## Core architecture and coding conventions

- Use four-space indentation, Allman braces, file-scoped namespaces, PascalCase types/methods, and camelCase locals/parameters. Preserve copyright headers, including the repository's existing source-file header.
- `Utility` is a `sealed partial class` split across `Utility.*.cs`. Add static helpers to a new or existing related partial file. ASPNET has its own separate `UtilityVerse.ASPNET.Utility` partial class following the same pattern.
- Place core extension methods in `Extensions/`, namespace `UtilityVerse.Extensions`, and name extension classes `{Type}Extension`.
- Document public core members with XML `<summary>` comments. Core generates XML documentation, and source projects treat warnings as errors.
- Core helpers return `UtilityVerseResult<T>` for errors instead of throwing; see `src/UtilityVerse/breaking-changes.md`. Prefer `UtilityVerseResult<T>.Failure(errorMessage)` and `.Success(value)` to avoid string-constructor ambiguity. Legacy constructors remain. Tests assert `.Result` and `.HasError`. This convention does not apply to the Copy generator.

## Mandatory Readability and Performance Rules

These are hard requirements for every new or modified implementation, including generated C# and C# examples embedded in tests:

- Give code breathing room. Separate functions and members with a blank line, and separate logical stages inside functions. Use Allman braces for blocks, one statement per line, and readable spacing around operators. Keep only genuinely simple expressions on one line; expand compound statements, nested conditionals, and long argument lists.
- Keep code maintainable: use descriptive names, focused functions, explicit control flow, and comments explaining non-obvious invariants. Do not compress code to reduce line count or introduce abstractions without a concrete benefit.
- Design for speed and minimal allocations. Prefer allocation-free operations and `readonly struct` or other value-based representations where they actually avoid heap allocations without boxing, excessive copying, or loss of correctness. A struct is not automatically allocation-free or faster; retain reference types when shared identity or mutable ownership requires them.
- Avoid unnecessary boxing, closures, temporary collections, repeated enumeration, and reflection in runtime hot paths. Use known collection capacities and typed operations where appropriate. Pool or reuse buffers only with clear ownership and safe lifetimes.
- Preserve correctness, graph identity, API compatibility, and target-framework support before optimizing. Deep-copy destinations and graph tracking can require allocations; remove avoidable allocations rather than promising an allocation-free operation that cannot satisfy its contract.
- Validate performance changes with relevant measurements before claiming a speed or allocation improvement. Formatting-only changes need correctness checks, not invented performance claims. Apply these rules to touched code and keep unrelated repository-wide churn out of focused changes.

## Testing guidelines

Tests use xUnit v3 with Microsoft.Testing.Platform. Add or update tests for changed
behavior, including invalid inputs and regressions. Follow names such as
`Method_ReturnTrue_WhenCondition`. Put generator/analyzer tests in
`tests/UtilityVerse.Copy.UnitTest/` and follow the Copy instructions. No numeric
coverage threshold is documented.

## Git, commits, and pull requests

- Discuss contributions in an issue first. Follow `CONTRIBUTING.md`: fork and branch from `development`, open PRs against `development`, and do not push directly to `master` or `development`.
- Use concise imperative commit subjects matching history, such as `Centralize build settings` or `COPY: Add automated test project`.
- Describe behavior changes in PRs, link the issue, and report validation results. Include updated tests and scoped formatting changes.
- When committing for GitHub account `purkayasta`, use `12936435+purkayasta@users.noreply.github.com` for both author and committer email. Prefer per-command Git configuration overrides; preserve global settings. If GitHub rejects a push with GH007, correct the new unpublished commit's email and retry without disabling email privacy protection.
