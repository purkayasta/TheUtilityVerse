// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.UnitTest;
public class DateOnlyExtensionTest
{
    [Fact]
    public void IsInBetween_ReturnsTrue_IfDateIsInBetween()
    {
        DateOnly? examDate = new(2022, 01, 01);
        DateOnly firstDate = new(2020, 01, 01);
        DateOnly secondDate = new(2023, 01, 01);
        var result = examDate.Value.IsInBetween(firstDate, secondDate);
        Assert.True(result.Result);
    }

    [Fact]
    public void IsInBetween_ReturnFalse_WhenDateIsDefault()
    {
        DateOnly examDate = new();
        DateOnly firstDate = new(2020, 01, 01);
        DateOnly secondDate = new(2023, 01, 01);
        Assert.False(examDate.IsInBetween(firstDate, secondDate).Result);
    }

    [Fact]
    public void IsInBetween_ThrowsException_WhenDateOnlyIsNull()
    {
        DateOnly? examDate = null;
        DateOnly firstDate = new(2020, 01, 01);
        DateOnly secondDate = new(2023, 01, 01);
        Assert.Throws<InvalidOperationException>(() => examDate.Value.IsInBetween(firstDate, secondDate));
    }

    [Fact]
    public void IsInBetween_ThrowsExeption_WhenSecondDateIsNull()
    {
        DateOnly? examDate = new();
        DateOnly? firstDate = new(2020, 01, 01);
        DateOnly? secondDate = null;
        Assert.Throws<InvalidOperationException>(() => examDate.Value.IsInBetween(firstDate.Value, secondDate.Value));
    }

    [Fact]
    public void IsInBetween_ThrowsException_WhenStartDateIsHigherThanEndDate()
    {
        DateOnly examDate = new(1993, 08, 05);
        DateOnly secondDate = new(1990, 01, 01);
        DateOnly firstDate = new(2000, 01, 01);
        Assert.False(examDate.IsInBetween(firstDate, secondDate).Result);
    }
}
