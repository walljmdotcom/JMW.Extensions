using JMW.Parsing.Compile;
using JMW.Parsing.Handlers;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace JMW.Parsing.Tests;

/// <summary>
/// A property with a split reads the input in pieces ending at the split text, and returns the first piece its line
/// expression accepts.
/// </summary>
[TestFixture]
public class PropertySplitTest
{
    [TestCase(";;", "", "a;;b", "a")]
    [TestCase(";;", "b", "a;;b;;c", "b")]
    [TestCase(";;", "c", "a;;b;;c", "c")]
    [TestCase(";;", "c", "a;;b;;c;;", "c")]
    [TestCase(";;", "z", "a;;b", "")]
    [TestCase(";;", "", "", "")]
    [TestCase(";;", "", ";;a", "")]
    [TestCase(";;", "a", ";;a", "a")]
    [TestCase(";;", "b", "a;;;b", ";b")]
    [TestCase(";;", "", "a;", "a;")]
    [TestCase(";", "", "a;b", "a")]
    [TestCase("aa", "", "baaab", "b")]
    [TestCase("aa", "ab", "baaab", "ab")]
    [TestCase("", "", "abc", "a")]
    [TestCase("", "c", "abc", "c")]
    [TestCase("", "z", "abc", "")]
    public void ReturnsFirstAcceptedPiece(string split, string line_contains, string input, string expected)
    {
        var line = line_contains.Length > 0 ? "line: contains {s:[\"" + line_contains + "\"]}" : string.Empty;
        var text = "prop { name:\"p\" " + line + " split:\"" + split +"\" parsers:[ to { s:[\"|\"] } ] }";
        var property = new Property(new Parser().Parse(text).First());

        Assert.That(property.Parse(new StringReader(input)), Is.EqualTo(expected));
    }
}
