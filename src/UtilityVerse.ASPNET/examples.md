# UtilityVerse.ASPNET examples

Import `UtilityVerse.ASPNET`. The request factories are useful for unit and
integration tests that need an HttpRequest with an optional query parameter.

Each C# block is an independent console program. Run blocks separately. They
include checks that throw if the illustrated behavior fails. Package verification
compiles and runs the blocks against installed packages. Sections identify
features that need framework-specific APIs.

[Empty and query requests](#empty-and-query-requests)

## Empty and query requests

Both public overloads return a request backed by a new `DefaultHttpContext`.
The query overload sets the supplied key and value in the query collection.

```csharp
using UtilityVerse.ASPNET;

var empty = Utility.CreateHttpRequest();
var request = Utility.CreateHttpRequest("name", "Ada");
request.Method = "GET";
request.Path = "/people";

if (empty.Query.Count != 0 || request.Query["name"] != "Ada" ||
    request.Method != "GET" || request.Path != "/people")
{
    throw new Exception("HTTP request construction failed.");
}
```
