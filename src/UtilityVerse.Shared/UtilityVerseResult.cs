namespace UtilityVerse.Shared;

public class UtilityVerseResult<T>
{
    public T Result { get; set; }
    public string Error { get; set; } = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(Error);

    public static UtilityVerseResult<T> Success(T result) => new(result, string.Empty);

    public static UtilityVerseResult<T> Failure(string error) => new(default, error);

    public UtilityVerseResult(T result) => Result = result;

    public UtilityVerseResult(string error) => Error = error;

    public UtilityVerseResult(T result, string error)
    {
        Result = result;
        Error = error;
    }
}

