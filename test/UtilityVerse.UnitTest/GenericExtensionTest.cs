using UtilityVerse.Extensions;

namespace UtilityVerse.UnitTest
{
    public class GenericExtensionTest
    {
        internal record User(string Name);

        [Fact]
        public void ToByteArray_ReturnsValidByteArray_WhenValidInputIsProvided()
        {
            var user = new User("PRITOM PURKAYASTA");
            var expected = user.ToByteArray();
            Assert.Equal(expected.To<User>(), user);
        }

        [Fact]
        public void To_ThrowException_WhenNullByteArrayIsGiven()
        {
            byte[] array = default;
            Assert.Throws<ArgumentNullException>(() => array.To<User>());
        }

        [Fact]
        public void ToByteArray_ThrowsException_WhenNullClassIsProvided()
        {
            User? user = default;
            Assert.Throws<ArgumentNullException>(() => user.ToByteArray());
        }
    }
}
