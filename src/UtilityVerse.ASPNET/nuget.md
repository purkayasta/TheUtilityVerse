# UtilityVerse.ASPNET

<img src="https://raw.githubusercontent.com/purkayasta/TheUtilityVerse/development/assets/utility-verse.png" alt="UtilityVerse.ASPNET icon" width="64" height="64">

[![UtilityVerse.ASPNET NuGet version](https://img.shields.io/nuget/v/UtilityVerse.ASPNET?style=flat-square&logo=nuget&logoColor=white&label=NuGet&color=2563eb)](https://www.nuget.org/packages/UtilityVerse.ASPNET/) [![UtilityVerse.ASPNET total downloads](https://img.shields.io/nuget/dt/UtilityVerse.ASPNET?style=flat-square&logo=nuget&logoColor=white&label=downloads&color=16a34a)](https://www.nuget.org/packages/UtilityVerse.ASPNET/#statistics-body-tab)

HTTP helpers for ASP.NET Core applications and tests.

## Install

```sh
dotnet add package UtilityVerse.ASPNET
```

## Create an HTTP request

```csharp
using UtilityVerse.ASPNET;

var request = Utility.CreateHttpRequest("name", "value");
var value = request.Query["name"];
```

`Utility.CreateHttpRequest()` creates an `HttpRequest`, optionally with a query
string key and value.

## Related packages

| Package | Purpose |
| --- | --- |
| UtilityVerse | Core helpers and extension methods. |
| UtilityVerse.Copy | Generated deep and shallow copies, including depth configuration and stack safety. |

See the repository for package guides and contribution instructions.
[The UtilityVerse](https://github.com/purkayasta/TheUtilityVerse)

## Runnable examples

The package includes `examples.md` at its root with complete programs and expected
behavior for consumers and coding agents.
[Browse examples](https://github.com/purkayasta/TheUtilityVerse/blob/development/src/UtilityVerse.ASPNET/examples.md)
