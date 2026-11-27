# UtilityVerse examples

Import `UtilityVerse.Extensions` for extension methods, `UtilityVerse` for static
helpers, and `UtilityVerse.Contracts` for options and enums. Core also references
UtilityVerse.Shared. Result-returning methods expose `HasError`, `Error`, and `Result`.
Some APIs throw on invalid input as described in the examples below.

The package also targets .NET Standard 2.1. JSON, DateOnly, and selected platform
APIs in these examples require the modern .NET targets.

Each C# block is an independent console program. Run blocks separately. They
include checks that throw if the illustrated behavior fails. Package verification
compiles and runs the blocks against installed packages. Sections identify
features that need framework-specific APIs.

[Strings and booleans](#strings-and-booleans) · [Collections and array formatting](#collections-and-array-formatting) · [Dates numbers and platform information](#dates-numbers-and-platform-information) · [JSON byte conversion](#json-byte-conversion) · [Retries and pagination](#retries-and-pagination)

## Strings and booleans

Boolean parsing accepts true/false, yes/no, and 1/0 ignoring case. Empty input
returns false. Invalid boolean text and malformed Base64 throw. Email and IP
helpers apply the package's regular expressions.

```csharp
using UtilityVerse.Contracts;
using UtilityVerse.Extensions;

if (!"YES".ToBoolean() || "0".ToBoolean() || true.ToInt() != 1 || false.ToStr() != "False")
{
    throw new Exception("Boolean conversion failed.");
}

if (!"  ".IsNullOrEmptyOrWhiteSpace() || new[] { "a", "b" }.ToStr(",") != "a,b")
{
    throw new Exception("String helpers failed.");
}

foreach (var encoding in new[] { EncodingEnum.UTF8, EncodingEnum.UTF32, EncodingEnum.Unicode, EncodingEnum.Ascii, EncodingEnum.Latin })
{
    if ("hello".EncodeToBase64(encoding).DecodeFromBase64(encoding) != "hello")
    {
        throw new Exception("Encoding round trip failed.");
    }
}

if (!"person@example.com".IsValidEmail() || !"127.0.0.1".IsIpAddress() || "Hello World".ToSlugUrl() != "hello-world")
{
    throw new Exception("Validation or slug creation failed.");
}

var path = "folder\\file.txt".ConvertOsWisePath();
if (path != Path.Combine("folder", "file.txt"))
{
    throw new Exception("Platform path conversion failed.");
}

try
{
    "unknown".ToBoolean();
    throw new Exception("Expected invalid boolean input to fail.");
}
catch (FormatException)
{
}

try
{
    "***".DecodeFromBase64();
    throw new Exception("Expected invalid Base64 input to fail.");
}
catch (FormatException)
{
}
```

## Collections and array formatting

Collection overloads accept null and empty inputs. Enumerable and queryable
checks can enumerate the input or execute a provider query. Array formatting
returns a result wrapper for numeric and character arrays; string arrays return
a string directly. Numeric formatting follows the current culture.

```csharp
using System.Collections.Concurrent;
using UtilityVerse.Extensions;

var checks = new[]
{
    ((IEnumerable<int>)Array.Empty<int>()).IsNullOrEmpty(),
    ((IList<int>)new List<int>()).IsNullOrEmpty(),
    new List<int>().IsNullOrEmpty(),
    ((ICollection<int>)new List<int>()).IsNullOrEmpty(),
    ((IReadOnlyCollection<int>)Array.Empty<int>()).IsNullOrEmpty(),
    ((IReadOnlyList<int>)Array.Empty<int>()).IsNullOrEmpty(),
    ((IReadOnlySet<int>)new HashSet<int>()).IsNullOrEmpty(),
    Array.Empty<int>().OrderBy(value => value).IsNullOrEmpty(),
    Array.Empty<int>().AsQueryable().OrderBy(value => value).IsNullOrEmpty(),
    new ConcurrentBag<int>().IsNullOrEmpty(),
    new ConcurrentQueue<int>().IsNullOrEmpty(),
    new ConcurrentStack<int>().IsNullOrEmpty(),
    ((IProducerConsumerCollection<int>)new ConcurrentBag<int>()).IsNullOrEmpty(),
    ((IEnumerable<int>?)null).IsNullOrEmpty()
};

if (checks.Any(empty => !empty) || new List<int> { 1 }.IsNullOrEmpty())
{
    throw new Exception("Collection checks failed.");
}

var integers = new[] { 1, 2 }.ToStr(",");
var decimals = new[] { 1m, 2m }.ToStr(",");
var doubles = new[] { 1d, 2d }.ToStr(",");
var characters = new[] { 'a', 'b' }.ToStr("-");

if (integers.HasError || decimals.HasError || doubles.HasError || characters.HasError ||
    integers.Result != "1,2" || decimals.Result != "1,2" || doubles.Result != "1,2" || characters.Result != "a-b")
{
    throw new Exception("Array formatting failed.");
}

if (!Array.Empty<int>().ToStr().HasError || !Array.Empty<decimal>().ToStr().HasError ||
    !Array.Empty<double>().ToStr().HasError || !Array.Empty<char>().ToStr().HasError)
{
    throw new Exception("Expected empty arrays to return errors.");
}
```

## Dates numbers and platform information

Range checks are inclusive and report reversed bounds through an error result.
Date integer formatting returns an error if the formatted text cannot be parsed.
`ToUtcDateTime` constructs a UTC-kind value from the calendar components; its
DateTime overload defaults to midnight and optionally retains hours, minutes,
and seconds. Unix conversion back to DateTime requires a positive timestamp and
returns an unspecified-kind DateTime. Unsupported units and out-of-range values
can throw. Currency helpers multiply or divide by 100 without rounding.

```csharp
using UtilityVerse;
using UtilityVerse.Contracts;
using UtilityVerse.Extensions;

var date = new DateTime(2026, 11, 27, 14, 30, 15, DateTimeKind.Utc);
var day = DateOnly.FromDateTime(date);

if (!date.IsInBetween(date.AddDays(-1), date.AddDays(1)).Result ||
    !day.IsInBetween(day.AddDays(-1), day.AddDays(1)).Result ||
    !5.IsInBetween(1, 10).Result || !5.IsInBetween(10, 1).HasError)
{
    throw new Exception("Inclusive range checks failed.");
}

if (date.ToIntDate().Result != 20261127 || day.ToIntDate("yyyyMMdd").Result != 20261127 ||
    !date.ToIntDate("'invalid'").HasError)
{
    throw new Exception("Date formatting failed.");
}

foreach (var unit in new[] { TimeEnum.Second, TimeEnum.MilliSecond })
{
    var timestamp = date.ToUnixTimeStamp(unit);
    var restored = timestamp.Result.ToDateTime(unit);

    if (timestamp.HasError || restored.HasError || restored.Result != date)
    {
        throw new Exception("Unix conversion failed.");
    }
}

if (!0L.ToDateTime(TimeEnum.Second).HasError || day.ToDateTime().Kind != DateTimeKind.Local ||
    day.ToUtcDateTime().Kind != DateTimeKind.Utc || date.ToUtcDateTime().Hour != 0 ||
    date.ToUtcDateTime(includeTime: true).Hour != 14 || date.DestructFromDateTime().year != 2026)
{
    throw new Exception("Date construction failed.");
}

if (Utility.DollarToCent(12.34m).Result != 1234m || Utility.CentToDollar(1234m).Result != 12.34m)
{
    throw new Exception("Currency conversion failed.");
}

Console.WriteLine($"Windows={Utility.IsItWindows}, Linux={Utility.IsItLinux}, macOS={Utility.IsItMac}, BSD={Utility.IsBsd}");
Console.WriteLine(Utility.FrameworkRuntime);
```

## JSON byte conversion

JSON serialization includes public fields on the modern .NET targets. Typed
`To<T>()` restores the requested model. Untyped byte and span deserialization
returns JSON values such as `JsonElement`. Check error results before reading
values. The span overload uses its own default serializer options.

```csharp
using UtilityVerse.Extensions;

var source = new Payload { Name = "sample", Count = 3 };
var typedBytes = GenericExtension.ToByteArray(source);
var objectBytes = ObjectExtension.ToByteArray((object)source);

if (typedBytes.HasError || objectBytes.HasError)
{
    throw new Exception("Serialization failed.");
}

var restored = typedBytes.Result.To<Payload>();
var untyped = objectBytes.Result.ToObject();
ReadOnlySpan<byte> span = objectBytes.Result;
var spanResult = span.ToObject();

if (restored.HasError || restored.Result.Count != 3 || restored.Result.Name != "sample" ||
    untyped.HasError || spanResult.HasError || !new byte[] { 255 }.To<Payload>().HasError)
{
    throw new Exception("JSON conversion failed.");
}

public sealed class Payload
{
    public string Name { get; set; } = "";
    public int Count;
}
```

## Retries and pagination

`RetryOption.Count` is the total attempt count, including the first attempt.
The last exception propagates. `AddRetryAsync` runs a synchronous handler with
asynchronous waits. Use `AddRetryTaskAsync` when the handler returns a Task.
Invalid counts and delays throw. Pagination uses a zero-based page index and
materializes the selected page. Query failures populate `Errors`.

```csharp
using UtilityVerse;
using UtilityVerse.Contracts;
using UtilityVerse.Extensions;

var options = new RetryOption { Count = 3, Delay = TimeSpan.Zero };
var attempts = 0;
var value = Utility.AddRetry(() =>
{
    attempts++;

    if (attempts < 3)
    {
        throw new InvalidOperationException("Try again.");
    }

    return 42;
}, options);

var synchronous = await Utility.AddRetryAsync(() => 42, options);
var asynchronous = await Utility.AddRetryTaskAsync(async () =>
{
    await Task.Yield();
    return 42;
}, options);

if (value != 42 || attempts != 3 || synchronous != 42 || asynchronous != 42 || options.Count != 3)
{
    throw new Exception("Retry behavior failed.");
}

try
{
    Utility.AddRetry<int>(() => throw new InvalidOperationException("Final failure."), options);
    throw new Exception("Expected retry exhaustion.");
}
catch (InvalidOperationException)
{
}

var query = Enumerable.Range(1, 10).AsQueryable();
var page = query.ToPagedList(index: 1, pageSize: 3);

if (!page.IsValid || page.Count != 3 || !page.Results.SequenceEqual(new[] { 4, 5, 6 }) ||
    query.ToPagedList(-1, 3).IsValid || query.ToPagedList(0, 0).IsValid)
{
    throw new Exception("Pagination failed.");
}
```
