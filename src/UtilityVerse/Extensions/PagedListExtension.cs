// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------

using System;
using System.Linq;
using UtilityVerse.Contracts;

namespace UtilityVerse.Extensions;

/// <summary>
/// PagedList Extension
/// </summary>
public static class PagedListExtension
{
    /// <summary>
    /// This will convert your list into a paged list. (Pagination helper).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="queryable"></param>
    /// <param name="index"></param>
    /// <param name="pageSize"></param>
    /// <returns>PagedList</returns>
    public static PagedList<T> ToPagedList<T>(this IQueryable<T> queryable, int index, int pageSize)
    {
        if (index == default || pageSize == default)
        {
            return new PagedList<T>
            {
                Errors = $"{nameof(index)} or {nameof(pageSize)} params value is invalid"
            };
        }

        var pagedList = new PagedList<T>();
        try
        {
            var result = queryable.Take(pageSize).Skip(index * pageSize).ToList();
            pagedList.Results = result;
        }
        catch (Exception exception)
        {
            pagedList.Errors = ExtractExceptionError(exception);
        }

        return pagedList;
    }

    private static string ExtractExceptionError(Exception exception)
    {
        if (exception is null)
        {
            return string.Empty;
        }
        return $"Exception Message: {exception.Message}. Inner Exception: {exception.InnerException?.Message}";
    }
}