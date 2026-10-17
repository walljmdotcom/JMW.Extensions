using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace JMW.Tests;

[TestFixture]
public class ScannerTests
{
    private const string InvalidMessage = "bad encoding";

    private static Scanner Create(string text)
    {
        return new Scanner(new StringReader(text), InvalidMessage);
    }

    private const char Invalid = (char)0xFFFF;

    // U+FFFF, written as '~' in the cases, is rejected wherever it sits and by whichever member reaches it first.
    [TestCase("~")]
    [TestCase("~ab")]
    [TestCase("ab~")]
    [TestCase("a\n~\nb")]
    public void EveryMemberRejectsInvalidCharacter(string text)
    {
        text = text.Replace('~', Invalid);
        int at = text.IndexOf(Invalid, StringComparison.Ordinal);
        var reads = new (string Name, Action<Scanner> Act)[]
        {
            ("TryRead", static s => s.TryRead(out _)),
            ("TryPeek", static s => s.TryPeek(out _)),
            ("AtEnd", static s => _ = s.AtEnd),
            ("TryMatch", static s => s.TryMatch(Invalid.ToString())),
            ("ReadWhile", static s => s.ReadWhile(static _ => true)),
            ("ReadUntil", static s => s.ReadUntil('z', new StringBuilder())),
        };

        foreach ((string name, Action<Scanner> act) in reads)
        {
            Scanner scanner = Create(text);
            ReadCount(scanner, at);
            InvalidDataException? ex = Assert.Throws<InvalidDataException>(() => act(scanner), name);
            Assert.That(ex?.Message, Is.EqualTo(InvalidMessage), name);
        }

        // Reaching it through a longer read rejects it too.
        Assert.Throws<InvalidDataException>(() => Create(text).ReadWhile(static _ => true));
        Assert.Throws<InvalidDataException>(() => Create(text).ReadUntil('z', new StringBuilder()));
    }

    [TestCase(0xFFFE)]
    [TestCase(0xFFFD)]
    [TestCase(0)]
    public void OtherCharactersAreNotRejected(int code)
    {
        Scanner scanner = Create(((char)code).ToString());

        Assert.That(scanner.TryRead(out char c), Is.True);
        Assert.That(c, Is.EqualTo((char)code));
    }

