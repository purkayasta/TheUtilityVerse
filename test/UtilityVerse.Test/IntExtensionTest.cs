using UtilityVerse.Extensions;

namespace UtilityVerse.UnitTesting
{
    public class IntExtensionTest
    {
        [Fact]
        public void IsInBetween_ThrowException_IfSourceIsNull()
        {
            int? source = default;
            Assert.Throws<ArgumentNullException>(() => source.IsInBetween(default, default));
        }

        [Fact]
        public void IsInBetween_ThrowException_IfMinIsNull()
        {
            int? source = 0;
            Assert.Throws<ArgumentNullException>(() => source.IsInBetween(default, default));
        }

        [Fact]
        public void IsInBetween_ThrowException_IfMaxIsNull()
        {
            int? source = 0;
            Assert.Throws<ArgumentNullException>(() => source.IsInBetween(1, default));
        }

        [Fact]
        public void IsInBetween_ThrowException_WhenMinIsGreaterThanMax()
        {
            int? source = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() => source.IsInBetween(1, 0));
        }

        [Fact]
        public void IsInBetween_ReturnFalse_WhenWrongDataIsProvided()
        {
            int? source = 10;
            Assert.False(source.IsInBetween(1, 9));
        }

        [Fact]
        public void IsInBetween_ReturnTrue_WhenRightDataIsProvided()
        {
            int? source = 100;
            Assert.True(source.IsInBetween(10, 200));
        }
    }
}
