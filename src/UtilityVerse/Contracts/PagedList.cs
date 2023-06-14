// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


namespace UtilityVerse.Contracts;

/// <summary>
/// Pagination helper entity.
/// </summary>
/// <typeparam name="T"></typeparam>
public class PagedList<T>
{
    /// <summary>
    /// If the instance has any error or not. If error found then it will true.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Number of results.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// The collection.
    /// </summary>
    public IEnumerable<T> Results { get; set; } = Enumerable.Empty<T>();

    /// <summary>
    /// If there is any error, if found any this error object will be populated.
    /// </summary>
    public string? Errors { get; set; } = string.Empty;
}