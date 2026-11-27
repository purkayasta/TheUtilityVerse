# The UtilityVerse

C# utility packages for everyday helpers, ASP.NET Core, and generated object copies.
Choose the package your project needs.

[Packages](#packages) · [Object copying](#object-copying) ·
[Build and contribute](#build-and-contribute)

## Packages

| Package | NuGet | Downloads | Purpose | Documentation |
| --- | --- | --- | --- | --- |
| <img src="assets/utility-verse.png" alt="UtilityVerse icon" width="36" height="36"> [UtilityVerse](https://www.nuget.org/packages/UtilityVerse/) | [![UtilityVerse NuGet version](https://img.shields.io/nuget/v/UtilityVerse?style=flat-square&logo=nuget&logoColor=white&label=NuGet&color=2563eb)](https://www.nuget.org/packages/UtilityVerse/) | [![UtilityVerse total downloads](https://img.shields.io/nuget/dt/UtilityVerse?style=flat-square&logo=nuget&logoColor=white&label=downloads&color=16a34a)](https://www.nuget.org/packages/UtilityVerse/#statistics-body-tab) | Extension methods, retry helpers, pagination, encoding, and conversions. | [Core guide](src/UtilityVerse/nuget.md) |
| <img src="assets/utility-verse.png" alt="UtilityVerse.ASPNET icon" width="36" height="36"> [UtilityVerse.ASPNET](https://www.nuget.org/packages/UtilityVerse.ASPNET/) | [![UtilityVerse.ASPNET NuGet version](https://img.shields.io/nuget/v/UtilityVerse.ASPNET?style=flat-square&logo=nuget&logoColor=white&label=NuGet&color=2563eb)](https://www.nuget.org/packages/UtilityVerse.ASPNET/) | [![UtilityVerse.ASPNET total downloads](https://img.shields.io/nuget/dt/UtilityVerse.ASPNET?style=flat-square&logo=nuget&logoColor=white&label=downloads&color=16a34a)](https://www.nuget.org/packages/UtilityVerse.ASPNET/#statistics-body-tab) | ASP.NET Core HTTP helpers. | [ASP.NET guide](src/UtilityVerse.ASPNET/nuget.md) |
| <img src="src/UtilityVerse.Copy/Assets/copy.png" alt="UtilityVerse.Copy icon" width="36" height="36"> [UtilityVerse.Copy](https://www.nuget.org/packages/UtilityVerse.Copy/) | [![UtilityVerse.Copy NuGet version](https://img.shields.io/nuget/v/UtilityVerse.Copy?style=flat-square&logo=nuget&logoColor=white&label=NuGet&color=2563eb)](https://www.nuget.org/packages/UtilityVerse.Copy/) | [![UtilityVerse.Copy total downloads](https://img.shields.io/nuget/dt/UtilityVerse.Copy?style=flat-square&logo=nuget&logoColor=white&label=downloads&color=16a34a)](https://www.nuget.org/packages/UtilityVerse.Copy/#statistics-body-tab) | Generated deep and shallow copies for partial C# models. | [Copy guide](src/UtilityVerse.Copy/README.md) |

Core and ASP.NET include **.NET Standard 2.1** builds. Copy targets
**.NET Standard 2.0** for compiler-host compatibility. NuGet selects a compatible
assembly for the consuming project. Feature-specific requirements appear in the
package guides.

Shared contains the repository support types used by core helpers.
[Shared examples](src/UtilityVerse.Shared/examples.md)

```sh
dotnet add package UtilityVerse
# Or install the copy generator.
dotnet add package UtilityVerse.Copy
```

Core extension methods live in `UtilityVerse.Extensions`. Static helpers live on
`UtilityVerse.Utility`. Helpers using the result convention return
`UtilityVerseResult<T>` with `Result`, `HasError`, and `Error`.

## Object copying

Use `[DeepCopy]` or `IDeepCopy` on a partial model, then call `DeepCopy()`.
Use `[ShallowCopy]` or `IShallowCopy` to generate `ShallowCopy()`.

Deep copies preserve supported mutable cycles and shared reference identity.
Supported containers include lists, dictionaries, queues, and stacks. `Stack<T>`
keeps LIFO order, and `Queue<T>` keeps FIFO order.

Depth defaults to **5**, with the root at **level 1**. Set `[DeepCopy(depth: 10)]`
or add `[CopyDepth(10)]` alongside `IDeepCopy` to choose **1–100**. Invalid values
produce a compiler warning and fall back to 5. Runtime depth and execution-stack
guards fail clearly when the copy cannot continue safely.

The Copy guide covers installation, examples, supported types, compiler diagnostics,
and the independent compiler traversal guard. [Read the Copy guide](src/UtilityVerse.Copy/README.md)

## Build and contribute

Repository development uses .NET SDK **11.0.100**, as selected by `global.json`.

```sh
./scripts/build.sh
bash scripts/verify-packages.sh
```

Windows contributors can use `scripts/build.ps1`. The build script cleans outputs,
formats changed C# files, restores, builds, and tests. Package verification runs
fresh installed consumers.

Discuss contributions in an issue and open pull requests against `development`.
See the contribution workflow and core migration notes.
[Contributing](CONTRIBUTING.md) · [Core behavior changes](src/UtilityVerse/breaking-changes.md)

## Runnable examples

Each package ships an `examples.md` with complete C# programs covering its public
API families and error handling. XML method documentation ships alongside the
assemblies for IDEs and coding tools.

- [Core examples](src/UtilityVerse/examples.md)
- [Copy examples](src/UtilityVerse.Copy/examples.md)
- [ASP.NET examples](src/UtilityVerse.ASPNET/examples.md)
- [Shared repository examples](src/UtilityVerse.Shared/examples.md)

Package verification compiles and runs the example programs against installed packages.
