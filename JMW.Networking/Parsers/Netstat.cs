using JMW.Parsing.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JMW.Networking.Parsers;

public static class NetStat
{
    public static IEnumerable<IDictionary<string, string>> Parse(TextReader reader)
    {
        // grab tables
        var tables = ExtractTables(reader);

        // parse tables
        foreach (var table in tables)
        {
            var lines = table.Split('\n');
            var body = lines.Skip(1).ToArray();
            if (body.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }
            var positions = Table.DeriveColumnPositions(body);

            foreach (var o in lines.Skip(2))
            {
                if (string.IsNullOrWhiteSpace(o))
                {
                    continue;
                }

                var row = new Dictionary<string, string>();
                foreach (var position in positions.Positions)
                {
                    row.Add(position.Key, Slice(o, position.Value.Start, position.Value.Length).Trim());
                }
                yield return row;
            }
        }
    }

    // Rows are often shorter than the header, so a column can start or end past the end of the row.
    // A length of -1 means the column runs to the end of the row.
    private static string Slice(string row, int start, int length)
    {
        start = Math.Min(Math.Max(start, 0), row.Length);
        return length < 0 || start + length > row.Length
            ? row.Substring(start)
            : row.Substring(start, length);
    }

    private static readonly StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;

    private static IEnumerable<string> ExtractTables(TextReader output)
    {
        var sb = new StringBuilder();
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

            if (line.StartsWith("internet", ignoreCase) && sb.Length > 0)
            {
                // new block
                yield return sb.ToString();
                sb.Clear();
            }

            sb.AppendLine(line);
        }

        yield return sb.ToString();
    }
}
