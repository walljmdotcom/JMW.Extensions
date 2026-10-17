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
    private bool eof = false;

    internal int Line => reader.Line;
    internal int Column => reader.Column;

    internal Tokenizer(string template)
        : this(new StringReader(template))
    {
    }

    internal Tokenizer(TextReader reader)
    {
        this.reader = new(reader);
    }

    internal TokenType Next()
    {
        try
        {
            // A word that ran into the end of input is still pending and has to be emitted first.
            if (eof && !inWord)
            {
                return SetupErrorToken("EOF");
            }
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
                eof = true;
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

    private TokenType ConsumeUntil(char cc, TokenType type)
    {
        Token.Type = type;
        var more = reader.TryRead(out char c);
        var value = new List<char>();
        while (more && c != cc && c != 0)
        {
            value.Add(c);
            more = reader.TryRead(out c);
            // This is the unicode invalid character. If we encounter this it means we parsed the
            // template with an invalid encoding. Or the template was stored with an invalid
            // encoding.
            if (more && c == '\uffff')
            {
                return SetupErrorToken(INVALID_UNICODE_ERROR);
            }
        }
        if (!more)
        {
            eof = true;
        }
        else if (c != cc)
        {
            reader.PushBack(c);
        }
        Token.Value = new string(value.ToArray());
        return type;
    }

    private TokenType ConsumeComment()
    {
        if (reader.AtEnd)
        {
            eof = true;
            return SetupErrorToken("EOF");
        }

        ConsumeUntil('\n', TokenType.Comment);
        Token.Value = Token.Value.TrimEnd('\r', '\n');
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

        if (!reader.TryRead(out char c))
        {
            return SetupErrorToken(UNTERMINATED_COMMENT_ERROR);
        }
        text.Add(c);

        while (!(text.Count == 2 && text[0] == '*' && text[1] == '/'))
        {
            if (!reader.TryRead(out c))
            {
                return SetupErrorToken(UNTERMINATED_COMMENT_ERROR);
            }
            // This is the unicode invalid character. If we encounter this it means we parsed the
            // template with an invalid encoding. Or the template was stored with an invalid
            // encoding.
            if (c == '\uffff')
            {
                return SetupErrorToken(INVALID_UNICODE_ERROR);
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
        var identifier = new List<char>();
        var more = reader.TryRead(out char c);
        if (more && c == '"')
        {
            inWord = true;
            reader.PushBack(c);
            return ConsumeQuotedString();
        }

        while (more && !char.IsWhiteSpace(c) && !unacceptableWordChars.Contains(c))
        {
            identifier.Add(c);
            more = reader.TryRead(out c);
            // This is the unicode invalid character. If we encounter this it means we parsed the
            // template with an invalid encoding. Or the template was stored with an invalid
            // encoding.
            if (more && c == '\uffff')
            {
                return SetupErrorToken(INVALID_UNICODE_ERROR);
            }
        }
        if (!more)
        {
            eof = true;
        }
        else
        {
            reader.PushBack(c);
        }

        inWord = true;
        Token.Type = TokenType.Word;
        Token.Value = new string(identifier.ToArray());
        return ConsumeWhitespace();
    }

    private TokenType ConsumeQuotedString()
    {
        var text = new StringBuilder();

        if (!reader.TryRead(out char c) || c != '"')
        {
            return Next();
        }

        reader.ReadWhile(static ch => ch != '"' && ch != '\uffff', text);
        if (!reader.TryRead(out c))
        {
            return SetupErrorToken(UNTERMINATED_STRING_ERROR);
        }
        // This is the unicode invalid character. If we encounter this it means we parsed the
        // template with an invalid encoding. Or the template was stored with an invalid
        // encoding.
        if (c == '\uffff')
        {
            return SetupErrorToken(INVALID_UNICODE_ERROR);
        }

        Token.Value = text.ToString();
        Token.Type = TokenType.Word;
        return Next();
    }

    private TokenType ConsumeWhitespace()
    {
        var skipped = reader.ReadWhile(char.IsWhiteSpace); // ignore the ';', its optional.
        if (!reader.TryRead(out char c))
        {
            eof = true;
        }
        // This is the unicode invalid character. If we encounter this it means we parsed the
        // template with an invalid encoding. Or the template was stored with an invalid
        // encoding.
        else if (skipped > 0 && c == '\uffff')
        {
            return SetupErrorToken(INVALID_UNICODE_ERROR);
        }
        else
        {
            reader.PushBack(c);
        }
        return Next();
    }

    private bool MaybeReadWord()
    {
        if (!reader.TryPeek(out char c))
        {
            eof = true;
            return false;
        }

        return !unacceptableWordChars.Contains(c);
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
        if (reader.AtEnd)
        {
            eof = true;
            return false;
        }

        return reader.TryMatch(text);
    }
}