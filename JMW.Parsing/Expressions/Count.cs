using JMW.Collections;
using JMW.Extensions;
using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Parsing.Expressions;

public class Count : IExpression
{
    public const string NAME = "count";
    public const string RNG = "rng";

    public Search Search { get; set; } = new();
    public IntegerRangeCollection Rng { get; set; }

    public Count(Tag t)
    {
        if (t.Properties.TryGetValue(Search.SEARCH, out var searchTag))
        {
            foreach (var val in (Stack<Tag>)searchTag.Value)
            {
                if (val.Value.ToString() is null)
                {
                    continue;
                }
                this.Search.Query.Add(val.Value.ToString() ?? string.Empty);
            }

            if (searchTag.Properties.TryGetValue(Search.MODS, out var modsTag))
            {
                this.Search.Mods = modsTag.Value.ToString() ?? string.Empty;
            }
        }
        else
        {
            throw new ArgumentException("Required Property Missing: " + Search.SEARCH);
        }

        if (t.Properties.TryGetValue(RNG, out var rngTag))
        {
            this.Rng = new IntegerRangeCollection(rngTag.Value.ToString());
        }
        else
        {
            throw new ArgumentException("Required Property Missing: " + RNG);
        }
    }

    public Count(List<string> search, IntegerRangeCollection rng, string mods)
    {
        this.Rng = rng;
        this.Search.Query = search;
        this.Search.Mods = mods;
    }

    public bool Test(string inputValue)
    {
        var caseInsensitive = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE);
        var compareOptions = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var queryArray = this.Search.Query.ToArray();

        var working = inputValue;
        var c = 0;
        while (this.Search.Query.Any(sr => working.Contains(sr, compareOptions)))
        {
            c++;
            working = working.ParseAfterIndexOf_PlusLength(caseInsensitive, queryArray);
        }
        return this.Rng.Contains(c);
    }
}