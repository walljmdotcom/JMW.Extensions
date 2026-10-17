using JMW.Templates.Tags;
using NUnit.Framework;
using System;

namespace JMW.Templates.Tests
{
    [TestFixture]
    public class TagHelpersTest
    {
        [Test]
        public void EvaluateBooleanExpressionErrorTest()
        {
            Assert.That(
                () => TagHelpers.EvaluateBooleanExpression("x1.substr(0,2)", ["jason"]),
                Throws.Exception.With.Message.EqualTo("The expression:\nx1.substr(0,2)\ndid not evaluate to a boolean value."));
        }

        [Test]
        public void EvaluateArithmeticExpressionErrorTest()
        {
            Assert.That(
                () => TagHelpers.EvaluateArithmeticExpression("x1.substr(0,2)", ["jason"]),
                Throws.Exception.With.Message.EqualTo("The expression:\nx1.substr(0,2)\ndid not evaluate to a numeric value."));
        }

        [Test]
        public void RetrieveOctetErrorTest()
        {
            const string invalidOctet = "The value provided for octet argument was invalid. Must be a number between 1 and 4.";
            Assert.That(() => TagHelpers.RetrieveOctet("1.1.1.1", "0"), Throws.Exception.With.Message.EqualTo(invalidOctet));
            Assert.That(() => TagHelpers.RetrieveOctet("1.1.1.1", "5"), Throws.Exception.With.Message.EqualTo(invalidOctet));
            Assert.That(() => TagHelpers.RetrieveOctet("1.1.1.1", "x"), Throws.Exception.With.Message.EqualTo(invalidOctet));

            var data = "1.1.1";
            Assert.That(
                () => TagHelpers.RetrieveOctet(data, "1"),
                Throws.Exception.With.Message.EqualTo("There was a problem parsing octets from the IP: '" + data + "'. Please check formatting."));

            data = "1.1.1.1.1";
            Assert.That(
                () => TagHelpers.RetrieveOctet(data, "1"),
                Throws.Exception.With.Message.EqualTo("There was a problem parsing octets from the IP: '" + data + "'. Please check formatting."));

            data = "11111";
            Assert.That(
                () => TagHelpers.RetrieveOctet(data, "1"),
                Throws.Exception.With.Message.EqualTo("The provided column value: '" + data + "' is not an IP."));
        }

        [Test]
        public void ReplaceOctetTest()
        {
            Assert.That("1.1.1.255", Is.EqualTo(TagHelpers.ReplaceOctet("1.1.1.1", "4", 2600)));
            Assert.That("1.1.1.0", Is.EqualTo(TagHelpers.ReplaceOctet("1.1.1.1", "4", -1)));
        }

        [Test]
        public void SetupEngineErrorTest()
        {
            Assert.That(
                () => TagHelpers.EvaluateBooleanExpression("x1.substr(0,2); x2", ["jason"]),
                Throws.Exception.With.Message.EqualTo(
                    "The list of arguments provided in:\njason\n is greater that the number of variables listed in the expression: x1.substr(0,2); x2\n" +
                    "Variables must be specified in the format: x1, x2, x3 etc. and must be equal or less than the argumenst provided."));
        }

        [Test]
        public void CheckAllowedAttributesErrorTest()
        {
            var t = new Tag
            {
                Name = "foo",
                TagType = TagTypes.Tag
            };
            t.Properties.Add("foo", "bar");

            Assert.That(
                () => TagHelpers.CheckAllowedAttributes(t, ["blah"], new Token()),
                Throws.Exception.With.Message.EqualTo("Invalid attribute found on tag. Allowed attributes: blah Line: 1 Column: 1"));
        }
    }
}