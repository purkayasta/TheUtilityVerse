﻿// ---------------------------------------------------------------
// Copyright (c) Pritom Purkayasta All rights reserved.
// FREE TO USE TO CONNECT THE WORLD
// ----

namespace UtilityVerse.UnitTest;

public class DateTimeExtensionTest
{
    [Fact]
    public void IsInBetween_ReturnsTrue_WhenDateTimeIsCorrect()
    {
        DateTime examDate = DateTime.Now;
        DateTime startDate = new DateTime(2020, 01, 01);
        DateTime endDate = new DateTime(2030, 01, 01);
        Assert.True(examDate.IsInBetween(startDate, endDate).Result);
    }

    [Fact]
    public void IsInBetween_ReturnsFalse_WhenDataIsInvalid()
    {
        DateTime examDate = DateTime.Now;
        DateTime startDate = new DateTime(2020, 01, 01);
        DateTime endDate = new DateTime(2022, 01, 01);
        Assert.False(examDate.IsInBetween(startDate, endDate).Result);
    }


    [Fact]
    public void ToUnixTimeStamp_ReturnValidIntTimeStamp_WhenValidDateIsProvided()
    {
        DateTime dt = new DateTime(1994, 08, 05, 00, 00, 01, DateTimeKind.Unspecified);
        Assert.Equal(776023201, dt.ToUnixTimeStamp(Contracts.TimeEnum.Second).Result);
    }

    [Fact]
    public void ToDateTime_ReturnValidDateTime_WhenValidUnixEpochTimeIsProvided()
    {
        var newYear = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var unixEpochTime = newYear.ToUnixTimeStamp(Contracts.TimeEnum.Second).Result;
        Assert.Equal(unixEpochTime.ToDateTime(Contracts.TimeEnum.Second).Result, newYear);
    }

    [Fact]
    public void ToDateTime_ReturnValidDateTime_WhenValidUnixEpochMillisecondTimeIsProvided()
    {
        var newYear = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var unixEpochTime = newYear.ToUnixTimeStamp(Contracts.TimeEnum.MilliSecond).Result;
        Assert.Equal(unixEpochTime.ToDateTime(Contracts.TimeEnum.MilliSecond).Result, newYear);
    }

    [Fact]
    public void ToDateTime_ReturnDoesNotMatch_WhenMillisecondUnixTimeIsGiven_ButSecondUnixTimeIsExpected()
    {
        var newYear = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var unixEpochTime = newYear.ToUnixTimeStamp(Contracts.TimeEnum.Second).Result;
        Assert.NotEqual(unixEpochTime.ToDateTime(Contracts.TimeEnum.MilliSecond).Result, newYear);
    }

    [Theory]
    [InlineData("2023-01-01", 20230101)]
    [InlineData("2023-02-02", 20230202)]
    public void ToIntDate_ReturnValidInt_WhenValidDateTimeIsProvided(string dt, int dtInt)
    {
        var dateTime = DateTime.Parse(dt);
        Assert.Equal(dateTime.ToIntDate().Result, dtInt);
    }

    [Theory]
    [InlineData("2023-01-01", "yyMMdd", 230101)]
    public void ToIntDate_ReturnValidInt_WhenCustomFormatIsGiven(string dt, string format, int dtInt)
    {
        var datetime = DateTime.Parse(dt);
        var result = datetime.ToIntDate(format);
        Assert.Equal(result.Result, dtInt);
    }
}