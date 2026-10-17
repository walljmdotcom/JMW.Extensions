using JMW.Extensions.String;
using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Parsing.Expressions;

public class Look : IExpression
{
    public const string NAME = "look";
    public const string AHEAD = "a";
    public const string BEHIND = "b";

    public Search Search { get; set; } = new();
    public Search Ahead { get; set; } = new();
    public Search Behind { get; set; } = new();

    public Look(Tag t)
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

        if (t.Properties.TryGetValue(AHEAD, out var aheadTag))
        {
            foreach (var val in (Stack<Tag>)aheadTag.Value)
            {
                if (val.Value.ToString() is null)
                {
                    continue;
                }
                this.Ahead.Query.Add(val.Value.ToString() ?? string.Empty);
            }

            if (aheadTag.Properties.TryGetValue(Search.MODS, out var modsTag))
            {
                this.Search.Mods = modsTag.Value.ToString() ?? string.Empty;
            }
        }

        if (t.Properties.TryGetValue(BEHIND, out var behindTag))
        {
            foreach (var val in (Stack<Tag>)behindTag.Value)
            {
                if (val.Value.ToString() is null)
                {
                    continue;
                }
                this.Behind.Query.Add(val.Value.ToString() ?? string.Empty);
            }

            if (behindTag.Properties.TryGetValue(Search.MODS, out var modsTag))
            {
                this.Search.Mods = modsTag.Value.ToString() ?? string.Empty;
            }
        }
    }

    public Look(List<string> search, string mods, List<string> ahead, string modsa, List<string> behind, string modsb)
    {
        this.Search.Query = search;
        this.Search.Mods = mods;
        this.Ahead.Query = ahead;
        this.Ahead.Mods = modsa;
        this.Behind.Query = behind;
        this.Behind.Mods = modsb;
    }

    public bool Test(string inputValue)
    {
        var text = inputValue;

        var isCaseInsensitive = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE);

        // find case insensitively.
        bool find(string sch, string qry)
        {
            return sch.Contains(qry, isCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        if (!this.Search.Query.Any(sr => find(text, sr)))
        {
            return false;
        }

        if (this.Ahead.Query.Count > 0 && !this.Ahead.Query.Any(sr => find(text, sr)))
        {
            return false;
        }

        if (this.Behind.Query.Count > 0 && !this.Behind.Query.Any(sr => find(text, sr)))
        {
            return false;
        }

        if (this.Behind.Query.Count > 0)
        {
            text = text.ParseToLastIndexOf([.. this.Behind.Query]);
        }

        if (this.Ahead.Query.Count > 0)
        {
            text = text.ParseAfterLastIndexOf_PlusLength([.. this.Ahead.Query]);
        }

        var v = this.Search.Query.Any(sr => find(text, sr));
        return this.Search.Mods.Contains(Constants.NEGATE) ? !v : v;
    }
}