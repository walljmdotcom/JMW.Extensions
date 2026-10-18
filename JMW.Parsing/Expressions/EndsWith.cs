using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;

namespace JMW.Parsing.Expressions;

public class EndsWith : Base
{
    public const string NAME1 = "endswith";
    public const string NAME2 = "ends";

    public EndsWith(Tag t) : base(t)
    {
    }

    public EndsWith(List<string> search, string mods) : base(search, mods)
    {
    }

    public override bool Test(string s)
    {
        return base.Test(s, static (str, sch) => str.EndsWith(sch, StringComparison.Ordinal));
    }
}