    private static void ReadCount(Scanner scanner, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Assert.That(scanner.TryRead(out _), Is.True);
        }
    }

    [TestCase("", 0)]
    [TestCase("a", 1)]
    [TestCase("ab\ncd", 5)]
    public void PeekAndReadReturnFalseAtEnd(string text, int length)
    {
        Scanner scanner = Create(text);
        ReadCount(scanner, length);

        Assert.That(scanner.AtEnd, Is.True);
        Assert.That(scanner.TryPeek(out char peeked), Is.False);
        Assert.That(peeked, Is.EqualTo(default(char)));
        Assert.That(scanner.TryRead(out char read), Is.False);
        Assert.That(read, Is.EqualTo(default(char)));
        // Reading past the end stays at the end and leaves the position alone.
        Assert.That(scanner.TryRead(out _), Is.False);
        Assert.That(scanner.AtEnd, Is.True);
    }

    [Test]
    public void EmptyInputHasNothingToReadOrMatch()
    {
        Scanner scanner = Create(string.Empty);

        Assert.That(scanner.AtEnd, Is.True);
        Assert.That(scanner.TryMatch("a"), Is.False);
        Assert.That(scanner.ReadWhile(static _ => true), Is.EqualTo(0));
        StringBuilder into = new();
        Assert.That(scanner.ReadUntil('a', into), Is.False);
        Assert.That(into.ToString(), Is.Empty);
        Assert.That(scanner.Line, Is.EqualTo(0));
        Assert.That(scanner.Column, Is.EqualTo(0));
    }

    [Test]
    public void PeekDoesNotConsume()
    {
        Scanner scanner = Create("xy");

        Assert.That(scanner.TryPeek(out char first), Is.True);
        Assert.That(first, Is.EqualTo('x'));
        Assert.That(scanner.Column, Is.EqualTo(0));
        Assert.That(scanner.TryPeek(out char again), Is.True);
        Assert.That(again, Is.EqualTo('x'));
        Assert.That(scanner.TryRead(out char read), Is.True);
        Assert.That(read, Is.EqualTo('x'));
        Assert.That(scanner.Column, Is.EqualTo(1));
    }

    // Only '\n' starts a new line. A '\r' counts as a column, so "\r\n" ends at column 0 of the next line and a
    // lone '\r' stays on the same line.
    [TestCase("a", 1, 0, 1)]
    [TestCase("ab\n", 3, 1, 0)]
    [TestCase("ab\ncd", 5, 1, 2)]
    [TestCase("ab\r\n", 3, 0, 3)]
    [TestCase("ab\r\n", 4, 1, 0)]
    [TestCase("ab\r\ncd", 6, 1, 2)]
    [TestCase("ab\rcd", 3, 0, 3)]
    [TestCase("ab\rcd", 5, 0, 5)]
    [TestCase("\n\n\n", 3, 3, 0)]
    [TestCase("\r\n\r\n", 4, 2, 0)]
    [TestCase("\r\r", 2, 0, 2)]
    public void TracksLineAndColumn(string text, int read, int line, int column)
    {
        Scanner scanner = Create(text);
        ReadCount(scanner, read);

        Assert.That(scanner.Line, Is.EqualTo(line));
        Assert.That(scanner.Column, Is.EqualTo(column));
    }

    // Reads `read` characters, pushes back the last `pushed` of them, and checks the position is where it was
    // after reading only `read - pushed`.
    [TestCase("ab\ncd", 4, 2)]
    [TestCase("ab\ncd", 5, 3)]
    [TestCase("ab\ncd", 5, 5)]
    [TestCase("ab\r\ncd", 5, 3)]
    [TestCase("ab\r\ncd", 4, 1)]
    [TestCase("a\n\n\nb", 5, 4)]
    [TestCase("ab\rcd", 4, 2)]
    public void PushBackAcrossLineBreaksRestoresPosition(string text, int read, int pushed)
    {
        Scanner expected = Create(text);
        ReadCount(expected, read - pushed);

        Scanner scanner = Create(text);
        StringBuilder consumed = new();
        for (int i = 0; i < read; i++)
        {
            Assert.That(scanner.TryRead(out char c), Is.True);
            consumed.Append(c);
        }

        for (int i = read - 1; i >= read - pushed; i--)
        {
            scanner.PushBack(consumed[i]);
        }

        Assert.That(scanner.Line, Is.EqualTo(expected.Line));
        Assert.That(scanner.Column, Is.EqualTo(expected.Column));

        StringBuilder rest = new();
        while (scanner.TryRead(out char c))
        {
            rest.Append(c);
        }

        Assert.That(rest.ToString(), Is.EqualTo(text.Substring(read - pushed)));
    }

    // TryMatch consumes the text on a match and leaves the input and position unchanged otherwise.
    [TestCase("abc", "ab", true, "c")]
    [TestCase("abc", "abc", true, "")]
    [TestCase("abc", "abd", false, "abc")]
    [TestCase("abc", "x", false, "abc")]
    [TestCase("ab", "abc", false, "ab")]
    [TestCase("", "a", false, "")]
    [TestCase("a\nb", "a\nc", false, "a\nb")]
    [TestCase("a\nb", "a\n", true, "b")]
    public void TryMatchConsumesOnlyOnMatch(string text, string match, bool matched, string remaining)
    {
        Scanner scanner = Create(text);

        Assert.That(scanner.TryMatch(match), Is.EqualTo(matched));

        Scanner expected = Create(text);
        ReadCount(expected, text.Length - remaining.Length);
        Assert.That(scanner.Line, Is.EqualTo(expected.Line));
        Assert.That(scanner.Column, Is.EqualTo(expected.Column));

        StringBuilder rest = new();
        while (scanner.TryRead(out char c))
        {
            rest.Append(c);
        }

        Assert.That(rest.ToString(), Is.EqualTo(remaining));
    }

    [TestCase("   x", 3, "x")]
    [TestCase("x", 0, "x")]
    [TestCase("   ", 3, "")]
    [TestCase("", 0, "")]
    [TestCase(" \r\n\tx ", 4, "x ")]
    public void ReadWhileStopsBeforeTheFirstFailingCharacter(string text, int count, string remaining)
    {
        Scanner scanner = Create(text);
        StringBuilder into = new();

        Assert.That(scanner.ReadWhile(char.IsWhiteSpace, into), Is.EqualTo(count));
        Assert.That(into.ToString(), Is.EqualTo(text.Substring(0, count)));

        StringBuilder rest = new();
        while (scanner.TryRead(out char c))
        {
            rest.Append(c);
        }

        Assert.That(rest.ToString(), Is.EqualTo(remaining));
    }

    [Test]
    public void ReadWhileWithoutABuilderOnlySkips()
    {
        Scanner scanner = Create("  x");

        Assert.That(scanner.ReadWhile(char.IsWhiteSpace), Is.EqualTo(2));
        Assert.That(scanner.TryPeek(out char next), Is.True);
        Assert.That(next, Is.EqualTo('x'));
        Assert.That(scanner.Column, Is.EqualTo(2));
    }

    // ReadUntil consumes the terminator when present and stops at the end of input when it is not.
    [TestCase("abc\"def", '"', true, "abc", "def")]
    [TestCase("\"def", '"', true, "", "def")]
    [TestCase("abc\"", '"', true, "abc", "")]
    [TestCase("abc", '"', false, "abc", "")]
    [TestCase("", '"', false, "", "")]
    [TestCase("ab\ncd\nef", '\n', true, "ab", "cd\nef")]
    [TestCase("ab\r\ncd", '\n', true, "ab\r", "cd")]
    [TestCase("a\nb\"c", '"', true, "a\nb", "c")]
    public void ReadUntilConsumesTheTerminator(
        string text,
        char terminator,
        bool found,
        string value,
        string remaining
    )
    {
        Scanner scanner = Create(text);
        StringBuilder into = new();

        Assert.That(scanner.ReadUntil(terminator, into), Is.EqualTo(found));
        Assert.That(into.ToString(), Is.EqualTo(value));
        Assert.That(scanner.AtEnd, Is.EqualTo(remaining.Length == 0));

        StringBuilder rest = new();
        while (scanner.TryRead(out char c))
        {
            rest.Append(c);
        }

        Assert.That(rest.ToString(), Is.EqualTo(remaining));
    }

    [Test]
    public void ReadUntilAfterLineBreaksReportsTheLineAndColumnPastTheTerminator()
    {
        Scanner scanner = Create("a\r\nbc'd");

        Assert.That(scanner.ReadUntil('\'', new StringBuilder()), Is.True);
        Assert.That(scanner.Line, Is.EqualTo(1));
        Assert.That(scanner.Column, Is.EqualTo(3));
    }
}
