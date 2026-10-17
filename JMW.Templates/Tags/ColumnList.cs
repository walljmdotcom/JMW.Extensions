using JMW.Extensions;
using System;
using System.Collections.Generic;

namespace JMW.Templates.Tags;

/// <summary>
/// Reads the values named by an <c>&lt;if&gt;</c> or <c>&lt;transform&gt;</c> column list, such as
/// <c>"t:name, t:ip"</c>, for the current row.
/// </summary>
internal sealed class ColumnList
{
    // A body runs once per row, so each list is parsed once and reused. The limit keeps lists built from variable
    // values from growing the cache without end.
    private const int CacheLimit = 256;

    private static readonly string[] separator = [","];

    private readonly Dictionary<string, ValueRetriever> retrievers;
    private readonly Dictionary<string, ColumnReference[]> parsed = new(StringComparer.Ordinal);

    private readonly record struct ColumnReference(ValueRetriever Retriever, string Column);

    public ColumnList(Dictionary<string, ValueRetriever> retrievers)
    {
        this.retrievers = retrievers;
    }

    public List<string> ReadValues(string column_list)
    {
        if (!parsed.TryGetValue(column_list, out var columns))
        {
            columns = Parse(column_list);
            if (parsed.Count >= CacheLimit)
                parsed.Clear();

            parsed[column_list] = columns;
        }

        var values = new List<string>(columns.Length);
        foreach (var column in columns)
            values.Add(column.Retriever(column.Column));

        return values;
    }

    private ColumnReference[] Parse(string column_list)
    {
        var columns = new List<ColumnReference>();
        foreach (var column in column_list.Split(separator, StringSplitOptions.None))
        {
            if (string.IsNullOrWhiteSpace(column))
                continue;

            var retriever = column.ParseToLastIndexOf(":").Trim().ToLowerInvariant();
            var col = column.ParseAfterLastIndexOf_PlusLength(":").Trim().ToLowerInvariant();
            columns.Add(new ColumnReference(retrievers[retriever], col));
        }

        return [.. columns];
    }
}
