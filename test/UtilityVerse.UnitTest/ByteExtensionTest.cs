using System.Text.Json;
using System.Text.Json.Nodes;

namespace UtilityVerse.UnitTest;

public class ByteExtensionTest
{
    [Fact]
    public void ToObject_ThrowsJsonException_WhenInvalidByteArrayProvided()
    {
        byte[] bytes = [1, 2, 3, 4, 5];
        var result = ByteExtension.ToObject(bytes);
        Assert.Throws<JsonException>(() => result);
    }

    [Fact]
    public void ToObject_ReturnProperObject_WhenValidJsonByteArrayIsProvided()
    {
        JsonObject jObject = new()
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };
        var byteArr = JsonSerializer.SerializeToUtf8Bytes(jObject);
        var result = ByteExtension.ToObject(byteArr);
        Assert.Equal(result.Result, jObject.AsObject());
    }
}
