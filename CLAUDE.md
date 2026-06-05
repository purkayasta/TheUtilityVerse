# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project Overview

**TheUtilityVerse** is a multi-package C# utility library with three main packages:

1. **UtilityVerse** — Core utility methods with no dependencies (supports .NET 6.0, 8.0, 9.0, and netstandard2.1)
2. **UtilityVerse.ASPNET** — ASP.NET Core-specific extensions (supports multiple .NET versions)
3. **UtilityVerse.Copy** — Roslyn-based source generator for automatic `DeepCopy()` and `ShallowCopy()` method generation

All packages are published to NuGet. The repository uses a monorepo structure with source in `src/` and tests in `test/`.

---

## Build & Test Commands

```bash
# Build the entire solution
dotnet build UtilityVerse-Soln.sln

# Build a specific project (e.g., UtilityVerse.Copy)
dotnet build src/UtilityVerse.Copy/UtilityVerse.Copy.csproj

# Run all tests
dotnet test UtilityVerse-Soln.sln

# Run tests for a specific test project
dotnet test test/UtilityVerse.Copy.Test/UtilityVerse.Copy.Test.csproj

# Run a specific test by filter (example)
dotnet test --filter "DepthTest" test/UtilityVerse.Copy.Test/UtilityVerse.Copy.Test.csproj

# Pack the NuGet package (useful for testing package generation)
dotnet pack src/UtilityVerse.Copy/UtilityVerse.Copy.csproj

# Clean build artifacts
dotnet clean UtilityVerse-Soln.sln
```

---

## Project-Specific Guidance

### UtilityVerse (Core)

**Purpose**: General-purpose utility methods with zero dependencies.

**Key Details**:
- Multi-targeted: netstandard2.1, net6.0, net8.0, net9.0
- Entry point: `Utility` class (sealed, partial) in `src/UtilityVerse/Utility.cs`
- Utility methods are static helpers added to the `Utility` partial class
- Strict compilation settings: `TreatWarningsAsErrors` enabled, overflow checking enabled
- XML documentation generation enabled (`GenerateDocumentationFile`)
- Shared types come from `UtilityVerse.Shared` project (dependency)

**When working on this project**:
- Add new utility methods to the `Utility` partial class
- Run `dotnet test test/UtilityVerse.UnitTest/` to validate changes
- Ensure backward compatibility across supported frameworks

### UtilityVerse.ASPNET

**Purpose**: ASP.NET Core-specific utility extensions and helpers.

**Key Details**:
- Multi-targeted: netstandard2.1, net6.0, net8.0, net9.0
- Depends on: `Microsoft.AspNetCore.Http` and `Microsoft.AspNetCore.Http.Abstractions`
- Also uses strict compilation settings (warnings as errors, overflow checking)
- No unit test project currently; consider extending test coverage if adding features

**When working on this project**:
- Focus on utilities that enhance ASP.NET Core middleware, dependency injection, or HTTP handling
- Test manually in a sample ASP.NET Core app or expand test projects as needed

### UtilityVerse.Shared

**Purpose**: Shared types and interfaces used across multiple projects.

**Key Details**:
- Lightweight library (netstandard2.1) shared by both `UtilityVerse` and potentially others
- Contains common types and interfaces to avoid circular dependencies
- Minimal content; extend carefully

---

## Key Architecture: UtilityVerse.Copy Source Generator

The **UtilityVerse.Copy** project is a Roslyn-based source generator that automatically creates `DeepCopy()` and `ShallowCopy()` methods at compile-time. Understanding this architecture is critical for most development tasks.

It targets **netstandard2.0** — required for Roslyn analyzers/source generators (do not bump this). The test projects that consume it target net10.0.

### Entry Points

- **CopyGenerator.cs** — Main entry point (`IIncrementalGenerator` implementation). Orchestrates the scanning of types, validation, and delegation to specific generators.
- **Generators/DeepCopyGenerator.cs** — Generates recursive deep copy methods with circular reference handling and optional depth limiting.
- **Generators/ShallowCopyGenerator.cs** — Generates shallow copy methods.

### Support Classes

- **Helper.cs** — Provides utilities for:
  - Extracting depth metadata from attributes (`[DeepCopy(depth)]`)
  - Checking if types are `partial`
  - Generating property copy assignments with depth checks
  - Type classification (primitive, collection, custom type, etc.)

- **Analyzers/ValidationAnalyzer.cs** — Validates decorated types at compile-time:
  - Ensures types are `partial`
  - Warns if abstract types are decorated
  - Validates attribute usage

