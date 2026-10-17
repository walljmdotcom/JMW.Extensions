using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JMW.Parsing.Expressions;

public class RegularExpression : Base
{
    public const string NAME1 = "regex";
    public const string NAME2 = "reg";

    public RegularExpression(Tag t) : base(t)
    {
    }

    public RegularExpression(List<string> search, string mods) : base(search, mods)
    {
    }

    // Patterns come from instruction text, so a pathological one must fail with RegexMatchTimeoutException
    // rather than backtrack for minutes.
    private static readonly TimeSpan matchTimeout = TimeSpan.FromSeconds(2);

    public override bool Test(string s)
    {
        bool v = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE)
            ? this.Search.Query.Any(sr => Regex.IsMatch(s, sr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeout))
            : this.Search.Query.Any(sr => Regex.IsMatch(s, sr, RegexOptions.None, matchTimeout));

        return this.Search.Mods.Contains(Constants.NEGATE) ? !v : v;
    }
}