using System.Collections.Generic;
using JMW.Parsing.Compile;
using NUnit.Framework;

namespace JMW.Parsing.Tests;

[TestFixture]
public class TokenizerTest
{
    private static List<(TokenType Type, string Value)> Drain(string input)
    {
        var tokenizer = new Tokenizer(input);
        var tokens = new List<(TokenType, string)>();
        for (int i = 0; i < 100; i++)
        {
            TokenType type = tokenizer.Next();
            tokens.Add((type, tokenizer.Token.Value));
            if (type == TokenType.Error)
            {
                break;
            }
        }

        return tokens;
    }

    // U+FFFF inserted at every position, in words, quoted values, comments, and whitespace.
    [TestCase("foo { s:[\"x\" 'y']i } # comment\nbar: \"v\"")]
    [TestCase("a;b,c")]
    public void InvalidCharacterAnywhereIsAnError(string input)
    {
        for (int i = 0; i <= input.Length; i++)
        {
            List<(TokenType Type, string Value)> tokens = Drain(input.Insert(i, ((char)0xFFFF).ToString()));

            Assert.That(tokens[^1], Is.EqualTo((TokenType.Error, Tokenizer.INVALID_UNICODE_ERROR)), "at " + i);
        }
    }

    [TestCase("\"", '"')]
    [TestCase("\"x", '"')]
    [TestCase("'x", '\'')]
    [TestCase("foo: \"x", '"')]
    [TestCase("foo {s:[\"x\" 'y", '\'')]
    public void UnterminatedQuoteIsAnError(string input, char quote)
    {
        List<(TokenType Type, string Value)> tokens = Drain(input);

        Assert.That(
            tokens[^1],
            Is.EqualTo((TokenType.Error, "Unexpected end of input. Did you forget a closing " + quote + "?"))
        );
    }

    // A word that runs into the end of input is still emitted before EOF.
    [TestCase("foo", "foo")]
    [TestCase("foo ", "foo")]
    [TestCase("a;foo", "foo")]
    [TestCase("x {s:[\"y\"]i", "i")]
    public void TrailingWordIsEmittedBeforeEof(string input, string word)
    {
        List<(TokenType Type, string Value)> tokens = Drain(input);

        Assert.That(tokens[^2], Is.EqualTo((TokenType.Options, word)));
        Assert.That(tokens[^1], Is.EqualTo((TokenType.Error, "EOF")));
    }
}
