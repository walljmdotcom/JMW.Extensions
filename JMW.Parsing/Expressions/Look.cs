using JMW.Extensions;
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
                this.Ahead.Mods = modsTag.Value.ToString() ?? string.Empty;
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
                this.Behind.Mods = modsTag.Value.ToString() ?? string.Empty;
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

    /// <summary>
    ///   True when a search value sits after the last ahead value and before the last behind value.
    ///   Each list matches with its own <c>i</c> modifier; <c>n</c> on the search list negates the whole result.
    /// </summary>
    public bool Test(string inputValue)
    {
        var isMatch = this.IsBetween(inputValue);
        return this.Search.Mods.Contains(Constants.NEGATE) ? !isMatch : isMatch;
    }

    private bool IsBetween(string text)
    {
        var searchIgnoresCase = IgnoresCase(this.Search);
        var aheadIgnoresCase = IgnoresCase(this.Ahead);
        var behindIgnoresCase = IgnoresCase(this.Behind);

        if (!ContainsAny(text, this.Search.Query, searchIgnoresCase))
        {
            return false;
        }

        if (this.Ahead.Query.Count > 0 && !ContainsAny(text, this.Ahead.Query, aheadIgnoresCase))
        {
            return false;
        }

        if (this.Behind.Query.Count > 0 && !ContainsAny(text, this.Behind.Query, behindIgnoresCase))
        {
            return false;
        }

        if (this.Behind.Query.Count > 0)
        {
            text = text.ParseToLastIndexOf(behindIgnoresCase, this.Behind.QueryArray());
        }

        if (this.Ahead.Query.Count > 0)
        {
            text = text.ParseAfterLastIndexOf_PlusLength(aheadIgnoresCase, this.Ahead.QueryArray());
        }

        return ContainsAny(text, this.Search.Query, searchIgnoresCase);
    }

    private static bool IgnoresCase(Search search) => search.Mods.Contains(Constants.CASE_INSENSITIVE);

    private static bool ContainsAny(string text, List<string> queries, bool ignoreCase)
    {
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        foreach (var q in queries)
        {
            if (text.Contains(q, comparison))
            {
                return true;
            }
        }

        return false;
    }
}