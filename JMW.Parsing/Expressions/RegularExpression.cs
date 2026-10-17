using JMW.Parsing.Compile;
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

    public override bool Test(string s)
    {
        bool v = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE)
            ? this.Search.Query.Any(sr => Regex.IsMatch(s, sr, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            : this.Search.Query.Any(sr => Regex.IsMatch(s, sr));

        return this.Search.Mods.Contains(Constants.NEGATE) ? !v : v;
    }
}