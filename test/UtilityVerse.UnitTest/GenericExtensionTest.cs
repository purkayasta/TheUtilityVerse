// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.UnitTest;

public class GenericExtensionTest
{
    internal record User(string Name);

    [Fact]
    public void ToByteArray_ReturnsValidByteArray_WhenValidInputIsProvided()
    {
        var user = new User("PRITOM PURKAYASTA");
        var expected = user.ToByteArray().Result;
        Assert.Equal(expected.To<User>().Result, user);
    }

    [Fact]
    public void To_ThrowException_WhenNullByteArrayIsGiven()
    {
        byte[] array = default;
        Assert.Throws<Exception>(() => array.To<User>().Result);
    }

    [Fact]
    public void ToByteArray_ThrowsException_WhenNullClassIsProvided()
    {
        User user = default;
        Assert.Throws<Exception>(() => user.ToByteArray().Result);
    }
}
