namespace UtilityVerse.Contracts;

public class PagedList<T>
{
    public bool IsValid { get; set; }
    public int Count { get; set; }
    public IEnumerable<T> Results { get; set; } = Enumerable.Empty<T>();
    public string? Errors { get; set; } = string.Empty;
}