## The UtilityVerse 🧰 - *(Multi verse of utility methods) is a library for all the helper methods that you might want to use.*

> There are multiple packages in this universe. Use whatever suits you.

1. [UtilityVerse](https://www.nuget.org/packages/UtilityVerse/)
2. [UtilityVerse.ASPNET](https://www.nuget.org/packages/UtilityVerse.ASPNET/)
3. [UtilityVerse.Copy](https://www.nuget.org/packages/UtilityVerse.Copy/)

Most helpers are extension methods. Static helpers live on the `Utility` class. Helpers that can fail return a `UtilityVerseResult<T>` (check `HasError` / `Error`) instead of throwing.

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
