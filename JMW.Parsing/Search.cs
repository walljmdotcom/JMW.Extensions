using System.Collections.Generic;
using System.Linq;

namespace JMW.Parsing;

public class Search
{
    public const string MODS = "m";
    public const string SEARCH = "s";

    public List<string> Query { get; set; } = [];
    public string Mods { get; set; } = string.Empty;

    private QueryCopy? array;
    private QueryCopy? lowered;

    /// <summary>
    /// <see cref="Query"/> as an array, copied again only when the list or its items have changed.
    /// </summary>
    internal string[] QueryArray()
    {
        var query = this.Query;
        var current = this.array;
        if (current is null || !current.CopiedFrom(query))
        {
            current = new QueryCopy(query, [.. query]);
            this.array = current;
        }

        return current.Items;
    }

    /// <summary>
    /// <see cref="Query"/> lowercased with the invariant culture, made again only when the list or its items have
    /// changed.
    /// </summary>
    internal string[] LoweredQueryArray()
    {
        var query = this.Query;
        var current = this.lowered;
        if (current is null || !current.CopiedFrom(query))
        {
            current = new QueryCopy(query, query.Select(static q => q.ToLowerInvariant()).ToArray());
            this.lowered = current;
        }

        return current.Items;
    }

    private sealed class QueryCopy
    {
        private readonly List<string> source;
        private readonly string[] sourceItems;

        public QueryCopy(List<string> source, string[] items)
        {
            this.source = source;
            this.sourceItems = [.. source];
            this.Items = items;
        }

        public string[] Items { get; }

        // The list is public and settable, so the copy is checked against it item by item.
        public bool CopiedFrom(List<string> query)
        {
            if (!ReferenceEquals(query, this.source) || query.Count != this.sourceItems.Length)
            {
                return false;
            }

            for (var i = 0; i < this.sourceItems.Length; i++)
            {
                if (!ReferenceEquals(query[i], this.sourceItems[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
