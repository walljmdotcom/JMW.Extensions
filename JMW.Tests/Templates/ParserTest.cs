using NUnit.Framework;

namespace JMW.Templates.Tests
{
    [TestFixture]
    public class ParserTest
    {
        [Test]
        public void TestParsingSingleTopLevelTag()
        {
            var parser = new Parser();
            var block = @"<dummy>foo</dummy>";
            var tags = parser.Parse(block);
            Assert.That(tags[0].Name, Is.EqualTo("dummy"));
            Assert.That(tags.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestParsingTagWithParams()
        {
            var parser = new Parser();
            var block = @"<dummy param1 param2=naked param3='quoted'>foo</dummy>";
            var tags = parser.Parse(block);
            Assert.That(tags[0].Name, Is.EqualTo("dummy"));
            Assert.That(tags.Count, Is.EqualTo(1));
            Assert.That(tags[0].Properties["param1"], Is.EqualTo(""));
            Assert.That(tags[0].Properties["param2"], Is.EqualTo("naked"));
            Assert.That(tags[0].Properties["param3"], Is.EqualTo("quoted"));
        }

        [Test]
        public void TestParsingSingleTag()
        {
            var parser = new Parser();
            var block = @"<single />";
            var tags = parser.Parse(block);
            Assert.That(tags[0].Name, Is.EqualTo("single"));
            Assert.That(tags.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestParsingSingleTagWithParams()
        {
            var parser = new Parser();
            var block = @"<single  param1 param2=naked param3=""quoted"" />";
            var tags = parser.Parse(block);
            Assert.That(tags[0].Name, Is.EqualTo("single"));
            Assert.That(tags.Count, Is.EqualTo(1));
            Assert.That(tags[0].Properties["param1"], Is.EqualTo(""));
            Assert.That(tags[0].Properties["param2"], Is.EqualTo("naked"));
            Assert.That(tags[0].Properties["param3"], Is.EqualTo("quoted"));
        }

        [Test]
        public void TestParsingMultipleTopLevelTags()
        {
            var parser = new Parser();
            var block = @"<dummy>foo</dummy><dummy></dummy>";
            var tags = parser.Parse(block);
            Assert.That(tags.Count, Is.EqualTo(2));
            Assert.That(tags[0].Name, Is.EqualTo("dummy"));
            Assert.That(tags[1].Name, Is.EqualTo("dummy"));
        }

        [Test]
        public void TestParsingTopLevelTextAndTag()
        {
            var parser = new Parser();
            var block = @"foo<dummy></dummy>";
            var tags = parser.Parse(block);
            Assert.That(tags.Count, Is.EqualTo(2));
            Assert.That(tags[0].TokenText, Is.EqualTo("foo"));
            Assert.That(tags[0].TagType, Is.EqualTo(TagTypes.Text));
            Assert.That(tags[1].Name, Is.EqualTo("dummy"));
            Assert.That(tags[1].TagType, Is.EqualTo(TagTypes.Tag));
        }

        [Test]
        public void TestParseExceptionNoEndTag()
        {
            //Act
            static object test_delegate() => new Parser().Parse("<foo>some text");

            //Assert
            Assert.That(test_delegate, Throws.TypeOf<ParseException>(), "Template ended prematurely. Did you forget a </foo>?");
        }

        [Test]
        public void TestParseExceptionInvalidEndTag()
        {
            //Act
            static object test_delegate() => new Parser().Parse("<foo>some text</bar>");

            //Assert
            Assert.That(test_delegate, Throws.TypeOf<ParseException>(), "Invalid end tag. Line: 1 Column: 15");
        }

        [Test]
        public void TestParseExceptionInvalidTag()
        {
            //Act
            static object test_delegate() => new Parser().Parse("<foo some text</bar>");

            //Assert
            Assert.That(test_delegate, Throws.TypeOf<ParseException>(), "Invalid tag. Did you forget to add a '>'? Line: 1 Column: 15");
        }

        [Test]
        public void TestParseExceptionUnexpectedEqual()
        {
            //Act
            static object test_delegate() => new Parser().Parse("<foo =stuff >some text</foo>");

            //Assert
            Assert.That(test_delegate, Throws.TypeOf<ParseException>(), "Unexpected '='. Did you forget a param name? Line: 1 Column: 6");
        }

        [Test]
        public void ParsingTest()
        {
            var target = new Parser();
            var block = @"
this is a string
<Single />
<Tag location=""c:\path\to\file.txt"">
some text
<Single />
 <File variable=""/path/tofile""> whee </File>
</Tag>

<Jason> wall </Jason>

more text at the end.

";
            var actual = target.Parse(block);

            Assert.That(actual.Count, Is.EqualTo(5));
            Assert.That(actual[0].TagType == TagTypes.Text, Is.True);
            Assert.That(actual[0].TokenText == @"this is a string", Is.True);
            Assert.That(actual[1].TagType == TagTypes.Tag, Is.True);
            Assert.That(actual[1].Name == "single", Is.True);

            Assert.That(actual[2].TagType == TagTypes.Tag, Is.True);
            Assert.That(actual[2].Name == "tag", Is.True);

            Assert.That(actual[2].TokenText, Is.EqualTo(""));
            Assert.That(actual[2].HasChildren, Is.True);
            Assert.That(actual[2].Children.Count, Is.EqualTo(4));
            Assert.That(actual[2].Children[0].TagType, Is.EqualTo(TagTypes.Text));
            Assert.That(actual[2].Children[0].TokenText, Is.EqualTo("some text"));
            Assert.That(actual[2].Children[1].TagType, Is.EqualTo(TagTypes.Tag));
            Assert.That(actual[2].Children[1].Name, Is.EqualTo("single"));
            Assert.That(actual[2].Children[2].TagType, Is.EqualTo(TagTypes.Text));
            Assert.That(actual[2].Children[2].TokenText, Is.EqualTo(" "));
            Assert.That(actual[2].Children[3].TagType, Is.EqualTo(TagTypes.Tag));
            Assert.That(actual[2].Children[3].Name, Is.EqualTo("file"));
            Assert.That(actual[2].Children[3].Properties["variable"], Is.EqualTo("/path/tofile"));
            var child = actual[2].Children[3];
            Assert.That(child.HasChildren, Is.True);
            Assert.That(child.Children.Count, Is.EqualTo(1));
            Assert.That(child.Children[0].TagType, Is.EqualTo(TagTypes.Text));
            Assert.That(child.Children[0].TokenText, Is.EqualTo(" whee "));
            Assert.That(actual[2].Properties.Count == 1, Is.True);
            Assert.That(actual[2].Properties["location"] == @"c:\path\to\file.txt", Is.True);
            Assert.That(actual[3].TagType == TagTypes.Tag, Is.True);
            Assert.That(actual[3].Name == "jason", Is.True);
            Assert.That(actual[3].HasChildren, Is.True);
            Assert.That(actual[3].Children.Count, Is.EqualTo(1));
            Assert.That(actual[3].Children[0].TokenText, Is.EqualTo(" wall "));
            Assert.That(actual[4].TagType == TagTypes.Text, Is.True);
            Assert.That(actual[4].TokenText == @"more text at the end.", Is.True);
        }

        [Test]
        public void TestParsingMalformedConditional()
        {
            var parser = new Parser();
            var block = @"<If column=TranslatedVlanName type=Contains><Tag name=Sheet1></Tag></If>";

            //Act
            object testDelegate() => parser.Parse(block);

            //Assert
            Assert.That(testDelegate, Throws.TypeOf<ParseException>(), "Malformed conditional tag. Help: <If column=\"\" value=\"\" type=\"\">");
        }

        [TestCase("# comment", "")]
        [TestCase("# comment\n", "")]
        [TestCase("# comment\r\n", "")]
        [TestCase("#", "")]
        [TestCase("a\n# comment", "a")]
        [TestCase("a\r\n# comment", "a")]
        [TestCase("a\r# comment", "a")]
        [TestCase("a\n# comment\n", "a")]
        [TestCase("# comment\nb", "b")]
        [TestCase("# comment\r\nb", "b")]
        [TestCase("# comment\rb", "b")]
        [TestCase("# comment\\nb", "b")]
        [TestCase("<b>x</b># comment", "")]
        [TestCase("a \\# not a comment", "a # not a comment")]
        [TestCase("", "")]
        [TestCase("   ", "   ")]
        public void TestParsingTrailingComment(string block, string expectedText)
        {
            var tags = new Parser().Parse(block);

            var text = string.Concat(tags.FindAll(t => t.TagType == TagTypes.Text).ConvertAll(t => t.TokenText));
            Assert.That(text, Is.EqualTo(expectedText));
        }

        [TestCase(">", ">")]
        [TestCase(">>", ">>")]
        [TestCase("> quoted", "> quoted")]
        [TestCase("a>b", "a>b")]
        [TestCase("<b/>> after", "> after")]
        [TestCase("<b>x</b>>", ">")]
        public void TestParsingTextWithCloseSigil(string block, string expectedText)
        {
            var tags = new Parser().Parse(block);

            var text = string.Concat(tags.FindAll(t => t.TagType == TagTypes.Text).ConvertAll(t => t.TokenText));
            Assert.That(text, Is.EqualTo(expectedText));
        }

        [Test]
        public void TestParsingCloseSigilInsideTagChildren()
        {
            var tags = new Parser().Parse("<b>>x</b>");

            Assert.That(tags, Has.Count.EqualTo(1));
            Assert.That(tags[0].Children, Has.Count.EqualTo(1));
            Assert.That(tags[0].Children[0].TokenText, Is.EqualTo(">x"));
        }

        [TestCase("</b>")]
        [TestCase("a</b>")]
        [TestCase("/>")]
        [TestCase("# comment\n/>")]
        [TestCase("<b></b>/>")]
        [TestCase("<b></b></b>")]
        [TestCase("<b/>/>")]
        [TestCase("<b/></b>")]
        [TestCase("</b x=1>")]
        [TestCase("<b></b x=1>")]
        [TestCase("<b>")]
        [TestCase("<b></c>")]
        public void TestParsingUnbalancedTagsThrowsParseException(string block)
        {
            Assert.That(() => new Parser().Parse(block), Throws.TypeOf<ParseException>());
        }

        [TestCase("<b>")]
        [TestCase("<b><c>")]
        [TestCase("<b></c>")]
        [TestCase("</b>")]
        public void TestParsingAfterFailureStartsClean(string failing)
        {
            var parser = new Parser();
            Assert.That(() => parser.Parse(failing), Throws.TypeOf<ParseException>());

            var tags = parser.Parse("<c>x</c>");

            Assert.That(tags, Has.Count.EqualTo(1));
            Assert.That(tags[0].Name, Is.EqualTo("c"));
        }
    }
}