- **Analyzers/Rules.cs** — Diagnostic descriptors (IDs, messages, severities) reported by the analyzer
- **Attributes/** — Simple marker attributes (`[DeepCopy]`, `[ShallowCopy]`) with optional depth parameter
- **Interfaces/** — Marker interfaces (`IDeepCopy`, `IShallowCopy`) as alternative to attributes

### How It Works

1. **Type Detection**: `CopyGenerator` scans for types decorated with attributes or implementing marker interfaces
2. **Validation**: Checks if types are `partial` and meet other requirements
3. **Generation**: Delegates to `DeepCopyGenerator` or `ShallowCopyGenerator`
4. **Output**: Generated methods are added as source files during compilation (visible in `/obj/` directory)

### Depth Limiting (Recent Feature)

- Attributes accept an optional `maxDepth` parameter: `[DeepCopy(2)]`
- `Helper.ExtractDepth()` parses this from the attribute
- Deep copy generation adds guards to stop recursion at the specified depth
- Circular references are handled via an `objectMap` dictionary that tracks already-copied objects

### Key Files for Reference

- **Generators/DeepCopyGenerator.cs** — Depth extraction and type validation
- **Helper.cs** — Property assignment generation, type classification, and depth checks (`DefaultDepth = 5`)
- **Test files** (`test/UtilityVerse.Copy.Test/` and `test/UtilityVerse.Copy.UnitTest/`) — Examples of how depth and circular references are tested

---

## Important Development Notes

### Classes Must Be Partial

For the generator to work, any class, struct, or record you decorate with `[DeepCopy]` or `[ShallowCopy]` must be declared as `partial`. The generator produces a partial implementation.

```csharp
[DeepCopy]
public partial class MyClass { ... }
```

### Generated Code Location

Generated source files are created in:
```
obj/Debug/[TargetFramework]/generated/
```

You can inspect them during development to verify correctness.

### Testing Source Generators

- The `UtilityVerse.Copy.Test` and `UtilityVerse.Copy.UnitTest` projects target net10.0 (the generator itself is netstandard2.0)
- Tests exercise the generated code by instantiating classes with copy methods
- Use `DepthTest.cs` as a reference for testing depth-limiting behavior with circular references

### Circular Reference Handling

The deep copy generator uses an internal `Dictionary<object, object> objectMap` to track copied objects and prevent infinite recursion when circular references exist. This is critical for real-world models with parent-child relationships.

---

## Branch Strategy & Contribution Workflow

- **master branch** → production releases (the repo's default branch)
- **development branch** → integration branch for features
- **Feature branches** → created from `development`, prefixed meaningfully (e.g., `add-depth`, `fix-circular-refs`)
- Never push directly to `master` or `development`; open a PR targeting `development`

Per CONTRIBUTING.md:
1. Create an issue first to discuss your contribution
2. Fork the repo, create a branch from `development`
3. Submit a PR to merge back to `development` (not directly to master)

---

## Solution Structure

```
UtilityVerse-Soln.sln          # Main solution file
├── src/
│   ├── UtilityVerse/           # Core utilities
│   ├── UtilityVerse.ASPNET/    # ASP.NET extensions
│   ├── UtilityVerse.Copy/      # Source generator (Roslyn)
│   └── UtilityVerse.Shared/    # Shared types
└── test/
    ├── UtilityVerse.UnitTest/
    ├── UtilityVerse.Copy.Test/
    └── UtilityVerse.Copy.UnitTest/
```

Each project is multi-targeted (supports multiple .NET versions). Check `.csproj` files for specific `<TargetFramework>` entries.

---

## Code Style

- All projects enable implicit `using` statements; nullable reference types (`<Nullable>enable</Nullable>`) are on everywhere **except** the core `UtilityVerse` project
- C# latest language features are enabled (`<LangVersion>latest</LangVersion>`)
- Follow the existing code structure (e.g., how `Helper.cs` is organized, how generators delegate)

---

## Common Tasks

### Adding a new feature to DeepCopy/ShallowCopy

1. Understand the feature by reading relevant test files first
2. Modify `Helper.cs` if it affects type classification or property assignment logic
3. Update `DeepCopyGenerator.cs` or `ShallowCopyGenerator.cs` as needed
4. Add validation rules in `ValidationAnalyzer.cs` if the feature has restrictions
5. Write tests in `test/UtilityVerse.Copy.Test/` to verify behavior
6. Run `dotnet test` to ensure no regressions

### Debugging the generator

- Inspect generated code in `obj/Debug/[framework]/generated/`
- Add diagnostic messages using `context.ReportDiagnostic()` in validation
- Use sample classes with `[DeepCopy]` attributes in test projects to verify output

### Updating depth semantics or circular reference handling

- Changes typically go in `Helper.cs` (property assignment logic) and `DeepCopyGenerator.cs` (recursion depth checks)
- Test with classes that have circular references (like Company ↔ Employee models in tests)
- Verify both shallow and deep copy behavior doesn't regress

---

## Cross-Project Workflow

Since this is a monorepo with independent but related projects:

1. **Use single `dotnet test`** — `dotnet test UtilityVerse-Soln.sln` runs all tests at once, ensuring changes in one project don't break others
2. **Each project is independently packable** — You can pack/publish individual projects or the whole solution
3. **Shared code** — `UtilityVerse.Shared` provides types reused across projects to avoid duplication
4. **No inter-package dependencies** — Each NuGet package is independent; users can pick which ones they need

### Making Changes Across Multiple Projects

If your change spans multiple projects (e.g., adding a shared type):

1. Start with `UtilityVerse.Shared` (shared types)
2. Update dependent projects (`UtilityVerse`, `UtilityVerse.ASPNET`, `UtilityVerse.Copy` if applicable)
3. Run `dotnet build UtilityVerse-Soln.sln` to verify no circular dependencies
4. Run `dotnet test UtilityVerse-Soln.sln` to ensure all tests pass
