// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ---------------------------------------------------------------


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;

namespace UtilityVerse.ASPNET;

public sealed partial class Utility
{
    /// <summary>
    /// this is a method to create HTTP request for unit/integration tests.
    /// </summary>
    /// <param name="queryStringKey"></param>
    /// <param name="queryStringValue"></param>
    /// <returns></returns>
    public static HttpRequest CreateHttpRequest(string queryStringKey, string queryStringValue)
    {
        var defaultRequest = new DefaultHttpContext();
        var httpRequest = defaultRequest.Request;
        httpRequest.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            {queryStringKey, queryStringValue}
        });
        return httpRequest;
    }

    /// <summary>
    /// this is a method to create HTTP request object for unit/integration tests.
    /// </summary>
    /// <returns></returns>
    public static HttpRequest CreateHttpRequest() => new DefaultHttpContext().Request;
}
