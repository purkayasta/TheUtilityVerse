# UtilityVerse

<img src="https://raw.githubusercontent.com/purkayasta/TheUtilityVerse/development/assets/utility-verse.png" alt="UtilityVerse icon" width="64" height="64">

[![UtilityVerse NuGet version](https://img.shields.io/nuget/v/UtilityVerse?style=flat-square&logo=nuget&logoColor=white&label=NuGet&color=2563eb)](https://www.nuget.org/packages/UtilityVerse/) [![UtilityVerse total downloads](https://img.shields.io/nuget/dt/UtilityVerse?style=flat-square&logo=nuget&logoColor=white&label=downloads&color=16a34a)](https://www.nuget.org/packages/UtilityVerse/#statistics-body-tab)

Extension methods and static helpers for collections, strings, dates, conversions,
retries, and pagination.

[Install](#install) · [Results](#results) · [Methods](#methods) · [Features](#features)

## Install

```sh
dotnet add package UtilityVerse
```

Import `UtilityVerse.Extensions` for extension methods and `UtilityVerse` for
static helpers on `Utility`.

## Results

Helpers using the result convention return `UtilityVerseResult<T>`. Check
`HasError` and `Error`, then read `Result` when successful. Construct results
explicitly with `Success(value)` or `Failure(error)`.

The package supports .NET Standard 2.1 and includes framework-specific builds
for modern .NET. NuGet selects a compatible assembly automatically.
The core package references UtilityVerse.Shared for its result and exception types.

## Methods

- Bool: `ToInt()`, `ToStr()`
- Byte[]: `ToObject()`, `To<T>()`
- Char[]: `ToStr()`
- Collections: `IsNullOrEmpty()` (enumerables, lists, sets, concurrent collections and more)
- DateOnly: `IsInBetween()`, `ToIntDate()`, `ToDateTime()`, `ToUtcDateTime()`
- DateTime: `IsInBetween()`, `ToUnixTimeStamp()`, `ToIntDate()`, `DestructFromDateTime()`, `ToUtcDateTime()`
- Decimal[] / Double[] / Int[]: `ToStr()`
- Generic / Object: `ToByteArray()`
- Int: `IsInBetween()`
- Long: `ToDateTime()`
- IQueryable: `ToPagedList()`
- ReadOnlySpan\<byte\>: `ToObject()`
- String: `ToBoolean()`, `IsNullOrEmptyOrWhiteSpace()`, `EncodeToBase64()`, `DecodeFromBase64()`, `ConvertOsWisePath()`, `ToSlugUrl()`, `IsIpAddress()`, `IsValidEmail()`, `ToStr()` (on `string[]`)

## Features

- Retry mechanism: `Utility.AddRetry()`, `Utility.AddRetryAsync()`
- Currency conversion: `Utility.DollarToCent()`, `Utility.CentToDollar()`

## Related packages

UtilityVerse.ASPNET provides HTTP helpers. UtilityVerse.Copy generates deep and
shallow copies with configurable depth limits and execution-stack safeguards.
See the repository for their guides and migration notes.
[The UtilityVerse](https://github.com/purkayasta/TheUtilityVerse)

## Runnable examples

The package includes `examples.md` at its root with complete programs and expected
behavior for consumers and coding agents.
[Browse examples](https://github.com/purkayasta/TheUtilityVerse/blob/development/src/UtilityVerse/examples.md)
