# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

TheUtilityVerse is a set of independently published NuGet packages of C# helpers, built from one solution (`UtilityVerse-Soln.slnx`):

| Project | Package | Targets | Notes |
|---|---|---|---|
| `src/UtilityVerse` | `UtilityVerse` | netstandard2.1; net6.0; net8.0; net9.0; net10.0 | Core helpers, no external dependencies |
| `src/UtilityVerse.ASPNET` | `UtilityVerse.ASPNET` | same multi-target | ASP.NET Core helpers (`Microsoft.AspNetCore.Http*` 2.3.0) |
| `src/UtilityVerse.Shared` | (not packed separately) | netstandard2.1 | `UtilityVerseResult<T>`, `UtilityVerseException`; referenced by `UtilityVerse` |
| `src/UtilityVerse.Copy` | `UtilityVerse.Copy` | netstandard2.0 | Roslyn incremental source generator + analyzer — has its own [`CLAUDE.md`](src/UtilityVerse.Copy/CLAUDE.md) |
| `test/UtilityVerse.UnitTest` | — | net10.0 | xUnit tests for `UtilityVerse` |
| `test/UtilityVerse.Copy.UnitTest` | — | net10.0 | xUnit tests for the Copy generator + analyzer (in-memory generator runs) |
| `test/UtilityVerse.Copy.Test` | — | net10.0 | Sample console app consuming the Copy generator as a real analyzer reference |

## Commands

CI (`.github/workflows/dotnet.yml`) uses .NET 10 and runs restore → build → test on push to `development` and PRs to `master`. `global.json` opts `dotnet test` on .NET 10+ SDKs into Microsoft.Testing.Platform mode, which xunit.v3 4.x requires. That runner creates a gitignored `TestResults/` folder (reports, TRX, coverage, dumps) in the working directory; an empty one at the repo root is a leftover and safe to delete, or use `--results-directory <path>` to redirect it.

```bash
dotnet build UtilityVerse-Soln.slnx
dotnet test UtilityVerse-Soln.slnx
dotnet test test/UtilityVerse.UnitTest --filter "FullyQualifiedName~IntExtensionTest"          # one class
dotnet test test/UtilityVerse.UnitTest --filter "FullyQualifiedName~IntExtensionTest.IsInBetween_ReturnFalse_WhenWrongDataIsProvided"  # one test
dotnet run --project test/UtilityVerse.Copy.Test   # Copy generator harness; see src/UtilityVerse.Copy/CLAUDE.md
dotnet pack src/<Project> -c Release
```

`UtilityVerse` and `UtilityVerse.ASPNET` set `TreatWarningsAsErrors` and `CheckForOverflowUnderflow` in both Debug and Release, and `UtilityVerse` generates XML docs — so every public member needs a `<summary>` doc comment or the build fails. Because of multi-targeting down to netstandard2.1, avoid APIs not available on all targets (or guard with `#if`).

Before every commit that touches C# files, run `dotnet format` scoped to the changed files (`--include <paths>`) and include the results in that commit. Don't format the whole solution or untouched files.

## Architecture

### Core library (`UtilityVerse`)
- `Utility` is a `sealed partial class` split across `Utility.*.cs` files (e.g. `Utility.Currency.cs`, `Utility.OS.cs`); new static helpers go into a new or existing partial file. `UtilityVerse.ASPNET` has its own separate `UtilityVerse.ASPNET.Utility` partial class following the same pattern.
- Extension methods live in `Extensions/` as `{Type}Extension` static classes in namespace `UtilityVerse.Extensions`.
- **Error convention (breaking change, see `src/UtilityVerse/breaking-changes.md`):** helpers return `UtilityVerseResult<T>` instead of throwing. Invalid input → `new UtilityVerseResult<T>(errorMessage)` (sets `Error`, `HasError` becomes true); success → `new UtilityVerseResult<T>(value)`. Tests assert on `.Result` / `.HasError`.
- Source files start with the copyright header block used throughout the repo.

### Copy source generator (`UtilityVerse.Copy`)
`UtilityVerse.Copy` is a Roslyn source generator + analyzer with its own constraints (netstandard2.0, no `UtilityVerseResult<T>` convention, its own test project `test/UtilityVerse.Copy.UnitTest`). Everything about it — generator pipeline, emitted code per property type, analyzer rules UV001–UV003, known limitations, release steps and how to verify changes — is documented in **[`src/UtilityVerse.Copy/CLAUDE.md`](src/UtilityVerse.Copy/CLAUDE.md)**. Read that file before changing or reviewing anything in `src/UtilityVerse.Copy`, `test/UtilityVerse.Copy.UnitTest` or `test/UtilityVerse.Copy.Test`; the conventions in this file's "Core library" section do not apply there.

## Branching

Per `CONTRIBUTING.md`: branch from `development`, PR into `development`; don't push directly to `master` or `development`. CI also runs on PRs to `master`.
