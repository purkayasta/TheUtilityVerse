# Breaking Changes

- Targets `netstandard2.1`, `net6.0`, `net8.0`, `net9.0`, `net10.0` and `net11.0`.
- Helpers no longer throw exceptions on invalid input. They return a `UtilityVerseResult<T>` carrying an error message (`HasError` / `Error`), so callers can log it and handle it their own way.


## Stability fixes (unreleased)

Public helper signatures and result constructors remain available. These fixes
intentionally change incorrect behavior:

- Retry Count means total attempts, success returns immediately, waits are honored,
  options are not mutated, and the final exception is propagated. Use
  `AddRetryTaskAsync<T>(Func<Task<T>>, RetryOption)` for handlers that await work;
  the existing `AddRetryAsync<T>(Func<T>, RetryOption)` remains for synchronous handlers
  with asynchronous waits.
- Pagination uses a zero-based page index, skips before taking, rejects invalid
  arguments, and reports IsValid=true only when no error occurred.
- Array ToStr helpers return successful string results via explicit result factories.
  Legacy `UtilityVerseResult<string>(string)` still constructs an error; use
  `.Success(value)` or `.Failure(error)` to make intent unambiguous.
- Byte conversion consistently uses JSON, including public fields. Struct buffers
  from the previous native-memory implementation are not a supported wire format;
  regenerate them as JSON. Invalid buffers return an error result.

## Copy package migration

UtilityVerse.Copy defaults to **5 graph levels**, with the root at **level 1**.
For deeper graphs, configure `[DeepCopy(depth: 10)]` or `[CopyDepth(10)]` with
`IDeepCopy`, then rebuild the consumer. Valid depths are **1–100**. Invalid values
produce UV006 and fall back to 5. The root's budget covers the entire operation.

Exceeding the depth budget throws `NotSupportedException` and clears partial copy
state. Execution-stack checks can reject an operation before its configured limit.
An independent compiler traversal guard reports excessive type nesting with UV004.

Shallow copies share array references alongside other reference fields. Deep
copies of `Stack<T>` preserve LIFO order, and copies of `Queue<T>` preserve FIFO
order. Both containers count as one graph level.

See the full contract, constructor restrictions, and diagnostic fixes in the Copy
guide. [UtilityVerse.Copy guide](../UtilityVerse.Copy/README.md)
