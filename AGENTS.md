# Repository Guidelines

## Project Structure & Module Organization

`UtilityVerse.slnx` groups independently published C# utility packages:

- `src/UtilityVerse/`: core helpers, `Extensions/`, and `Contracts/`.
- `src/UtilityVerse.ASPNET/`: ASP.NET Core helpers.
- `src/UtilityVerse.Shared/`: shared result and exception types.
- `src/UtilityVerse.Copy/`: Roslyn copy generators and analyzers; package artwork lives in `Assets/`.
- `tests/UtilityVerse.UnitTest/` and `tests/UtilityVerse.Copy.UnitTest/`: automated tests.
- `tests/UtilityVerse.Copy.Test/`: console sample consuming the generator.
- `assets/`: repository artwork; `scripts/`: build automation.

Read `CLAUDE.md` for architecture details and `src/UtilityVerse.Copy/CLAUDE.md` before changing the generator or its tests.

## Build, Test, and Development Commands

Use the .NET 11 SDK (11.0.100 or a compatible patch, as specified in `global.json`). Run commands from the repository root:

- `./scripts/build.sh` (Linux/macOS) or `./scripts/build.ps1` (Windows): remove `bin`/`obj`, format changed C# files, restore, build, and test. Run before every commit touching C#.
- `dotnet build UtilityVerse.slnx`: build all projects and target frameworks.
- `dotnet test UtilityVerse.slnx`: run automated tests.
- `dotnet run --project tests/UtilityVerse.Copy.Test`: run the generator sample.
- `dotnet format UtilityVerse.slnx --include <paths>`: format only changed files.

## Coding Style & Naming Conventions

Follow existing C# style: four-space indentation, Allman braces, file-scoped namespaces, PascalCase types/methods, and camelCase locals/parameters. Preserve copyright headers. Name extension classes `{Type}Extension`; place related helpers in `Utility.*.cs` partial files.

Document public core members with XML summaries. Preserve compatibility across target frameworks and existing nullable settings. Core helpers use `UtilityVerseResult<T>` for errors; generator code follows its own conventions. Manage dependency versions centrally in `Directory.Packages.props`.

## Testing Guidelines

Tests use xUnit v3 with Microsoft.Testing.Platform. Add or update tests for changed behavior, including invalid inputs and regressions. Follow names such as `Method_ReturnTrue_WhenCondition`. Place generator/analyzer tests in `UtilityVerse.Copy.UnitTest`. No numeric coverage threshold is documented.

## Commit & Pull Request Guidelines

Use concise, imperative commit subjects, matching history: `Centralize build settings` or `COPY: Add automated test project`.

Discuss contributions in an issue first. Fork and branch from `development`; open PRs against `development`. Do not push directly to `master` or `development`. Describe behavior changes, link the issue, and report validation results. Include updated tests and scoped formatting changes.
