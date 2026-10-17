using System.IO;
using NUnit.Framework;

namespace JMW.Templates.Tests
{
    [TestFixture]
    public class TokenizerTest
    {
        [Test]
        public void TestMaybeReadClose()
        {
            var reader = new StringReader("foo>");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.consumeTagName(TokenType.OPENTAG), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Type, Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer._inTag, Is.True);
        }

        [Test]
        public void TestMaybeReadText()
        {
            var reader = new StringReader("o");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.maybeReadText(">"), Is.False);
            Assert.That((char)tokenizer._reader.Read(), Is.EqualTo('o'));
            Assert.That(tokenizer._reader.Read(), Is.EqualTo(-1));
        }

        [Test]
        public void TestMaybeReadSingleClose()
        {
            var reader = new StringReader(" />");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.maybeReadSingleClose(), Is.False);
            Assert.That((char)tokenizer._reader.Read(), Is.EqualTo(' '));
            Assert.That(tokenizer.maybeReadSingleClose(), Is.True);
            Assert.That(tokenizer._reader.Read(), Is.EqualTo(-1));
        }

        [Test]
        public void TestConsumeTagName()
        {
            var reader = new StringReader("foo >");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.consumeTagName(TokenType.OPENTAG), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Type, Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer._inTag, Is.True);
            Assert.That(tokenizer.maybeReadClose(), Is.True);
        }

        [Test]
        public void TestConsumeWhitespace1()
        {
            var tokenizer = new Tokenizer("  f");
            tokenizer.consumeWhitespace();
            Assert.That((char)tokenizer._reader.Read(), Is.EqualTo('f'));
        }

        [Test]
        public void TestConsumeWhitespace2()
        {
            var tokenizer = new Tokenizer("  f  ");
            tokenizer.consumeWhitespace();
            Assert.That((char)tokenizer._reader.Read(), Is.EqualTo('f'));
            tokenizer.consumeWhitespace();
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestOpenTag()
        {
            var reader = new StringReader("<foo>");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer._eof, Is.False);

            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestFullTag1()
        {
            var tokenizer = new Tokenizer("<foo param1 param2=naked param3=\"quoted\" >");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param1"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param2"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMVALUE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("naked"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param3"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMVALUE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("quoted"));
        }

        [Test]
        public void TestFullTagError1()
        {
            var tokenizer = new Tokenizer("<foo param1 param2=naked param3=");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param1"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param2"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMVALUE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("naked"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param3"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("Invalid tag. Did you forget to add a >?"));
        }

        [Test]
        public void TestFullTagError3()
        {
            var tokenizer = new Tokenizer("<foo param3=\"quoted");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param3"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("Unexpected end of template.  Did you forget to add a \"?"));
        }

        [Test]
        public void TestFullTagError2()
        {
            var tokenizer = new Tokenizer("<foo param1=naked");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.PARAMNAME));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("param1"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("Invalid tag. Did you forget to add a >?"));
        }

        [Test]
        public void TestEndTag()
        {
            var tokenizer = new Tokenizer("</foo>");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
        }

        [Test]
        public void TestSingleCloseTag1()
        {
            var reader = new StringReader("<foo />");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer._inTag, Is.True);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.SINGLEClOSE));
            Assert.That(tokenizer._inTag, Is.False);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestSingleCloseTag2()
        {
            var reader = new StringReader("/ ");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("/ "));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestSingleCloseTagError1()
        {
            var reader = new StringReader("<foo / >");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer._inTag, Is.True);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("Single tag closing forward slash detected without closing angle bracket."));
        }

        [Test]
        public void TestSingleCloseTagError2()
        {
            var reader = new StringReader("<foo /");
            var tokenizer = new Tokenizer(reader);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer._inTag, Is.True);
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("Single tag closing forward slash detected without closing angle bracket."));
        }

        [Test]
        public void TestText()
        {
            var tokenizer = new Tokenizer("foo bar<");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo bar"));
            Assert.That((char)tokenizer._reader.Read(), Is.EqualTo('<'));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestTagStream()
        {
            var tokenizer = new Tokenizer("<foo>bar</foo><singleclose />");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("singleclose"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.SINGLEClOSE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(""));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestTagStream2()
        {
            var tokenizer = new Tokenizer("<foo>bar</foo><singleclose /> #this is a comment\n<bar>foo</bar>");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("singleclose"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.SINGLEClOSE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(""));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(" "));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.COMMENT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("this is a comment"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestTagStream3()
        {
            var tokenizer = new Tokenizer("<foo>bar</foo><singleclose /> \\#this is not a comment\n<bar>foo</bar>");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("singleclose"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.SINGLEClOSE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(""));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(" #this is not a comment\n"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestTagStream4()
        {
            var tokenizer = new Tokenizer("<foo>bar</foo><singleclose />\\#this is not a comment\n<bar>foo</bar>");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("singleclose"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.SINGLEClOSE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(""));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("#this is not a comment\n"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestTagStream5()
        {
            var tokenizer = new Tokenizer("<foo>bar</foo><singleclose />#this is a comment\n<bar>foo</bar>");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("singleclose"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.SINGLEClOSE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(""));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.COMMENT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("this is a comment"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestTagStream6()
        {
            var tokenizer = new Tokenizer("<foo>bar</foo><singleclose />#");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.TEXT));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("bar"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ENDTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("foo"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.OPENTAG));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("singleclose"));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.SINGLEClOSE));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(""));
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Value, Is.EqualTo("EOF"));
        }

        [Test]
        public void TestInvalidUnicodeCharError()
        {
            var tokenizer = new Tokenizer("bar\uffff</foo><singleclose />");
            Assert.That(tokenizer.Next(), Is.EqualTo(TokenType.ERROR));
            Assert.That(tokenizer.Token.Line, Is.EqualTo(0));
            Assert.That(tokenizer.Token.Column, Is.EqualTo(4));
            Assert.That(tokenizer.Token.Value, Is.EqualTo(Tokenizer.INVALID_UNICODE_ERROR));
        }
    }
}