using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;

namespace JMW.Parsing.Expressions;

public class StartsWith : Base
{
    public const string NAME1 = "startswith";
    public const string NAME2 = "starts";

    public StartsWith(Tag t) : base(t)
    {
    }

    public StartsWith(List<string> search, string mods) : base(search, mods)
    {
    }

    public override bool Test(string s)
    {
        return base.Test(s, static (str, sch) => str.StartsWith(sch, StringComparison.Ordinal));
    }
}