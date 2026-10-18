using JMW.Collections;
using JMW.Parsing.Expressions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Contains = JMW.Parsing.Expressions.Contains;

namespace JMW.Parsing.Tests;

[TestFixture]
public class LineExpressionTest
{
    // Values are counted left to right, each match starting after the last.
    [TestCase("a b c", " ", "", 2)]
    [TestCase("aaa", "aa", "", 1)]
    [TestCase("aaaa", "aa", "", 2)]
    [TestCase("abcabc", "abc|b", "", 2)]
    [TestCase("abcabc", "b|abc", "", 2)]
    [TestCase("xAx ax", "a", "", 1)]
    [TestCase("xAx ax", "a", "i", 2)]
    [TestCase("xAx ax", "A", "i", 2)]
    [TestCase("none here", "z", "", 0)]
    [TestCase("", "a", "", 0)]
    [TestCase("a-b-c", "-|b", "", 3)]
    public void CountsMatches(string line, string values, string mods, int expected)
    {
        var search = values.Split('|').ToList();

        Assert.That(new Count(search, Exactly(expected), mods).Test(line), Is.True);
        Assert.That(new Count(search, Exactly(expected + 1), mods).Test(line), Is.False);
    }

    // An empty value matches everywhere without moving, so it is not counted and cannot loop forever.
    [TestCase("abc", "", 0)]
    [TestCase("a a", "|a", 2)]
    public void SkipsEmptyCountValue(string line, string values, int expected)
    {
        var count = new Count(values.Split('|').ToList(), Exactly(expected), string.Empty);

        Assert.That(count.Test(line), Is.True);
    }

    [TestCase("Gi1/0/1 connected", "CONNECTED", "i", true)]
    [TestCase("Gi1/0/1 connected", "CONNECTED", "", false)]
    [TestCase("Gi1/0/1 connected", "zzz|Connected", "i", true)]
    [TestCase("Gi1/0/1 connected", "connected", "n", false)]
    [TestCase("Gi1/0/1 connected", "zzz", "in", true)]
    public void ContainsMatchesAnyValue(string line, string values, string mods, bool expected)
    {
        Assert.That(new Contains(values.Split('|').ToList(), mods).Test(line), Is.EqualTo(expected));
    }

    [TestCase("Gi1/0/1 up", "gi1", "i", true)]
    [TestCase("Gi1/0/1 up", "gi1", "", false)]
    [TestCase("Gi1/0/1 up", "GI1", "i", true)]
    public void StartsWithMatchesAnyValue(string line, string values, string mods, bool expected)
    {
        Assert.That(new StartsWith(values.Split('|').ToList(), mods).Test(line), Is.EqualTo(expected));
    }

    // Expressions reuse their query between tests, so a query changed after the first test must still take effect.
    [Test]
    public void SeesQueryChangedBetweenTests()
    {
        var contains = new Contains(["ALPHA"], "i");
        Assert.That(contains.Test("alpha"), Is.True);

        contains.Search.Query[0] = "BETA";
        Assert.That(contains.Test("alpha"), Is.False);
        Assert.That(contains.Test("beta"), Is.True);

        contains.Search.Query.Add("GAMMA");
        Assert.That(contains.Test("gamma"), Is.True);

        contains.Search = new Search { Query = ["delta"], Mods = "i" };
        Assert.That(contains.Test("gamma"), Is.False);

        contains.Search.Mods = string.Empty;
        Assert.That(contains.Test("DELTA"), Is.False);
    }

    [Test]
    public void AndAndOrCombineResults()
    {
        IExpression yes = new Contains(["a"], string.Empty);
        IExpression no = new Contains(["z"], string.Empty);

        Assert.That(new And(new List<IExpression> { yes, yes }).Test("a"), Is.True);
        Assert.That(new And(new List<IExpression> { yes, no }).Test("a"), Is.False);
        Assert.That(new Or(new List<IExpression> { no, yes }).Test("a"), Is.True);
        Assert.That(new Or(new List<IExpression> { no, no }).Test("a"), Is.False);
    }

    private static IntegerRangeCollection Exactly(int count) => new(count.ToString(CultureInfo.InvariantCulture));
}
