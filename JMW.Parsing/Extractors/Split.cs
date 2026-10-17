using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Parsing.Extractors;

public class Split : Base
{
    public const string NAME = "split";

    public override string Parse(string value)
    {
        if (this.Index < 0)
        {
            throw new ArgumentException("Required Attribute Missing: " + INDEX);
        }

        // should we remove the empty entries?
        var splitOptions = this.Search.Mods.Contains(Constants.REMOVE_EMPTY_ENTRIES)
             ? StringSplitOptions.RemoveEmptyEntries
             : StringSplitOptions.None;
        var isCaseInsensitive = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE);

        var splitSeparators = isCaseInsensitive
            ? this.Search.Query.Select(o => o.ToLowerInvariant()).ToArray()
            : [.. this.Search.Query];

        string[] fields = isCaseInsensitive
            ? value.ToLowerInvariant().Split(splitSeparators, splitOptions)
            : value.Split(splitSeparators, splitOptions);

        return fields.Length > this.Index
            ? this.Search.Mods.Contains(Constants.TRIM) // should trim the output?
                ? fields[this.Index].Trim()
                : fields[this.Index]
            : throw new IndexOutOfRangeException();
    }

    public Split(Tag t) : base(t)
    {
    }

    public Split(List<string> search, string mods, int idx) : base(search, mods, -1, idx)
    {
    }
}