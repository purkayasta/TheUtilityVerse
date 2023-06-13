using UtilityVerse.Contracts;

namespace UtilityVerse.Extensions;

public static class PagedListExtension
{
    public static PagedList<T> ToPagedList<T>(this IQueryable<T> queryable, int index, int pageSize)
    {
        if (index == default || pageSize == default)
            throw new InvalidDataException($"{nameof(index)} or {nameof(pageSize)} params value is invalid");

        var pagedList = new PagedList<T>();
        try
        {
            var result = queryable.Take(pageSize).Skip(index * pageSize).ToList();
            pagedList.Results = result;
            pagedList.Count = result.Count;
            pagedList.IsValid = true;
        }
        catch (Exception exception)
        {
            pagedList.IsValid = false;
            pagedList.Errors = ExtractExceptionError(exception);
        }

        return pagedList;
    }


    private static string ExtractExceptionError(Exception? exception)
        => exception is null
            ? string.Empty
            : $"Exception Message: {exception.Message}. Inner Exception: {exception.InnerException?.Message}";
}