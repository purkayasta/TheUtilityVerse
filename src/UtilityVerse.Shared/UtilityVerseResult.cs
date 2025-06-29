namespace UtilityVerse.Shared;

public class UtilityVerseResult<T>
{
    public T Result { get; set; }
    public string Error { get; set; } = string.Empty;
    public bool HasError => Error.Length > 0;

    public UtilityVerseResult(T result) => Result = result;

    public UtilityVerseResult(string error) => Error = error;

    public UtilityVerseResult(T result, string error)
    {
        Result = result;
        Error = error;
    }
}

