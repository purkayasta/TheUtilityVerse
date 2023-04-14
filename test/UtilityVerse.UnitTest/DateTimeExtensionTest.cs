﻿using UtilityVerse.Extensions;

namespace UtilityVerse.UnitTest
{
    public class DateTimeExtensionTest
    {
        [Fact]
        public void IsInBetween_ThrowsException_WhenSourceDateIsNull()
        {
            DateTime? dateTime = null;
            Assert.Throws<ArgumentNullException>(() => dateTime.IsInBetween(null, null));
        }

        [Fact]
        public void IsInBetween_ThrowsException_WhenFirstDateIsNull()
        {
            DateTime? dateTime = new DateTime();
            Assert.Throws<ArgumentNullException>(() => dateTime.IsInBetween(null, null));
        }

        [Fact]
        public void IsInBetween_ThrowsException_WhenSecondDateIsNull()
        {
            DateTime? dateTime = new DateTime();
            Assert.Throws<ArgumentNullException>(() => dateTime.IsInBetween(new DateTime(), null));
        }

        [Fact]
        public void IsInBetween_ThrowsException_WhenDateTimeIsNotRight()
        {
            DateTime? dateTime = DateTime.Now;
            Assert.Throws<ArgumentOutOfRangeException>(() => dateTime.IsInBetween(DateTime.Now, DateTime.UtcNow));
        }

        [Fact]
        public void IsInBetween_ReturnsTrue_WhenDateTimeIsCorrect()
        {
            DateTime? examDate = DateTime.Now;
            DateTime? startDate = new DateTime(2020, 01, 01);
            DateTime? endDate = new DateTime(2024, 01, 01);
            Assert.True(examDate.IsInBetween(startDate, endDate));
        }

        [Fact]
        public void IsInBetween_ReturnsFalse_WhenDataIsInvalid()
        {
            DateTime? examDate = DateTime.Now;
            DateTime? startDate = new DateTime(2020, 01, 01);
            DateTime? endDate = new DateTime(2022, 01, 01);
            Assert.False(examDate.IsInBetween(startDate, endDate));
        }

        [Fact]
        public void ToUnixTimeStamp_ThrowsException_WhenNullIsGiven()
        {
            DateTime? dt = default;
            Assert.Throws<ArgumentNullException>(() => dt.ToUnixTimeStamp(Contracts.UtilityVerseTimeEnum.Second));
        }

        [Fact]
        public void ToUnixTimeStamp_ReturnValidIntTimeStamp_WhenValidDateIsProvided()
        {
            DateTime? dt = new DateTime(1994, 08, 05, 00, 00, 01, DateTimeKind.Unspecified);
            Assert.Equal(776023201, dt.ToUnixTimeStamp(Contracts.UtilityVerseTimeEnum.Second));
        }

        [Fact]
        public void ToDateTime_ThrowsException_WhenInvalidLongNumberProvided()
        {
            long? longNumber = default;
            Assert.Throws<ArgumentNullException>(() => longNumber.ToDateTime(Contracts.UtilityVerseTimeEnum.MilliSecond));
        }

        [Fact]
        public void ToDateTime_ThrowsException_WhenNumberIsBelowOne()
        {
            long? longNumber = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => longNumber.ToDateTime(Contracts.UtilityVerseTimeEnum.Second));
        }

        [Fact]
        public void ToDateTime_ReturnValidDateTime_WhenValidUnixEpochTimeIsProvided()
        {
            var newYear = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            var unixEpochTime = newYear.ToUnixTimeStamp(Contracts.UtilityVerseTimeEnum.Second);
            Assert.Equal(unixEpochTime.ToDateTime(Contracts.UtilityVerseTimeEnum.Second), newYear);
        }

        [Fact]
        public void ToDateTime_ReturnValidDateTime_WhenValidUnixEpochMillisecondTimeIsProvided()
        {
            var newYear = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            var unixEpochTime = newYear.ToUnixTimeStamp(Contracts.UtilityVerseTimeEnum.MilliSecond);
            Assert.Equal(unixEpochTime.ToDateTime(Contracts.UtilityVerseTimeEnum.MilliSecond), newYear);
        }

        [Fact]
        public void ToDateTime_ReturnDoesNotMatch_WhenMillisecondUnixTimeIsGiven_ButSecondUnixTimeIsExpected()
        {
            var newYear = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            var unixEpochTime = newYear.ToUnixTimeStamp(Contracts.UtilityVerseTimeEnum.Second);
            Assert.NotEqual(unixEpochTime.ToDateTime(Contracts.UtilityVerseTimeEnum.MilliSecond), newYear);
        }
    }
}
