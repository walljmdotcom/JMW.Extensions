using JMW.Networking.Parsers.JunosConfig;
using JMW.Tests;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace JMW.Parsing.Tests;

[TestFixture]
public class JunosParsingTest : TestBase
{
    [Test]
    public void ParseTest()
    {
        var dir = Path.Combine(ExecutionPath, "Networking", "junos.txt");
        var junos = File.ReadAllText(dir);
        _ = new JMW.Networking.Parsers.JunosConfig.Parser().Parse(junos);
    }

    // Parsing runs on a worker task so a tokenizer that loops forever fails the test instead of hanging the run.
    private static List<Tag> ParseWithin(string input, CancellationToken cancellationToken)
    {
        var task = Task.Run(() => new Parser().Parse(input), cancellationToken);
        try
        {
            task.Wait(cancellationToken);
        }
        catch (AggregateException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
        return task.Result;
    }

    [TestCase("system { host-name foo; }")]
    [TestCase("system { host-name foo; }\n")]
    [TestCase("system { host-name foo; }\r\n")]
    [TestCase("system {\n    host-name foo;\n}")]
    [TestCase("system {\r\n    host-name foo;\r\n}")]
    [TestCase("system { host-name foo; } # trailing comment")]
    [TestCase("system { host-name foo; } # trailing comment\n")]
    [TestCase("system { host-name foo; } # trailing comment\r\n")]
    [TestCase("# leading comment\nsystem { host-name foo; }")]
    [TestCase("/* block */ system { host-name foo; }")]
    [TestCase("system { /* block */ host-name foo; }")]
    [TestCase("system { host-name \"foo\"; }")]
    [TestCase("system { host-name \"foo\"; }\n")]
    [CancelAfter(10000)]
    public void Parse_CompleteInput_ReturnsSystemObject(string input, CancellationToken cancellationToken)
    {
        var tags = ParseWithin(input, cancellationToken);

        Assert.That(tags, Has.Count.EqualTo(1));
        Assert.That(tags[0].Name, Is.EqualTo("system"));
        Assert.That(tags[0].Children, Has.Count.EqualTo(1));
        var child = tags[0].Children.Peek();
        Assert.That(child.Name, Is.EqualTo("host-name"));
        Assert.That(child.Value, Is.EqualTo("foo"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\n")]
    [TestCase("\r\n")]
    [TestCase(" \t\r\n  ")]
    [TestCase("# only a comment")]
    [TestCase("# only a comment\n")]
    [TestCase("# only a comment\r\n")]
    [TestCase("#")]
    [TestCase("/* only a block */")]
    [TestCase("/* only a block */\n")]
    [TestCase("/**/")]
    [TestCase("/** starred */")]
    [TestCase("/***/")]
    [CancelAfter(10000)]
    public void Parse_NothingToParse_ReturnsEmpty(string input, CancellationToken cancellationToken)
    {
        Assert.That(ParseWithin(input, cancellationToken), Is.Empty);
    }

    [TestCase("/*")]
    [TestCase("/* unterminated")]
    [TestCase("/* unterminated\n")]
    [TestCase("/* unterminated\r\n")]
    [TestCase("/* almost *")]
    [TestCase("system { /* unterminated")]
    [TestCase("system { host-name \"foo")]
    [TestCase("system { host-name \"foo\n")]
    [TestCase("system { host-name \"foo\r\n")]
    [TestCase("system { host-name \"")]
    [TestCase("system { host-name foo; ")]
    [TestCase("system { host-name foo; # comment")]
    [TestCase("system { host-name foo")]
    [TestCase("system { host-name foo\n")]
    [TestCase("host-name")]
    [TestCase("}")]
    [TestCase("system { } }")]
    [TestCase("]")]
    [TestCase("{")]
    [TestCase("system;{")]
    [CancelAfter(10000)]
    public void Parse_MalformedInput_ThrowsParseException(string input, CancellationToken cancellationToken)
    {
        Assert.Throws<ParseException>(() => ParseWithin(input, cancellationToken));
    }

    // U+FFFF inserted at every position, in words, quoted strings, comments, block comments, and whitespace.
    [TestCase("system { host-name \"foo bar\"; } # comment\n/* block */ x;")]
    [TestCase("[ a b ]")]
    [CancelAfter(10000)]
    public void Parse_InvalidCharacterAnywhere_ThrowsEncodingError(string input, CancellationToken cancellationToken)
    {
        for (var i = 0; i <= input.Length; i++)
        {
            var ex = Assert.Throws<ParseException>(() => ParseWithin(input.Insert(i, ((char)0xFFFF).ToString()), cancellationToken));
            Assert.That(ex?.ErrorText, Does.Contain("Invalid Character Encoding"), "at " + i);
        }
    }

    [Test]
    [CancelAfter(10000)]
    public void Parse_AfterFailure_SameParserStartsClean(CancellationToken cancellationToken)
    {
        var parser = new Parser();
        var task = Task.Run(
            () =>
            {
                Assert.Throws<ParseException>(() => parser.Parse("system { host-name foo;"));
                return parser.Parse("interfaces { lo0; }");
            },
            cancellationToken
        );
        task.Wait(cancellationToken);

        Assert.That(task.Result, Has.Count.EqualTo(1));
        Assert.That(task.Result[0].Name, Is.EqualTo("interfaces"));
    }
}