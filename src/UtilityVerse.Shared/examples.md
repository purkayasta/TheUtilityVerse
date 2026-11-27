# UtilityVerse.Shared examples

Use explicit factories to distinguish successful string values from error messages.
This repository support project targets .NET Standard 2.1.

Each C# block is an independent console program. Run blocks separately. They
include checks that throw if the illustrated behavior fails. Repository
verification compiles and runs these blocks with the core consumer.

[Result factories and legacy constructors](#result-factories-and-legacy-constructors) · [Exception guards](#exception-guards)

## Result factories and legacy constructors

`HasError` reflects whether `Error` contains a nonempty message. `Failure` should
receive a meaningful nonempty error. The legacy string constructor represents an
error, including when `T` is string.

```csharp
using UtilityVerse.Shared;

var success = UtilityVerseResult<string>.Success("ready");
var failure = UtilityVerseResult<string>.Failure("A name is required.");
var numeric = new UtilityVerseResult<int>(42);
var legacyError = new UtilityVerseResult<string>("A legacy error.");
var legacySuccess = new UtilityVerseResult<string>("ready", string.Empty);
var partial = new UtilityVerseResult<int>(7, "Partial result.");

if (success.HasError || success.Result != "ready" || !failure.HasError ||
    numeric.Result != 42 || !legacyError.HasError || legacySuccess.HasError ||
    !partial.HasError || partial.Result != 7)
{
    throw new Exception("Result semantics failed.");
}
```

## Exception guards

These methods throw `System.Exception`. The supplied parameter name appears in
guard failures. Null, empty, and whitespace strings are rejected by the string
guard. The type is a static guard container despite its name.

```csharp
using UtilityVerse.Shared;

UtilityVerseException.ThrowIfNull(new object(), "resource");
UtilityVerseException.ThrowIfNullOrEmpty("ready", "name");

var failures = new Action[]
{
    () => UtilityVerseException.ThrowIfNull(null, "resource"),
    () => UtilityVerseException.ThrowIfNullOrEmpty("  ", "name"),
    () => UtilityVerseException.Throw("Explicit failure.")
};

foreach (var action in failures)
{
    var failed = false;

    try
    {
        action();
    }
    catch (Exception error)
    {
        failed = error.Message.Length > 0;
    }

    if (!failed)
    {
        throw new Exception("Expected guard failure.");
    }
}
```
