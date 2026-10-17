using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JMW.Parsing.Compile;

public class Tokenizer
{
    public Token Token { get; } = new Token();

    public const string INVALID_UNICODE_ERROR = "Invalid Character Encoding for parsing instruction input";

    internal int Line => this.lineReader.Line;
    internal int Column => this.lineReader.Column;

    private const string objectStart = @"{";
    private const string objectStop = @"}";
    private const string arrayStart = @"[";
    private const string arrayStop = @"]";
    private const string propertyStart = @":";
    private const string commentStart = @"#";

    private static readonly HashSet<char> acceptableWordChars = new("abcdefghijklmnopqrstuvwxyz_-".ToCharArray());

    private readonly Scanner lineReader;

    private bool inWord = false;

    public Tokenizer(TextReader reader)
    {
        this.lineReader = new(reader, INVALID_UNICODE_ERROR);
    }

    public Tokenizer(string template) : this(new StringReader(template))
    {
    }

    public TokenType Next()
    {
        try
        {
            this.Token.Line = this.Line;
            this.Token.Column = this.Column;

            if (this.inWord)
            {
                this.inWord = false;

                // you have a word, is it a property or an object?
                if (this.MaybeReadObjectStart())
                {
                    return this.ConsumeObjectStart();
                }

                if (this.MaybeReadPropertyStart())
                {
                    return this.ConsumePropertyName();
                }

                // if its not a prop and its not an object, then its a set of options.
                this.Token.Type = TokenType.Options;
                return this.Token.Type;
            }

            if (this.MaybeReadArrayStart())
            {
                return this.ConsumeArrayStart();
            }

            if (this.MaybeReadCommentStart())
            {
                return this.ConsumeComment();
            }

            if (this.MaybeReadObjectStop())
            {
                return this.ConsumeObjectStop();
            }

            if (this.MaybeReadArrayStop())
            {
                return this.ConsumeArrayStop();
            }

            if (this.MaybeReadBeginPropertyValue())
            {
                return this.ConsumePropertyValue();
            }

            if (this.MaybeReadWord())
            {
                return this.ConsumeWord();
            }

            if (!this.lineReader.TryPeek(out char next))
            {
                return this.SetupErrorToken("EOF");
            }

            return IsSkippable(next)
                ? this.ConsumeWhitespace()
                : this.SetupErrorToken("Unexpected character '" + next + "'");
        }
        catch (Exception ex)
        {
            return this.SetupErrorToken(ex.Message);
        }
    }

    private TokenType SetupErrorToken(string msg)
    {
        return this.SetupErrorToken(msg, this.Line, this.Column);
    }

    private TokenType SetupErrorToken(string msg, int line, int column)
    {
        this.Token.Type = TokenType.Error;
        this.Token.Value = msg;
        this.Token.Line = line;
        this.Token.Column = column;
        return this.Token.Type;
    }

    private TokenType ConsumeComment()
    {
        if (this.lineReader.AtEnd)
        {
            return this.SetupErrorToken("EOF");
        }

        // A comment runs to the end of the line, or to the end of the input on the last line.
        var value = new StringBuilder();
        this.lineReader.ReadUntil('\n', value);
        this.Token.Type = TokenType.Comment;
        this.Token.Value = value.ToString().TrimEnd('\r');
        return this.Token.Type;
    }

    // Only called after a peek found the opening quote.
    private TokenType ConsumePropertyValue()
    {
        this.lineReader.TryRead(out char quote);
        var value = new StringBuilder();
        if (!this.lineReader.ReadUntil(quote, value))
        {
            return this.SetupErrorToken("Unexpected end of input. Did you forget a closing " + quote + "?");
        }

        this.Token.Type = TokenType.Value;
        this.Token.Value = value.ToString();
        return this.Token.Type;
    }

    private TokenType ConsumePropertyName()
    {
        this.Token.Type = TokenType.PropertyName;
        return this.Token.Type;
    }

    private TokenType ConsumeObjectStart()
    {
        this.Token.Type = TokenType.ObjectStart;
        return this.Token.Type;
    }

    private TokenType ConsumeObjectStop()
    {
        this.Token.Value = string.Empty;
        this.Token.Type = TokenType.ObjectStop;
        return this.Token.Type;
    }

    private TokenType ConsumeArrayStart()
    {
        this.Token.Type = TokenType.ArrayStart;
        return this.Token.Type;
    }

    private TokenType ConsumeArrayStop()
    {
        this.Token.Value = string.Empty;
        this.Token.Type = TokenType.ArrayStop;
        return this.Token.Type;
    }

    private TokenType ConsumeWord()
    {
        var identifier = new StringBuilder();
        this.lineReader.ReadWhile(IsWordChar, identifier);
        this.inWord = true;
        this.Token.Type = TokenType.Word;
        this.Token.Value = identifier.ToString();
        return this.ConsumeWhitespace();
    }

    private TokenType ConsumeWhitespace()
    {
        this.lineReader.ReadWhile(IsSkippable); // ignore the ';', its optional.
        return this.Next();
    }

    private static bool IsSkippable(char c)
    {
        return char.IsWhiteSpace(c) || c == ';' || c == ',';
    }

    private static bool IsWordChar(char c)
    {
        return !char.IsWhiteSpace(c) && acceptableWordChars.Contains(c);
    }

    private bool MaybeReadWord()
    {
        return this.lineReader.TryPeek(out char c) && acceptableWordChars.Contains(c);
    }

    private bool MaybeReadCommentStart()
    {
        return this.MaybeReadText(commentStart);
    }

    private bool MaybeReadObjectStart()
    {
        return this.MaybeReadText(objectStart);
    }

    private bool MaybeReadObjectStop()
    {
        return this.MaybeReadText(objectStop);
    }

    private bool MaybeReadArrayStart()
    {
        return this.MaybeReadText(arrayStart);
    }

    private bool MaybeReadArrayStop()
    {
        return this.MaybeReadText(arrayStop);
    }

    private bool MaybeReadPropertyStart()
    {
        return this.MaybeReadText(propertyStart);
    }

    private bool MaybeReadBeginPropertyValue()
    {
        return this.lineReader.TryPeek(out char c) && (c == '"' || c == '\'');
    }

    private bool MaybeReadText(string text)
    {
        return this.lineReader.TryMatch(text);
    }
}