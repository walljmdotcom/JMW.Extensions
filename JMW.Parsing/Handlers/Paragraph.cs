using JMW.Extensions.Enumerable;
using JMW.Parsing.Compile;
using JMW.Parsing.Expressions;
using JMW.Parsing.IO;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JMW.Parsing.Handlers;

public class Paragraph : Base, IParser, IProperty
{
    public const string NAME = "para";

    public const string A_INCLUDE = "include";
    public const string A_START = "start";
    public const string A_STOP = "stop";
    public const string A_PROPS = "props";

    public string Name { get; } = NAME;

    private readonly Include include = new(null, null);
    private readonly IExpression? stop = null;
    private readonly IExpression? start = null;

    public Paragraph(Tag token)
    {
        if (!token.Properties.TryGetValue(A_START, out var startTag))
        {
            throw new ParseException("Missing property: " + A_START);
        }
        if (!token.Properties.TryGetValue(A_PROPS, out var propsTag))
        {
            throw new ParseException("Missing property: " + A_PROPS);
        }

        if (token.Properties.TryGetValue(A_INCLUDE, out var includeTag))
        {
            this.include = new Include(includeTag);
        }

        if (token.Properties.TryGetValue(A_STOP, out var stopTag))
        {
            this.stop = Expressions.Base.ToExpression(stopTag);
        }

        // find first line that matches.
        this.start = Expressions.Base.ToExpression(startTag);

        var props = (Stack<Tag>)propsTag.Value;
        foreach (var p in props)
        {
            if (p.Name == NAME)
                base.Props.Add(new Paragraph(p));
            else
                base.Props.Add(new Property(p));
        }
    }

    public override IEnumerable<object[]> Parse(StreamReader? reader)
    {
        if (reader is null)
            yield break;

        var paras = this.GetParagraphs(reader, this.start, this.stop);

        // parse the paragraphs
        foreach (var lines in paras)
        {
            var record = new List<object>();

            if (lines.Count == 0 || this.start is null || !this.start.Test(lines.First()))
                continue;

            // process the properties
            var line = lines.ToDelimitedString("\n");
            foreach (var p in this.Props)
            {
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(line));
                using var sr = new StreamReader(ms);
                record.Add(p.Parse(sr));
            }
            yield return record.ToArray();
        }
    }

    public IEnumerable<List<string>> GetParagraphs(StreamReader reader, IExpression? paraStart, IExpression? paraStop)
    {
        var sr = new SectionReader(reader, this.include);

        var paragraph = new List<string>();
        var started = false;
        string? line;
        while ((line = sr.ReadLine()) is not null)
        {
            if (
                   paragraph.Count > 0
                && (
                       (paraStop is not null && paraStop.Test(line))
                    || (paraStart is not null && paraStart.Test(line))
                )
            )
            {
                yield return paragraph;
                paragraph = [];
            }

            if (!started && this.start is not null && !this.start.Test(line))
                continue;

            started = true;

            if (this.stop is not null && this.stop.Test(line))
            {
                started = false;
                continue;
            }

            paragraph.Add(line);
        }

        // return the last paragraph.
        yield return paragraph;
    }

    object IProperty.Parse(StreamReader reader)
    {
        return this.Parse(reader);
    }
}