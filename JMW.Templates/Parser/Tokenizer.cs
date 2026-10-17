using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JMW.IO;

// Internals are visible to the Test assemblies
[assembly: InternalsVisibleTo("JMW.Tests")]

namespace JMW.Template
{
    public class Tokenizer
    {
        internal LineTrackingReader _reader;
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

        internal bool _inTag = false;
        internal bool _inParam = false;
        internal bool _eof = false;

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
            _reader = new LineTrackingReader(reader);
            Token = new Token();
        }

        public TokenType Next()
        {
            NEXT_START:
            try
            {
                if (_eof)
                {
                    return setupErrorToken(EOF);
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
            var c = _reader.Read();
            var l = -1;
            while (c != -1 && c != BEGIN && !isComment(tb, c))
            {
                // This is the unicode invalid character. If we encounter this it means we parsed the
                // template with an invalid encoding. Or the template was stored with an invalid
                // encoding.
                if (c == '\uffff')
                {
                    return setupErrorToken(INVALID_UNICODE_ERROR);
                }
                consumed = true;
                if (l == ESCAPE && c == COMMENT_START)
                {
                    // you need to replace the escape with the actual char
                    tb[tb.Length - 1] = (char)c;
                }
                else
                    tb.Append((char)c);
                l = c;
                c = _reader.Read();
            }
            if (c != -1) // NOTE(jeremy): -1 when cast to a char is an invalid unicode char. Don't push it back!
            {
                _reader.PushBack((char)c);
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
            var c = _reader.Read();
            switch (c)
            {
                // this function only gets called when its reading a tag name, a parameter name, or a param value.
                //  that being the case, if you hit the end of the file, then that's a syntax error, because you
                //  would always want to find a closing tag marker.
                case -1:
                    return setupErrorToken("Invalid tag. Did you forget to add a " + CLOSE + "?", Line, Column);

                case '"':
                case '\'':
                    return consumeUntil(c, TokenType.PARAMVALUE);
            }

            _reader.PushBack((char)c);
            return consumeIdentifier(TokenType.PARAMVALUE);
        }

        internal TokenType consumeUntil(int qry, TokenType type)
        {
            Token.Type = type;
            var c = _reader.Read();
            var value = new List<char>();
            while (c != qry && c > -1)
            {
                value.Add((char)c);
                c = _reader.Read();
            }
            if (c == -1)
            {
                return setupErrorToken("Unexpected end of template.  Did you forget to add a " + (char)qry + "?", Line, Column);
            }
            
            Token.Value = new string(value.ToArray());
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
                return setupErrorToken("Invalid tag. Did you forget to add a " + CLOSE + "?", Line, Column - 2);
            }
            _inTag = true;
            return consumeIdentifier(type);
        }

        internal TokenType consumeIdentifier(TokenType type)
        {
            var identifier = new List<char>();
            var c = _reader.Read();
            while (c != -1 && c != FORWARD_SLASH && c != CLOSE && c != BEGIN && c != PARAM_ASSIGN && !char.IsWhiteSpace((char)c))
            {
                identifier.Add((char)c);
                c = _reader.Read();
            }
            if (c == -1)
            {
                // this function only gets called when its reading a tag name, a parameter name, or a param value.
                //  that being the case, if you hit the end of the file, then that's a syntax error, because you
                //  would always want to find a closing tag marker.
                return setupErrorToken("Invalid tag. Did you forget to add a " + CLOSE + "?", Line, Column);
            }

            _reader.PushBack((char)c);
            Token.Type = type;
            Token.Value = new string(identifier.ToArray());
            consumeWhitespace();
            return type;
        }

        internal void consumeWhitespace()
        {
            var c = _reader.Read();
            while (c != -1 && char.IsWhiteSpace((char)c))
            {
                c = _reader.Read();
            }
            if (c == -1)
            {
                _eof = true;
            }
            else
            {
                _reader.PushBack((char)c);
            }
        }

        internal TokenType consumeComment()
        {
            var c = _reader.Read();
            if (c == -1)
            {
                _eof = true;
                return setupErrorToken(EOF);
            }
            // A comment runs to the end of the line, or to the end of the template on the last line.
            var value = new StringBuilder();
            while (c != -1 && c != '\n')
            {
                value.Append((char)c);
                c = _reader.Read();
            }
            if (c == -1)
            {
                _eof = true;
            }

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
            readText(text, out var buf, out var n);
            var ok = (n == text.Length && new string(buf) == text);
            if (!ok)
            {
                _reader.PushBack(buf.Take(n).ToArray());
            }
            return ok;
        }

        private void readText(string text, out char[] buf, out int n)
        {
            buf = new char[text.Length];
            n = _reader.Read(buf);
            if (n == 0)
            {
                _eof = true;
            }
        }
    }
}