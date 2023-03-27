using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace UtilityVerse
{
    public sealed partial class UtilityVerse
    {
        /// <summary>
        /// this is a method to create http request for unit/integration tests.
        /// </summary>
        /// <param name="queryStringKey"></param>
        /// <param name="queryStringValue"></param>
        /// <returns></returns>
        public static HttpRequest CreateHttpRequest(string queryStringKey, string queryStringValue)
        {
            var defaultRequest = new DefaultHttpContext();
            var httpRequest = defaultRequest.Request;
            httpRequest.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                {queryStringKey, queryStringValue}
            });
            return httpRequest;
        }

        /// <summary>
        /// this is a method to create httprequest object for unit/integration tests.
        /// </summary>
        /// <returns></returns>
        public static HttpRequest CreateHttpRequest() => new DefaultHttpContext().Request;
    }
}
