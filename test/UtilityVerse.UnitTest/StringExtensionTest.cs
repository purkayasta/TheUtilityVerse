using UtilityVerse.Extensions;

namespace UtilityVerse.UnitTest
{
    public class StringExtensionTest
    {
        [Fact]
        public void IsNullEmptyOrWhiteSpace_ReturnTrue_WhenValueIsNull()
        {
            string? value = default;
            Assert.True(value.IsNullOrEmptyOrWhiteSpace());
        }

        [Fact]
        public void IsNullEmptyOrWhiteSpace_ReturnTrue_WhenValueIsEmpty()
        {
            string? value = string.Empty;
            Assert.True(value.IsNullOrEmptyOrWhiteSpace());
        }

        [Fact]
        public void IsNullEmptyOrWhiteSpace_ReturnTrue_WhenValueIsWhiteSpace()
        {
            string? value = " ";
            Assert.True(value.IsNullOrEmptyOrWhiteSpace());
        }

        [Fact]
        public void AsString_ReturnEmpty_WhenArrayIsEmpty()
        {
            string[] arr = Array.Empty<string>();
            Assert.Equal(string.Empty, arr.AsString());
        }

        [Fact]
        public void AsString_ReturnValid_WhenArrayIsValid()
        {
            string[] arr = new[] { "Pritom", "Purkayasta" };
            Assert.Equal("PritomPurkayasta", arr.AsString());
        }

        [Fact]
        public void AsString_ReturnValid_WhenArrayWithSeperatorIsValid()
        {
            string[] arr = new[] { "Pritom", "Purkayasta" };
            Assert.Equal("Pritom Purkayasta", arr.AsString(" "));
        }
    }
}
