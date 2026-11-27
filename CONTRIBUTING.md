# Contributing to The UtilityVerse

Discuss a proposed change in an issue first. Use the repository README to find the
package guide, then read the applicable `AGENTS.md` instructions before editing.
[Package guides](README.md#packages) · [Repository instructions](AGENTS.md)

## Branch and pull request

1. Fork the repository and create a branch from `development`.
2. Keep the change focused and add or update regression tests for changed behavior.
3. Run the validation workflow below.
4. Open a pull request against `development`, link the issue, and report validation results.

Push contributions to your feature branch. Describe consumer-visible behavior
changes and update the affected README and NuGet package documentation.

## Validate a change

Use .NET SDK **11.0.100** with compatible patch roll-forward, as selected by
`global.json`. Run commands from the repository root.

```sh
./scripts/build.sh
bash scripts/verify-packages.sh
```

On Windows, use `scripts/build.ps1` for the full build workflow. The build script
removes `bin` and `obj` under source and tests, formats modified and untracked C#
files, restores, builds, and tests. Include scoped formatting changes in the commit.
For an individual formatting pass, use `dotnet format UtilityVerse.slnx --include <paths>`.

Package verification builds Release packages and exercises fresh consumers on
.NET 8 and .NET 11. Copy changes must also follow the project's test-first workflow
and keep diagnostics, supported-shape documentation, and runtime behavior aligned.
[Copy instructions](src/UtilityVerse.Copy/AGENTS.md)
