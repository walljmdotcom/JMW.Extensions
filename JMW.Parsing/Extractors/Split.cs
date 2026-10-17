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

        string field;
        if (isCaseInsensitive && this.Search.Query.Any(static s => s.Length > 0))
        {
            if (!TryGetFieldIgnoreCase(value, this.Search.Query, splitOptions == StringSplitOptions.RemoveEmptyEntries, this.Index, out field))
                throw new IndexOutOfRangeException();
        }
        else
        {
            string[] fields = value.Split(this.QueryArray(), splitOptions);
            field = fields.Length > this.Index ? fields[this.Index] : throw new IndexOutOfRangeException();
        }

        return this.Search.Mods.Contains(Constants.TRIM) // should trim the output?
            ? field.Trim()
            : field;
    }

    // Finds the field string.Split would return at the index, matching separators in any case. Splitting a lowercased
    // copy instead would return lowercased text.
    private static bool TryGetFieldIgnoreCase(string value, List<string> separators, bool remove_empty, int index, out string field)
    {
        var found = 0;
        var start = 0;
        var position = 0;
        while (true)
        {
            var separatorLength = position < value.Length ? SeparatorLengthAt(value, position, separators) : 0;
            if (separatorLength == 0 && position < value.Length)
            {
                position++;
                continue;
            }

            if (!remove_empty || position > start)
            {
                if (found == index)
                {
                    field = value.Substring(start, position - start);
                    return true;
                }

                found++;
            }

            if (position == value.Length)
            {
                field = string.Empty;
                return false;
            }

            position += separatorLength;
            start = position;
        }
    }

    // Like string.Split, the first separator in the list that matches wins.
    private static int SeparatorLengthAt(string value, int position, List<string> separators)
    {
        foreach (var separator in separators)
        {
            if (
                separator.Length > 0
                && separator.Length <= value.Length - position
                && string.Compare(value, position, separator, 0, separator.Length, StringComparison.OrdinalIgnoreCase) == 0
            )
            {
                return separator.Length;
            }
        }

        return 0;
    }

    public Split(Tag t) : base(t)
    {
    }

    public Split(List<string> search, string mods, int idx) : base(search, mods, -1, idx)
    {
    }
}