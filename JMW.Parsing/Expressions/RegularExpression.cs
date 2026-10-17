using JMW.Parsing.Compile;
using System;
using System.Collections.Generic;
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

    // Regex's static cache holds 15 patterns, so with more in use the static IsMatch parses them again on every
    // call. Each expression keeps its own, built on first use and replaced when the query or options change.
    private Patterns patterns = new([], RegexOptions.None);

    public override bool Test(string s)
    {
        var options = this.Search.Mods.Contains(Constants.CASE_INSENSITIVE)
            ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant
            : RegexOptions.None;

        var query = this.Search.Query;
        var current = this.patterns;
        if (!current.Matches(query, options))
        {
            current = new Patterns([.. query], options);
            this.patterns = current;
        }

        var v = false;
        for (var i = 0; i < current.Sources.Length; i++)
        {
            // Built on first use, so a bad pattern after a matching one still never throws.
            var regex = current.Regexes[i] ??= new Regex(current.Sources[i], options, matchTimeout);
            if (regex.IsMatch(s))
            {
                v = true;
                break;
            }
        }

        return this.Search.Mods.Contains(Constants.NEGATE) ? !v : v;
    }

    private sealed class Patterns
    {
        private readonly RegexOptions options;

        public Patterns(string[] sources, RegexOptions options)
        {
            this.Sources = sources;
            this.Regexes = new Regex?[sources.Length];
            this.options = options;
        }

        public string[] Sources { get; }
        public Regex?[] Regexes { get; }

        public bool Matches(List<string> query, RegexOptions options)
        {
            if (options != this.options || query.Count != this.Sources.Length)
            {
                return false;
            }

            for (var i = 0; i < this.Sources.Length; i++)
            {
                if (!string.Equals(query[i], this.Sources[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
