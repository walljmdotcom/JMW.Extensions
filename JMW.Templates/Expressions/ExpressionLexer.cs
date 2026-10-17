using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace JMW.Templates.Expressions;

internal enum ExpressionTokenKind
{
    Number,
    String,
    Identifier,
    Punctuator,
    End,
}

internal readonly record struct ExpressionToken(ExpressionTokenKind Kind, string Text, double Number, int Position);

internal static class ExpressionLexer
{
    // Longest first, so "===" wins over "==" and "=".
    private static readonly string[] punctuators =
    [
        "===", "!==",
        "==", "!=", "<=", ">=", "&&", "||", "++", "--", "+=", "-=", "*=", "/=", "%=",
        "+", "-", "*", "/", "%", "<", ">", "!", "=", "?", ":", ";", ",", ".", "(", ")",
    ];

    public static List<ExpressionToken> Tokenize(string source)
    {
        var tokens = new List<ExpressionToken>();
        int i = 0;

        while (i < source.Length)
        {
            char c = source[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
            }
            else if (IsDigit(c) || (c == '.' && i + 1 < source.Length && IsDigit(source[i + 1])))
            {
                tokens.Add(ReadNumber(source, ref i));
            }
            else if (c is '\'' or '"')
            {
                tokens.Add(ReadString(source, ref i));
            }
            else if (IsIdentifierStart(c))
            {
                int start = i;
                while (i < source.Length && IsIdentifierPart(source[i])) i++;
                tokens.Add(new ExpressionToken(ExpressionTokenKind.Identifier, source.Substring(start, i - start), 0, start));
            }
            else
            {
                tokens.Add(ReadPunctuator(source, ref i));
            }
        }

        tokens.Add(new ExpressionToken(ExpressionTokenKind.End, string.Empty, 0, source.Length));
        return tokens;
    }

    private static ExpressionToken ReadNumber(string source, ref int i)
    {
        int start = i;
        while (i < source.Length && IsDigit(source[i])) i++;

        if (i < source.Length && source[i] == '.')
        {
            i++;
            while (i < source.Length && IsDigit(source[i])) i++;
        }

        if (i < source.Length && source[i] is 'e' or 'E')
        {
            int exponentStart = i;
            i++;
            if (i < source.Length && source[i] is '+' or '-') i++;

            if (i >= source.Length || !IsDigit(source[i]))
            {
                throw new ExpressionException($"Invalid number at position {exponentStart}.");
            }

            while (i < source.Length && IsDigit(source[i])) i++;
        }

        if (i < source.Length && IsIdentifierStart(source[i]))
        {
            throw new ExpressionException($"Invalid number at position {start}.");
        }

        string text = source.Substring(start, i - start);
        double value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        return new ExpressionToken(ExpressionTokenKind.Number, text, value, start);
    }

    private static ExpressionToken ReadString(string source, ref int i)
    {
        int start = i;
        char quote = source[i++];
        var sb = new StringBuilder();

        while (true)
        {
            if (i >= source.Length)
            {
                throw new ExpressionException($"Unterminated string starting at position {start}.");
            }

            char c = source[i++];
            if (c == quote) break;

            if (c != '\\')
            {
                sb.Append(c);
                continue;
            }

            if (i >= source.Length)
            {
                throw new ExpressionException($"Unterminated string starting at position {start}.");
            }

            char escaped = source[i++];
            switch (escaped)
            {
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'v': sb.Append('\v'); break;
                case '0': sb.Append('\0'); break;
                case 'u':
                    if (i + 4 > source.Length
                        || !int.TryParse(source.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                    {
                        throw new ExpressionException($"Invalid unicode escape at position {i - 2}.");
                    }

                    sb.Append((char)code);
                    i += 4;
                    break;
                default:
                    // \\, \', \" and any other escaped character stand for themselves.
                    sb.Append(escaped);
                    break;
            }
        }

        return new ExpressionToken(ExpressionTokenKind.String, sb.ToString(), 0, start);
    }

    private static ExpressionToken ReadPunctuator(string source, ref int i)
    {
        foreach (string punctuator in punctuators)
        {
            if (string.CompareOrdinal(source, i, punctuator, 0, punctuator.Length) == 0)
            {
                var token = new ExpressionToken(ExpressionTokenKind.Punctuator, punctuator, 0, i);
                i += punctuator.Length;
                return token;
            }
        }

        throw new ExpressionException($"Unexpected character '{source[i]}' at position {i}.");
    }

    private static bool IsDigit(char c) => c is >= '0' and <= '9';

    private static bool IsIdentifierStart(char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or '$';

    private static bool IsIdentifierPart(char c) => IsIdentifierStart(c) || IsDigit(c);
}
