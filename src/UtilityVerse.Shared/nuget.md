# UtilityVerse.Shared

Repository support project providing result and exception types for UtilityVerse.
Targets .NET Standard 2.1.

## Construct a result

```csharp
using UtilityVerse.Shared;

var success = UtilityVerseResult<string>.Success("ready");
var failure = UtilityVerseResult<string>.Failure("A value is required.");
```

Check `HasError` and `Error`, then read `Result` when successful. Explicit factories
make string results unambiguous. Existing constructors remain available.

UtilityVerse references this package for its shared contracts.

## Repository examples

Complete programs for contributors and coding agents cover factories, legacy
constructors, and exception guards. [Examples](examples.md)
