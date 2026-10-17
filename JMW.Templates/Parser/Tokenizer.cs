using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

// Internals are visible to the Test assemblies
[assembly: InternalsVisibleTo("JMW.Tests")]

namespace JMW.Templates
{
    public class Tokenizer
    {
        internal Scanner _reader;
        public Token Token
        {
            get;
        }

        public const string INVALID_UNICODE_ERROR = "Invalid Character Encoding for template input";

        private const char BEGIN = '<';
        private const string END = @"</";
        private const char CLOSE = '>';
        private const string SINGLE_CLOSE = @"/>";
        private const char FORWARD_SLASH = '/';
        private const char COMMENT_START = '#';
        private const char PARAM_ASSIGN = '=';
        private const char ESCAPE = '\\';
        private const string EOF = "EOF";
        private const string UNCLOSED_TAG_ERROR = "Invalid tag. Did you forget to add a >?";

        internal bool _inTag = false;
        internal bool _inParam = false;

        internal int Line
        {
            get { return _reader.Line; }
        }

        internal int Column
        {
            get { return _reader.Column; }
        }

        public Tokenizer(string template)
            : this(new StringReader(template))
        {
        }

        public Tokenizer(TextReader reader)
        {
            _reader = new Scanner(reader, INVALID_UNICODE_ERROR);
            Token = new Token();
        }

        public TokenType Next()
        {
            NEXT_START:
            try
            {
                if (_reader.AtEnd)
                {
                    // A tag must end with a closing sigil, so running out inside one is an error.
                    return setupErrorToken(_inTag ? UNCLOSED_TAG_ERROR : EOF);
                }

                Token.Line = Line;
                Token.Column = Column;

                if (maybeReadCommentStart())
                {
                    return consumeComment();
                }

                if (maybeReadEnd())
                {
                    return consumeTagName(TokenType.ENDTAG);
                }

                if (maybeReadBegin())
                {
                    return consumeTagName(TokenType.OPENTAG);
                }

                // Close sigils end the current tag. Outside a tag a '>' is ordinary text.
                if (_inTag && maybeReadClose())
                {
                    _inTag = false;
                    // yes yes it's a goto. This is a state machine deal with it.
                    goto NEXT_START;
                }

                if (maybeReadSingleClose())
                {
                    _inTag = false;
                    Token.Type = TokenType.SINGLEClOSE;
                    Token.Value = string.Empty;
                    return Token.Type;
                }

                if (_inTag) // if we are in a tag then we expect to start reading parameters.
                {
                    if (maybeReadText(PARAM_ASSIGN.ToString()))
                    {
                        return !_inParam
                            ? setupErrorToken("Unexpected " + PARAM_ASSIGN + ". Did you forget a param name?", Line, Column - 1)
                            : consumeParamValue();
                    }

                    return consumeParamName();
                }

                return consumeTextOrTag();
            }
            catch (Exception ex)
            {
                return setupErrorToken(ex.Message);
            }
        }

        private TokenType setupErrorToken(string msg)
        {
            return setupErrorToken(msg, Line, Column);
        }

        private TokenType setupErrorToken(string msg, int line, int column)
        {
            Token.Type = TokenType.ERROR;
            Token.Value = msg;
            Token.Line = line;
            Token.Column = column;
            return Token.Type;
        }

        private static bool isComment(StringBuilder tb, int c)
        {
            return c == COMMENT_START && (tb.Length == 0 || tb[tb.Length - 1] != ESCAPE);
        }

        internal TokenType consumeTextOrTag()
        {
            Token.Type = TokenType.TEXT;
            var consumed = false;
            var tb = new StringBuilder();
            var more = _reader.TryRead(out char c);
            var l = -1;
            while (more && c != BEGIN && !isComment(tb, c))
            {
                consumed = true;
                if (l == ESCAPE && c == COMMENT_START)
                {
                    // you need to replace the escape with the actual char
                    tb[tb.Length - 1] = c;
                }
                else
                    tb.Append(c);
                l = c;
                more = _reader.TryRead(out c);
            }
            if (more)
            {
                _reader.PushBack(c);
            }
            if (consumed)
            {
                Token.Value = tb.ToString();
                return Token.Type;
            }

            return Next();
        }

