// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.UnitTest;

public class IntExtensionTest
{
    [Fact]
    public void IsInBetween_ThrowException_IfMaxIsNull()
    {
        int source = 0;
        Assert.Throws<Exception>(() => source.IsInBetween(1, default).Result);
    }

    [Fact]
    public void IsInBetween_ThrowException_WhenMinIsGreaterThanMax()
    {
        int source = 0;
        Assert.Throws<Exception>(() => source.IsInBetween(1, 0).Result);
    }

    [Fact]
    public void IsInBetween_ReturnFalse_WhenWrongDataIsProvided()
    {
        int source = 10;
        Assert.False(source.IsInBetween(1, 9).Result);
    }

    [Fact]
    public void IsInBetween_ReturnTrue_WhenRightDataIsProvided()
    {
        int source = 100;
        Assert.True(source.IsInBetween(10, 200).Result);
    }
}
