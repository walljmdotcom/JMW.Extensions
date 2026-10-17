using System.Linq;
using NUnit.Framework;

namespace JMW.Extensions.String.Tests
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [Test]
        public void SafeSubstringTest1()
        {
            var str = "This is a string";
            Assert.That("This is a" == str.SafeSubstring(0, 9));
            Assert.That("This is a string" == str.SafeSubstring(0, 30));
            Assert.That("is a" == str.SafeSubstring(5, 4));
            Assert.That(string.Empty == str.SafeSubstring(30));
            Assert.That(string.Empty == str.SafeSubstring(30, 10));
            Assert.That(string.Empty == str.SafeSubstring(0, -10));
            Assert.That(string.Empty == str.SafeSubstring(0, 0));
        }

        [Test]
        public void ParseToIndexOfTest1()
        {
            var str = "This is a string";
            Assert.That("This is a " == str.ParseToIndexOf("str"));
            Assert.That("This is a " == str.ParseToIndexOf(true, "Str"));
            Assert.That(str == str.ParseToIndexOf("blah"));
            Assert.That(str == str.ParseToIndexOf("Str"));
            Assert.That(string.Empty == string.Empty.ParseToIndexOf("Str"));
            Assert.That("that" == "that".ParseToIndexOf());
            Assert.That("that this is a test",Is.EqualTo( "that this is a testfoo blah foo blah foo ".ParseToIndexOf("blah", "foo")));
            Assert.That("that this is a test", Is.EqualTo("that this is a testfoo blah foo blah foo ".ParseToIndexOf("foo", "blah")));
        }

        [Test]
        public void ParseToLastIndexOfTest1()
        {
            var str = "This is a string a string";
            Assert.That("This is a string a " == str.ParseToLastIndexOf("str"));
            Assert.That("This is a string a " == str.ParseToLastIndexOf(true, "Str"));
            Assert.That(str == str.ParseToLastIndexOf("blah"));
            Assert.That(str == str.ParseToLastIndexOf("Str"));
            Assert.That(string.Empty == string.Empty.ParseToLastIndexOf("Str"));
            Assert.That("that" == "that".ParseToLastIndexOf());
            Assert.That("that this is a test foo ", Is.EqualTo("that this is a test foo blah jason".ParseToLastIndexOf("blah", "foo")));
            Assert.That("that this is a test foo ", Is.EqualTo("that this is a test foo blah jason".ParseToLastIndexOf("foo", "blah")));
        }

        [Test]
        public void ParseToIndexOf_PlusLengthTest1()
        {
            var str = "This is a string";
            Assert.That("This is a str" == str.ParseToIndexOf_PlusLength("str"));
            Assert.That("This is a str" == str.ParseToIndexOf_PlusLength(true, "Str"));
            Assert.That(str == str.ParseToIndexOf_PlusLength("blah"));
            Assert.That(str == str.ParseToIndexOf_PlusLength("Str"));
            Assert.That(string.Empty == string.Empty.ParseToIndexOf_PlusLength("Str"));
            Assert.That("that" == "that".ParseToIndexOf_PlusLength());
            Assert.That("that this is a test foo", Is.EqualTo("that this is a test foo blah jason".ParseToIndexOf_PlusLength("blah", "foo")));
            Assert.That("that this is a test foo", Is.EqualTo("that this is a test foo blah jason".ParseToIndexOf_PlusLength("foo", "blah")));
        }

        [Test]
        public void ParseToDesignatedIndexOf_PlusLengthTest1()
        {
            var str = "This is a string a string a string";
            Assert.That("This is a string a str" == str.ParseToDesignatedIndexOf_PlusLength(2, false, "str"));
            Assert.That("This is a string a str" == str.ParseToDesignatedIndexOf_PlusLength(2, true, "Str"));
            Assert.That(str == str.ParseToDesignatedIndexOf_PlusLength(1, false, "blah"));
            Assert.That(str == str.ParseToDesignatedIndexOf_PlusLength(1, false, "Str"));
            Assert.That(string.Empty == string.Empty.ParseToDesignatedIndexOf_PlusLength(1, false, "Str"));
            Assert.That("that" == "that".ParseToDesignatedIndexOf_PlusLength(1, false));
            Assert.That("that this is a test foo", Is.EqualTo("that this is a test foo blah jason".ParseToDesignatedIndexOf_PlusLength(1, false, "blah", "foo")));
            Assert.That("that this is a test foo", Is.EqualTo("that this is a test foo blah jason".ParseToDesignatedIndexOf_PlusLength(1, false, "foo", "blah")));
        }

        [Test]
        public void ParseToLastIndexOf_PlusLengthTest1()
        {
            var str = "This is a string a string";
            Assert.That("This is a string a str" == str.ParseToLastIndexOf_PlusLength("str"));
            Assert.That("This is a string a str" == str.ParseToLastIndexOf_PlusLength(true, "Str"));
            Assert.That(str == str.ParseToLastIndexOf_PlusLength("blah"));
            Assert.That(str == str.ParseToLastIndexOf_PlusLength("Str"));
            Assert.That(string.Empty == string.Empty.ParseToLastIndexOf_PlusLength("Str"));
            Assert.That("that" == "that".ParseToLastIndexOf_PlusLength());
            Assert.That("that this is a test foo blah", Is.EqualTo("that this is a test foo blah jason".ParseToLastIndexOf_PlusLength("blah", "foo")));
            Assert.That("that this is a test foo blah", Is.EqualTo("that this is a test foo blah jason".ParseToLastIndexOf_PlusLength("foo", "blah")));
        }

        [Test]
        public void ParseAfterIndexOfTest1()
        {
            var str = "This is is a string";
            Assert.That("is is is a string" == str.ParseAfterIndexOf("is"));
            Assert.That("is is is a string" == str.ParseAfterIndexOf(true, "IS"));
            Assert.That(str == str.ParseAfterIndexOf("blah"));
            Assert.That(str == str.ParseAfterIndexOf("Str"));
            Assert.That(string.Empty == string.Empty.ParseAfterIndexOf("Str"));
            Assert.That("that" == "that".ParseAfterIndexOf());
            Assert.That("blah foo that this is a test", Is.EqualTo("jason blah foo that this is a test".ParseAfterIndexOf("blah", "foo")));
            Assert.That("blah foo that this is a test", Is.EqualTo("jason blah foo that this is a test".ParseAfterIndexOf("foo", "blah")));
        }

        [Test]
        public void ParseAfterLastIndexOfTest1()
        {
            var str = "This is is a string";
            Assert.That("is a string" == str.ParseAfterLastIndexOf("is"));
            Assert.That("is a string" == str.ParseAfterLastIndexOf(true, "IS"));
            Assert.That(str == str.ParseAfterLastIndexOf("blah"));
            Assert.That(str == str.ParseAfterLastIndexOf("Str"));
            Assert.That(string.Empty == string.Empty.ParseAfterLastIndexOf("Str"));
            Assert.That("that" == "that".ParseAfterLastIndexOf());
            Assert.That("foo that this is a test", Is.EqualTo("jason blah foo that this is a test".ParseAfterLastIndexOf("foo", "blah")));
            Assert.That("foo that this is a test", Is.EqualTo("jason blah foo that this is a test".ParseAfterLastIndexOf("blah", "foo")));
        }

        [Test]
        public void ParseAfterIndexOf_PlusLengthTest1()
        {
            var str = "This is a string";
            Assert.That(" is a string" == str.ParseAfterIndexOf_PlusLength("This"));
            Assert.That(" is a string" == str.ParseAfterIndexOf_PlusLength(true, "this"));
            Assert.That(str == str.ParseAfterIndexOf_PlusLength("blah"));
            Assert.That(str == str.ParseAfterIndexOf_PlusLength("Str"));
            Assert.That(string.Empty == string.Empty.ParseAfterIndexOf_PlusLength("Str"));
            Assert.That("that" == "that".ParseAfterIndexOf_PlusLength());
            Assert.That(" blah that this is a test", Is.EqualTo("jason foo blah that this is a test".ParseAfterIndexOf_PlusLength("blah", "foo")));
            Assert.That(" blah that this is a test", Is.EqualTo("jason foo blah that this is a test".ParseAfterIndexOf_PlusLength("foo", "blah")));
        }

        [Test]
        public void ParseAfterLastIndexOf_PlusLengthTest1()
        {
            var str = "This is another This is a string a string";
            Assert.That(" is a string a string" == str.ParseAfterLastIndexOf_PlusLength("This"));
            Assert.That(" is a string a string" == str.ParseAfterLastIndexOf_PlusLength(true, "this"));
            Assert.That(str == str.ParseAfterLastIndexOf_PlusLength("blah"));
            Assert.That(str == str.ParseAfterLastIndexOf_PlusLength("Str"));
            Assert.That(string.Empty == string.Empty.ParseAfterLastIndexOf_PlusLength("Str"));
            Assert.That("that" == "that".ParseAfterLastIndexOf_PlusLength());
            Assert.That(" that this is a test", Is.EqualTo("jason foo blah that this is a test".ParseAfterLastIndexOf_PlusLength("blah", "foo")));
            Assert.That(" that this is a test", Is.EqualTo("jason foo blah that this is a test".ParseAfterLastIndexOf_PlusLength("foo", "blah")));
        }

        [Test]
        public void ParseAfterDesignatedIndexOf_PlusLengthTest1()
        {
            var str = "This is a string a string a string";
            Assert.That("ing a string" == str.ParseAfterDesignatedIndexOf_PlusLength(2, false, "str"));
            Assert.That("ing a string" == str.ParseAfterDesignatedIndexOf_PlusLength(2, true, "Str"));
            Assert.That(str == str.ParseAfterDesignatedIndexOf_PlusLength(1, false, "blah"));
            Assert.That(str == str.ParseAfterDesignatedIndexOf_PlusLength(1, false, "Str"));
            Assert.That(string.Empty == string.Empty.ParseAfterDesignatedIndexOf_PlusLength(1, false, "Str"));
            Assert.That("that" == "that".ParseAfterDesignatedIndexOf_PlusLength(1, false));
            Assert.That(" blah jason", Is.EqualTo("that this is a test foo blah jason".ParseAfterDesignatedIndexOf_PlusLength(1, false, "blah", "foo")));
            Assert.That(" jason", Is.EqualTo("that this is a test foo blah jason".ParseAfterDesignatedIndexOf_PlusLength(2, false, "foo", "blah")));
        }


        [Test]
        public void IndexOfTest1()
        {
            //        01234567890123456798012345647980123
            var str = "This is a string a string a string";
            Assert.That(10, Is.EqualTo(str.IndexOf("STR", true, 1)));
            Assert.That(10, Is.EqualTo(str.IndexOf("str", false, 1)));

            Assert.That(19, Is.EqualTo(str.IndexOf("STR", true, 2)));
            Assert.That(19, Is.EqualTo(str.IndexOf("str", false, 2)));
        }


        [Test]
        public void ToLinesTest()
        {
            var str = "This is another\r\nThis is a string\r\n a string";
            Assert.That(3, Is.EqualTo(str.ToLines().Count()));
        }

        [Test]
        public void ContainsFastTest()
        {
            var str = "This is another\r\nThis is a string\r\n a string";
            Assert.That(true, Is.EqualTo(str.ContainsFast('t')));
            Assert.That(false, Is.EqualTo(str.ContainsFast('q')));
        }

        [Test]
        public void ToPascalCaseTest()
        {
            var str = "This is another";
            Assert.That("ThisIsAnother", Is.EqualTo(str.ToPascalCase()));
            Assert.That("This_IsAnother", Is.EqualTo("This _ is another".ToPascalCase()));
        }

        [TestCase("", "")]
        [TestCase(" ", "")]
        [TestCase("   ", "")]
        [TestCase("a", "A")]
        [TestCase("a  b", "AB")]
        [TestCase(" leading", "Leading")]
        [TestCase("trailing ", "Trailing")]
        [TestCase("  both  ends  ", "BothEnds")]
        [TestCase("already Pascal", "AlreadyPascal")]
        [TestCase("mIxed caSe", "MIxedCaSe")]
        [TestCase("in/valid name", "InvalidName")]
        [TestCase("/abc", "Abc")]
        [TestCase("i stay i", "IStayI")]
        public void ToPascalCaseHandlesEmptyWords(string input, string expected)
        {
            Assert.That(input.ToPascalCase(), Is.EqualTo(expected));
        }

        [TestCase("", "", false, 0)]
        [TestCase("abc", "", false, 0)]
        [TestCase("abc", "", true, 0)]
        [TestCase("", "a", false, 0)]
        [TestCase("aaa", "a", false, 3)]
        [TestCase("aaaa", "aa", false, 2)]
        [TestCase("port portindex port", "port", false, 3)]
        [TestCase("port portindex port", "port", true, 2)]
        [TestCase("port", "port", true, 1)]
        [TestCase("Port port", "port", false, 1)]
        [TestCase("abc", "abcd", false, 0)]
        public void CountInstancesTest(string test, string c, bool wordBoundaries, int expected)
        {
            Assert.That(test.CountInstances(c, wordBoundaries), Is.EqualTo(expected));
        }
    }
}