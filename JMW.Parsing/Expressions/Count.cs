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
        var comparison = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        // Counts matches left to right, each starting after the last. Where two values match at the same place, the
        // first in the list wins. An empty value would match forever without moving, so it is not counted.
        var count = 0;
        var position = 0;
        while (true)
        {
            var best = -1;
            var bestLength = 0;
            foreach (var value in this.Search.Query)
            {
                if (value.Length == 0)
                    continue;

                var index = inputValue.IndexOf(value, position, comparison);
                if (index >= 0 && (best < 0 || index < best))
                {
                    best = index;
                    bestLength = value.Length;
                }
            }

            if (best < 0)
                break;

            count++;
            position = best + bestLength;
        }

        return this.Rng.Contains(count);
    }
}