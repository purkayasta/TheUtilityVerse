namespace UtilityVerse.Shared;

public class UtilityVerseResult<T>
{
    public T Result { get; set; }
    public string Error { get; set; } = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(Error);

    /// <summary>Creates a successful result carrying the supplied value, including string values.</summary>
    public static UtilityVerseResult<T> Success(T result) => new(result, string.Empty);

    /// <summary>Creates an error result carrying the supplied message and a default result value.</summary>
    public static UtilityVerseResult<T> Failure(string error) => new(default, error);

    /// <summary>Creates a result from a value. For string results, prefer Success to select value semantics explicitly.</summary>
    public UtilityVerseResult(T result) => Result = result;

    /// <summary>Creates a result from an error message. This overload treats a string argument as an error.</summary>
    public UtilityVerseResult(string error) => Error = error;

    /// <summary>Creates a result carrying both the supplied value and error message.</summary>
    public UtilityVerseResult(T result, string error)
    {
        Result = result;
        Error = error;
    }
}

