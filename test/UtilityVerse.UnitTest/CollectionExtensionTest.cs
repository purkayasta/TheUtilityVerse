using UtilityVerse.Extensions;

namespace UtilityVerse.UnitTest
{
    public class CollectionExtensionTest
    {
        [Fact]
        public void IsNullOrEmpty_ReturnsTrue_WhenEmptyEnumerableProvided()
        {
            var result = Enumerable.Empty<string>();
            Assert.True(result.IsNullOrEmpty());
        }

        [Fact]
        public void IsNullOrEmpty_ReturnsTrue_WhenEmptyListIsProvided()
        {
            var result = new List<string>();
            Assert.True(result.IsNullOrEmpty());
        }

        [Fact]
        public void IsNullOrEmpty_ReturnsTrue_WhenNullEnumerableProvided()
        {
            IEnumerable<string>? result = default;
            Assert.True(result.IsNullOrEmpty());
        }

        [Fact]
        public void IsNullOrEmpty_ReturnsFalse_WhenValidListIsProvided()
        {
            var result = new List<int> { 1, 2 };
            Assert.False(result.IsNullOrEmpty());
        }

        [Fact]
        public void IsNullOrEmpty_ReturnsFalse_WhenValidOrderedListProvided()
        {
            var result = new List<int> { 1, 2, 5, 3, 23 };
            Assert.False(result.OrderBy(x => x).IsNullOrEmpty());
        }
    }
}
