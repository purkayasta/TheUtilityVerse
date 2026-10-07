# Breaking Changes

- Targets `netstandard2.1`, `net6.0`, `net8.0`, `net9.0` and `net10.0`.
- Helpers no longer throw exceptions on invalid input. They return a `UtilityVerseResult<T>` carrying an error message (`HasError` / `Error`), so callers can log it and handle it their own way.
