using NUnit.Framework;

namespace JMW.Templates.Tests
{
    [TestFixture]
    public class TagTest
    {
        [Test]
        public void TestlTag()
        {
            var tag = new Tag();
            tag.Name = "foo";
            tag.TagType = TagTypes.Tag;

            Assert.That(tag.ToString(), Is.EqualTo("foo:Tag"));
        }

        [Test]
        public void TestParseException()
        {
            var tag = new ParseException("error");

            Assert.That(tag.ErrorText, Is.EqualTo("error"));
            Assert.That(tag.Location, Is.EqualTo(""));
        }
    }
}