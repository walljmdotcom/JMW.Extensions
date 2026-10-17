using JMW.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JMW.Networking.Parsers.JunosConfig;

internal class Tokenizer
{
    private static readonly HashSet<char> unacceptableWordChars = new("{}[]#;".ToCharArray());
    private readonly string objectStart = @"{";
    private readonly string objectStop = @"}";
    private readonly string arrayStart = @"[";
    private readonly string arrayStop = @"]";
    private readonly string commentBlockStart = @"/*";
    private readonly string commentStart = @"#";
    private readonly string lineStop = @";";

    private readonly Scanner reader;

    internal Token Token { get; } = new();

    internal static readonly string INVALID_UNICODE_ERROR = "Invalid Character Encoding for parsing instruction input";
    internal static readonly string UNTERMINATED_COMMENT_ERROR = "Unexpected end of input. Did you forget to close a /* comment with */?";
    internal static readonly string UNTERMINATED_STRING_ERROR = "Unexpected end of input. Did you forget a closing \"?";

    private bool inWord = false;

    internal int Line => reader.Line;
    internal int Column => reader.Column;

    internal Tokenizer(string template)
        : this(new StringReader(template))
    {
    }

    internal Tokenizer(TextReader reader)
    {
        this.reader = new(reader, INVALID_UNICODE_ERROR);
    }

    internal TokenType Next()
    {
        try
        {
            Token.Line = Line;
            Token.Column = Column;

            if (inWord)
            {
                inWord = false;

                // you have a word, is it a property or an object?
                if (MaybeReadObjectStart())
                {
                    return ConsumeObjectStart();
                }

                // ignore empty words.
                if (Token.Value.Length > 0)
                {
                    Token.Type = TokenType.Word;
                    return Token.Type;
                }
            }

            if (MaybeReadArrayStart())
            {
                return ConsumeArrayStart();
            }

            if (MaybeReadCommentBlockStart())
            {
                return ConsumeCommentBlockStart();
            }

            if (MaybeReadCommentStart())
            {
                return ConsumeComment();
            }

            if (MaybeReadObjectStop())
            {
                return ConsumeObjectStop();
            }

            if (MaybeReadArrayStop())
            {
                return ConsumeArrayStop();
            }
            if (MaybeReadLineStop())
            {
                return ConsumeLineStop();
            }
            if (MaybeReadWord())
            {
                return ConsumeWord();
            }

            if (!reader.TryPeek(out char next))
            {
                return SetupErrorToken("EOF");
            }

            return char.IsWhiteSpace(next)
                ? ConsumeWhitespace()
                : SetupErrorToken("Unexpected character '" + next + "'");
        }
        catch (Exception ex)
        {
            return SetupErrorToken(ex.Message);
        }
    }

    private TokenType SetupErrorToken(string msg)
    {
        return SetupErrorToken(msg, Line, Column);
    }

    private TokenType SetupErrorToken(string msg, int line, int column)
    {
        Token.Type = TokenType.Error;
        Token.Value = msg;
        Token.Line = line;
        Token.Column = column;
        return Token.Type;
    }

    private TokenType ConsumeComment()
    {
        if (reader.AtEnd)
        {
            return SetupErrorToken("EOF");
        }

        // A comment runs to the end of the line, or to the end of the input on the last line.
        var value = new StringBuilder();
        reader.ReadUntil('\n', value);
        Token.Type = TokenType.Comment;
        Token.Value = value.ToString().TrimEnd('\r');
        return Token.Type;
    }

    private TokenType ConsumeObjectStart()
    {
        Token.Type = TokenType.ObjectStart;
        return Token.Type;
    }

    private TokenType ConsumeObjectStop()
    {
        Token.Value = string.Empty;
        Token.Type = TokenType.ObjectStop;
        return Token.Type;
    }

    private TokenType ConsumeArrayStart()
    {
        Token.Type = TokenType.ArrayStart;
        return Token.Type;
    }

    private TokenType ConsumeCommentBlockStart()
    {
        var text = new RingBuffer<char>(2);

        while (!(text.Count == 2 && text[0] == '*' && text[1] == '/'))
        {
            if (!reader.TryRead(out char c))
            {
                return SetupErrorToken(UNTERMINATED_COMMENT_ERROR);
            }
            text.Add(c);
        }

        return Next();
    }

    private TokenType ConsumeArrayStop()
    {
        Token.Value = string.Empty;
        Token.Type = TokenType.ArrayStop;
        return Token.Type;
    }

    private TokenType ConsumeLineStop()
    {
        Token.Value = string.Empty;
        Token.Type = TokenType.LineStop;
        return Token.Type;
    }

    private TokenType ConsumeWord()
    {
        inWord = true;
        if (reader.TryMatch("\""))
        {
            return ConsumeQuotedString();
        }

        var identifier = new StringBuilder();
        reader.ReadWhile(static c => !char.IsWhiteSpace(c) && !unacceptableWordChars.Contains(c), identifier);
        Token.Type = TokenType.Word;
        Token.Value = identifier.ToString();
        return ConsumeWhitespace();
    }

    // Called after the opening quote.
    private TokenType ConsumeQuotedString()
    {
        var text = new StringBuilder();
        if (!reader.ReadUntil('"', text))
        {
            return SetupErrorToken(UNTERMINATED_STRING_ERROR);
        }

        Token.Value = text.ToString();
        Token.Type = TokenType.Word;
        return Next();
    }

    private TokenType ConsumeWhitespace()
    {
        reader.ReadWhile(char.IsWhiteSpace); // ignore the ';', its optional.
        return Next();
    }

    private bool MaybeReadWord()
    {
        return reader.TryPeek(out char c) && !unacceptableWordChars.Contains(c);
    }

    private bool MaybeReadCommentStart()
    {
        return MaybeReadText(commentStart);
    }

    private bool MaybeReadObjectStart()
    {
        return MaybeReadText(objectStart);
    }

    private bool MaybeReadObjectStop()
    {
        return MaybeReadText(objectStop);
    }

    private bool MaybeReadArrayStart()
    {
        return MaybeReadText(arrayStart);
    }

    private bool MaybeReadCommentBlockStart()
    {
        return MaybeReadText(commentBlockStart);
    }

    private bool MaybeReadArrayStop()
    {
        return MaybeReadText(arrayStop);
    }

    private bool MaybeReadLineStop()
    {
        return MaybeReadText(lineStop);
    }

    private bool MaybeReadText(string text)
    {
        return reader.TryMatch(text);
    }
}