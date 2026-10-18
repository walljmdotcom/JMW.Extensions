using JMW.Parsing.Extractors;
using NUnit.Framework;
using System;

namespace JMW.Parsing.Tests;

/// <summary>
/// Without column positions, a col extractor returns the field at its index among the space-separated fields.
/// </summary>
[TestFixture]
public class ColumnSplitTest
{
    [TestCase("a b c", 0, "a")]
    [TestCase("a b c", 2, "c")]
    [TestCase("  a   b  ", 0, "a")]
    [TestCase("  a   b  ", 1, "b")]
    [TestCase("Gi1/0/1  connected  1000", 1, "connected")]
    [TestCase("a\tb c", 0, "a\tb")]
    [TestCase("a\tb c", 1, "c")]
    [TestCase("x", 0, "x")]
    public void ReturnsFieldAtIndex(string line, int index, string expected)
    {
        Assert.That(new Column(index).Parse(line), Is.EqualTo(expected));
    }

    [TestCase("a b", 2)]
    [TestCase("  a   b  ", 2)]
    [TestCase("", 0)]
    [TestCase("   ", 0)]
    public void ThrowsPastLastField(string line, int index)
    {
        Assert.That(() => new Column(index).Parse(line), Throws.TypeOf<IndexOutOfRangeException>());
    }
}
