using NUnit.Framework;

namespace JMW.Extensions.Tests
{
    [TestFixture]
    public class ObjectExtensionTests
    {
        [Test]
        public void IsEmpty_IsNotEmpty()
        {
            Assert.That("This is a".IsNotEmpty());
            Assert.That(string.Empty.IsEmpty());
            Assert.That(((string?)null).IsEmpty());
        }

        [Test]
        public void IsNotNullTest()
        {
            string? test = null;

            Assert.That("blah".IsNotNull(), Is.True);
            Assert.That(test.IsNotNull(), Is.False);
        }

        [Test]
        public void IsNullTest()
        {
            string? test = null;

            Assert.That("blah".IsNull(), Is.False);
            Assert.That(test.IsNull(), Is.True);
        }
    }
}