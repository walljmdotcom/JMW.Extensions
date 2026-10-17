using JMW.Extensions;
using JMW.Parsing.Compile;
using JMW.Parsing.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Parsing.Extractors;

public class Column : Base
{
    private static readonly char[] separator = [' '];

    public const string NAME = "col";

    public List<ColumnPosition>? Positions { get; set; }

    public override string Parse(string s)
    {
        if (this.Positions is not null && this.Positions.Count < this.Index)
        {
            throw new ArgumentException($"Provided index ({this.Index}) exceeded the number of columns ({this.Positions.Count}):{this.Positions.Select(c => c.Name).ToDelimitedString(",")}");
        }

        if (this.Positions is not null)
        {
            return this.Positions[this.Index].GetColumnValue(s);
        }

        // treat it like a split.
        return s.Split(separator, StringSplitOptions.RemoveEmptyEntries)[this.Index];
    }

    public Column(Tag t) : base(t)
    {
    }

    public Column(int idx) : base(null, "", -1, idx)
    {
    }
}