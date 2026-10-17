using JMW.Parsing.Expressions;
using NUnit.Framework;
using System;
using System.Linq;

namespace JMW.Parsing.Tests;

/// <summary>
/// A regex expression keeps its parsed patterns between tests, so changes to the query or mods must still apply.
/// </summary>
[TestFixture]
public class RegularExpressionTest
{
    [TestCase("foo", "", "a foo", true)]
    [TestCase("foo", "", "a FOO", false)]
    [TestCase("foo", "i", "a FOO", true)]
    [TestCase("foo", "n", "a foo", false)]
    [TestCase("foo", "in", "a FOO", false)]
    [TestCase("^foo", "", "a foo", false)]
    public void Tests(string pattern, string mods, string input, bool expected)
    {
        var exp = new RegularExpression([pattern], mods);

        Assert.That(exp.Test(input), Is.EqualTo(expected));
        Assert.That(exp.Test(input), Is.EqualTo(expected));
    }

    [Test]
    public void SeesPatternReplacedBetweenTests()
    {
        var exp = new RegularExpression(["foo"], string.Empty);
        Assert.That(exp.Test("bar"), Is.False);

        exp.Search.Query[0] = "bar";
        Assert.That(exp.Test("bar"), Is.True);

        exp.Search.Query.Add("baz");
        Assert.That(exp.Test("baz"), Is.True);

        exp.Search = new Search { Query = ["qux"] };
        Assert.That(exp.Test("baz"), Is.False);
    }

    [Test]
    public void SeesModsChangedBetweenTests()
    {
        var exp = new RegularExpression(["foo"], string.Empty);
        Assert.That(exp.Test("FOO"), Is.False);

        exp.Search.Mods = "i";
        Assert.That(exp.Test("FOO"), Is.True);

        exp.Search.Mods = string.Empty;
        Assert.That(exp.Test("FOO"), Is.False);
    }

    // More expressions than Regex's static cache holds, tested in turn.
    [Test]
    public void TestsManyExpressionsInTurn()
    {
        var expressions = Enumerable.Range(0, 20).Select(static i => new RegularExpression(["^line" + i + "$"], string.Empty)).ToArray();

        for (var pass = 0; pass < 2; pass++)
        {
            for (var i = 0; i < expressions.Length; i++)
            {
                Assert.That(expressions[i].Test("line" + i), Is.True);
                Assert.That(expressions[(i + 1) % expressions.Length].Test("line" + i), Is.False);
            }
        }
    }

    [Test]
    public void BadPatternAfterAMatchIsNotParsed()
    {
        var exp = new RegularExpression(["foo", "("], string.Empty);

        Assert.That(exp.Test("foo"), Is.True);
        Assert.That(() => exp.Test("bar"), Throws.InstanceOf<ArgumentException>());
    }
}
