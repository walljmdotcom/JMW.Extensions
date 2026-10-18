using JMW.Extensions;
using JMW.Parsing.Compile;
using JMW.Parsing.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Parsing.Extractors;

public class Column : Base
{
    public const string NAME = "col";

    public List<ColumnPosition>? Positions { get; set; }

    public override string Parse(string s)
    {
        if (this.Positions is not null && this.Positions.Count <= this.Index)
        {
            throw new ArgumentException($"Provided index ({this.Index}) exceeded the number of columns ({this.Positions.Count}):{this.Positions.Select(c => c.Name).ToDelimitedString(",")}");
        }

        if (this.Positions is not null)
        {
            return this.Positions[this.Index].GetColumnValue(s);
        }

        return FieldAt(s, this.Index);
    }

    // The field s.Split(' ', RemoveEmptyEntries) would return at the index, without splitting the rest of the line.
    private static string FieldAt(string s, int index)
    {
        var found = 0;
        var i = 0;
        while (i < s.Length)
        {
            while (i < s.Length && s[i] == ' ')
                i++;

            if (i == s.Length)
                break;

            var start = i;
            while (i < s.Length && s[i] != ' ')
                i++;

            if (found == index)
                return s.Substring(start, i - start);

            found++;
        }

        throw new IndexOutOfRangeException();
    }

    public Column(Tag t) : base(t)
    {
    }

    public Column(int idx) : base(null, "", -1, idx)
    {
    }
}