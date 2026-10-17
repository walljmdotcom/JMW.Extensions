using JMW.Parsing.Extractors;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace JMW.Parsing.Tests;

/// <summary>
/// Extractors reuse their query between calls, so a query changed after the first call must still take effect.
/// </summary>
[TestFixture]
public class ExtractorQueryTest
{
    private const string Line = "a,b;c d";

    private static readonly Dictionary<string, Action<Base>> Changes = new()
    {
        ["item replaced"] = static e => e.Search.Query[0] = ";",
        ["count changed"] = static e =>
        {
            e.Search.Query.Clear();
            e.Search.Query.AddRange([";", ";"]);
        },
        ["list replaced"] = static e => e.Search.Query = [";"],
        ["search replaced"] = static e => e.Search = new Search { Query = [";"] },
    };

    [Test]
    public void SeesQueryChangedBetweenCalls(
        [Values("item replaced", "count changed", "list replaced", "search replaced")] string change,
        [Values("to", "after", "split")] string extractor
    )
    {
        Base e = extractor switch
        {
            "to" => new To([","], string.Empty, -1),
            "after" => new After([","], string.Empty, -1),
            _ => new Split([","], string.Empty, 1),
        };

        var first = e.Parse(Line);
        Changes[change](e);
        var second = e.Parse(Line);

        var (expectedFirst, expectedSecond) = extractor switch
        {
            "to" => ("a", "a,b"),
            "after" => (",b;c d", ";c d"),
            _ => ("b;c d", "c d"),
        };

        Assert.That(first, Is.EqualTo(expectedFirst));
        Assert.That(second, Is.EqualTo(expectedSecond));
    }
}