        internal TokenType consumeParamValue()
        {
            // this function only gets called when its reading a tag name, a parameter name, or a param value.
            //  that being the case, if you hit the end of the file, then that's a syntax error, because you
            //  would always want to find a closing tag marker.
            if (!_reader.TryRead(out char c))
            {
                return setupErrorToken(UNCLOSED_TAG_ERROR);
            }

            switch (c)
            {
                case '"':
                case '\'':
                    return consumeUntil(c, TokenType.PARAMVALUE);
            }

            _reader.PushBack(c);
            return consumeIdentifier(TokenType.PARAMVALUE);
        }

        internal TokenType consumeUntil(char qry, TokenType type)
        {
            Token.Type = type;
            var value = new StringBuilder();
            if (!_reader.ReadUntil(qry, value))
            {
                return setupErrorToken("Unexpected end of template.  Did you forget to add a " + qry + "?", Line, Column);
            }

            Token.Value = value.ToString();
            return type;
        }

        internal TokenType consumeParamName()
        {
            _inParam = true;
            return consumeIdentifier(TokenType.PARAMNAME);
        }

        internal TokenType consumeTagName(TokenType type)
        {
            if (_inTag)
            {
                return setupErrorToken(UNCLOSED_TAG_ERROR, Line, Column - 2);
            }
            _inTag = true;
            return consumeIdentifier(type);
        }

        internal TokenType consumeIdentifier(TokenType type)
        {
            var identifier = new StringBuilder();
            _reader.ReadWhile(
                static c => c != FORWARD_SLASH && c != CLOSE && c != BEGIN && c != PARAM_ASSIGN && !char.IsWhiteSpace(c),
                identifier
            );
            if (_reader.AtEnd)
            {
                // this function only gets called when its reading a tag name, a parameter name, or a param value.
                //  that being the case, if you hit the end of the file, then that's a syntax error, because you
                //  would always want to find a closing tag marker.
                return setupErrorToken(UNCLOSED_TAG_ERROR);
            }

            Token.Type = type;
            Token.Value = identifier.ToString();
            consumeWhitespace();
            return type;
        }

        internal void consumeWhitespace()
        {
            _reader.ReadWhile(char.IsWhiteSpace);
        }

        internal TokenType consumeComment()
        {
            if (_reader.AtEnd)
            {
                return setupErrorToken(EOF);
            }
            // A comment runs to the end of the line, or to the end of the template on the last line.
            var value = new StringBuilder();
            _reader.ReadUntil('\n', value);

            Token.Type = TokenType.COMMENT;
            Token.Value = value.ToString().TrimEnd('\r');
            return Token.Type;
        }

        internal bool maybeReadCommentStart()
        {
            return maybeReadText(COMMENT_START.ToString());
        }

        internal bool maybeReadBegin()
        {
            return maybeReadText(BEGIN.ToString());
        }

        internal bool maybeReadEnd()
        {
            return maybeReadText(END);
        }

        internal bool maybeReadClose()
        {
            return maybeReadText(CLOSE.ToString());
        }

        internal bool maybeReadSingleClose()
        {
            var ok = maybeReadText(SINGLE_CLOSE);

            if (!ok)
            {
                var isslash = maybeReadText(FORWARD_SLASH.ToString());
                if (isslash && _inTag)
                    throw new Exception("Single tag closing forward slash detected without closing angle bracket.");

                if (isslash)
                {
                    // put the text back.
                    _reader.PushBack(FORWARD_SLASH);
                }
            }

            return ok;
        }

        internal bool maybeReadText(string text)
        {
            return _reader.TryMatch(text);
        }
    }
}