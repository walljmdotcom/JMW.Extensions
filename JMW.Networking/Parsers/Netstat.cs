using JMW.Parsing.Handlers;
using System;
using System.Collections.Generic;
using System.IO;

namespace JMW.Networking.Parsers;

public static class NetStat
{
    public static IEnumerable<IDictionary<string, string>> Parse(TextReader reader)
    {
        // Each table is a title line, a header line, then its rows. Blank lines were dropped while reading.
        foreach (var lines in ExtractTables(reader))
        {
            if (lines.Count < 2)
            {
                continue;
            }

            var body = lines.GetRange(1, lines.Count - 1);
            var positions = Table.DeriveColumnPositions(body);

            for (var i = 1; i < body.Count; i++)
            {
                var row = new Dictionary<string, string>();
                foreach (var position in positions.Positions)
                {
                    row.Add(position.Key, TrimmedSlice(body[i], position.Value.Start, position.Value.Length));
                }
                yield return row;
            }
        }
    }

    // Rows are often shorter than the header, so a column can start or end past the end of the row.
    // A length of -1 means the column runs to the end of the row.
    private static string TrimmedSlice(string row, int start, int length)
    {
        start = Math.Min(Math.Max(start, 0), row.Length);
        var end = length < 0 || start + length > row.Length ? row.Length : start + length;

        while (start < end && char.IsWhiteSpace(row[start]))
        {
            start++;
        }

        while (end > start && char.IsWhiteSpace(row[end - 1]))
        {
            end--;
        }

        return row.Substring(start, end - start);
    }

    private static readonly StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;

    private static IEnumerable<List<string>> ExtractTables(TextReader output)
    {
        var lines = new List<string>();
        string? line;
        while ((line = output.ReadLine()) is not null)
        {
            if (
                   string.IsNullOrWhiteSpace(line)
                || line.StartsWith("netstat", ignoreCase)
                || line.StartsWith("routing", ignoreCase)
            )
            {
                continue;
            }

            if (line.StartsWith("internet", ignoreCase) && lines.Count > 0)
            {
                // new block
                yield return lines;
                lines = [];
            }

            lines.Add(line);
        }

        yield return lines;
    }
}
