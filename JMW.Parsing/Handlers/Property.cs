using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JMW.Parsing.Handlers;

public class Property : IProperty
{
    public const string NAME = "prop";
    public const string A_NAME = "name";
    public const string A_PARSERS = "parsers";
    public const string A_LINE = "line";
    public const string A_SPLIT = "split";

    public string Name { get; set; }
    public Stack<Extractors.Base> Extractors { get; set; } = new Stack<Extractors.Base>();
    public Expressions.IExpression? Line { get; set; }
    public string? Split { get; set; }

    public Property(Tag token)
    {
        if (token.Properties.TryGetValue(A_NAME, out var nameTag))
        {
            this.Name = nameTag.Value?.ToString() ?? string.Empty;
        }
        else
        {
            throw new ParseException("Required Attribute Missing: " + A_NAME);
        }

        if (token.Properties.TryGetValue(A_PARSERS, out var parsersTag))
        {
            var parsers = (Stack<Tag>)parsersTag.Value;
            foreach (var p in parsers)
            {
                this.Extractors.Push(Parsing.Extractors.Base.ToExtractor(p));
            }
        }
        else
        {
            throw new ParseException("Required Attribute Missing: " + A_PARSERS);
        }

        if (token.Properties.TryGetValue(A_LINE, out var lineTag))
        {
            this.Line = Expressions.Base.ToExpression(lineTag);
        }

        if (token.Properties.TryGetValue(A_SPLIT, out var splitTag))
        {
            this.Split = splitTag.Value?.ToString() ?? string.Empty;
        }
    }

    public object Parse(TextReader reader)
    {
        if (this.Extractors.Count > 0)
        {
            if (this.Split is not null)
            {
                var split = this.Split;
                var sb = new StringBuilder();
                var c = 0;
                while (c != -1)
                {
                    sb.Clear();
                    string line;
                    while (true)
                    {
                        if ((c = reader.Read()) == -1)
                        {
                            line = sb.ToString();
                            break;
                        }

                        sb.Append((char)c);
                        if (EndsWith(sb, split))
                        {
                            line = sb.ToString(0, sb.Length - split.Length);
                            break;
                        }
                    }

                    if (this.Line is not null && !this.Line.Test(line))
                    {
                        continue;
                    }

                    return ExtractText(line);
                }
            }
            else
            {
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (this.Line is not null && !this.Line.Test(line))
                    {
                        continue;
                    }

                    return ExtractText(line);
                }
            }
        }

        return string.Empty;
    }

    private static bool EndsWith(StringBuilder sb, string value)
    {
        if (sb.Length < value.Length)
        {
            return false;
        }

        var offset = sb.Length - value.Length;
        for (var i = 0; i < value.Length; i++)
        {
            if (sb[offset + i] != value[i])
            {
                return false;
            }
        }

        return true;
    }

    private string ExtractText(string line)
    {
        var r = line;
        foreach (var parser in this.Extractors)
        {
            r = parser.Parse(r);
        }

        return r;
    }
}