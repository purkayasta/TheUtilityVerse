# Utility Verse Contribution Guideline

> Please read the [README](README.md) to understand what this project does, and go through the codebase first.

- Discuss what you want to contribute by creating an issue first.
- Don't push directly to `master` or `development`.
- Fork the repo, create a branch based on `development`, then open a PR against this repository's `development` branch.
- Add or update tests for your change and make sure it passes by running `scripts/build.ps1` (Windows) or `scripts/build.sh` (Linux/macOS), which clean, restore, build and test.
- Run `dotnet format` scoped to the C# files you changed (`--include <paths>`) before committing.
