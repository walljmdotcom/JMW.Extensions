using JMW.Extensions;
using JMW.Parsing.Compile;
using System.Collections.Generic;

namespace JMW.Parsing.Extractors;

public class After : Base
{
    public const string NAME = "after";

    public override string Parse(string s)
    {
        var isCaseInsensitive = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE); // case insensitive
        var shouldTrim = this.Search.Mods.Contains(Constants.TRIM); // trim the output
        var excludeSearchItem = this.Search.Mods.Contains(Constants.WITH_VALUE);

        var value = excludeSearchItem switch
        {
            true when this.Quantifier > -1
                => s.ParseAfterDesignatedIndexOf_PlusLength(this.Quantifier, isCaseInsensitive, [.. this.Search.Query]),
            true when this.Search.Mods.Contains(Constants.LAST)
                => s.ParseAfterLastIndexOf_PlusLength(isCaseInsensitive, [.. this.Search.Query]),
            true => s.ParseAfterIndexOf_PlusLength(isCaseInsensitive, [.. this.Search.Query]),
            _ => s.ParseAfterIndexOf(isCaseInsensitive, [.. this.Search.Query]),
        };

        return shouldTrim ? value.Trim() : value;
    }

    public After(Tag t) : base(t)
    {
    }

    public After(List<string> search, string mods, int q) : base(search, mods, q, -1)
    {
    }
}