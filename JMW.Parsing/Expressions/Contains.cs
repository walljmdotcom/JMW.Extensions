using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;

namespace JMW.Parsing.Expressions;

public class Contains : Base
{
    public const string NAME = "contains";

    public Contains(Tag t) : base(t)
    {
    }

    public Contains(List<string> search, string mods) : base(search, mods)
    {
    }

    public override bool Test(string s)
    {
        return this.Search.Mods.Contains(Constants.CASE_INSENSITIVE)
            ? base.Test(s, static (str, sch) => str.Contains(sch, StringComparison.OrdinalIgnoreCase))
            : base.Test(s, static (str, sch) => str.Contains(sch, StringComparison.Ordinal));
    }
}