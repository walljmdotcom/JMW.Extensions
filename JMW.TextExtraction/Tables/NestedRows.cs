namespace JMW.TextExtraction.Tables;

/// <summary>
/// Tables whose rows come in two kinds: an outer row, and indented rows below it named by an indented second
/// header line, as Junos <c>show route instance</c> prints each instance over its routing tables:
/// <code>
/// Instance             Type
///          Primary RIB                                     Active/holddown/hidden
/// master               forwarding
///          inet.0                                          38/0/0
/// </code>
/// Each indented row becomes a row of its own that carries its outer row's cells, so the table above reads as
/// Instance, Type, Primary RIB, and Active/holddown/hidden. Blank lines between groups do not end the table.
/// </summary>
internal static class NestedRows
{
    private const int MinOuterRows = 2;

    public static List<DetectedTable> Find(IReadOnlyList<string> lines)
    {
        List<DetectedTable> tables = [];
        for (int i = 0; i + 1 < lines.Count; i++)
        {
            if (TryAt(lines, i) is { } table)
            {
                tables.Add(table);
                i = table.LastLine;
            }
        }

        return tables;
    }

    private static DetectedTable? TryAt(IReadOnlyList<string> lines, int h)
    {
        string outerHeader = lines[h];
        string innerHeader = lines[h + 1];
        int outerIndent = Lines.Indent(outerHeader);
        int innerIndent = Lines.Indent(innerHeader);
        if (Lines.Classify(outerHeader) != LineKind.Content || Lines.Classify(innerHeader) != LineKind.Content
            || innerIndent <= outerIndent || !TableDetector.IsHeaderLike(outerHeader)
            || !TableDetector.IsHeaderLike(innerHeader))
        {
            return null;
        }

        int first = h + 2;
        if (first < lines.Count && Lines.Classify(lines[first]) == LineKind.Rule)
        {
            first++;
        }

        // Outer rows sit left of the inner header's indent and inner rows at or past it. A rule, a line that
        // reads as "key: value", or a line too shallow ends the table.
        List<(int Line, List<int> Inner)> groups = [];
        int last = -1;
        for (int i = first; i < lines.Count; i++)
        {
            LineKind kind = Lines.Classify(lines[i]);
            if (kind == LineKind.Blank)
            {
                continue;
            }

            int indent = Lines.Indent(lines[i]);
            if (kind != LineKind.Content || indent < outerIndent
                || LineKeys.IsKeyValue(Tokenizer.Tokenize(lines[i])))
            {
                break;
            }

            if (indent < innerIndent)
            {
                groups.Add((i, []));
            }
            else if (groups.Count > 0)
            {
                groups[^1].Inner.Add(i);
            }
            else
            {
                break;
            }

            last = i;
        }

        if (groups.Count < MinOuterRows || !groups.Exists(static g => g.Inner.Count > 0))
        {
            return null;
        }

        int[] outerCuts = Cuts(lines, groups.Select(static g => g.Line), outerHeader, out List<string> outerNames);
        int[] innerCuts = Cuts(lines, groups.SelectMany(static g => g.Inner), innerHeader, out List<string> innerNames);
        if (outerNames.Count < 2 || innerNames.Count < 1)
        {
            return null;
        }

        List<DetectedColumn> columns = [];
        foreach ((string name, int cut) in outerNames.Zip(outerCuts).Concat(innerNames.Zip(innerCuts)))
        {
            string unique = name;
            for (int n = 2; columns.Exists(c => string.Equals(c.Name, unique, StringComparison.OrdinalIgnoreCase)); n++)
            {
                unique = name + " " + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            columns.Add(new DetectedColumn(unique, cut, cut));
        }

        List<IReadOnlyList<string>> rows = [];
        List<Span> rowLines = [];
        string[] noInner = new string[innerNames.Count];
        Array.Fill(noInner, "");
        foreach ((int line, List<int> inner) in groups)
        {
            string[] outer = Cells.Slice(lines[line], outerCuts);
            if (inner.Count == 0)
            {
                rows.Add([.. outer, .. noInner]);
                rowLines.Add(new Span(line, line + 1));
            }

            foreach (int i in inner)
            {
                rows.Add([.. outer, .. Cells.Slice(lines[i], innerCuts)]);
                rowLines.Add(new Span(i, i + 1));
            }
        }

        return new DetectedTable(TableLayout.Aligned, h, h, last, columns, rows, rowLines);
    }

    private static int[] Cuts(IReadOnlyList<string> lines, IEnumerable<int> rows, string header, out List<string> names)
    {
        Occupancy occupancy = new();
        foreach (int row in rows)
        {
            occupancy.Add(lines[row]);
        }

        List<ColumnGroup> groups = HeaderAlignment.Align(occupancy.Spans(), Lines.Words(header));
        names = groups.Select(g => string.Join(" ", g.Words.Select(w => w.Of(header)))).ToList();
        int[] cuts = new int[groups.Count];
        for (int g = 1; g < groups.Count; g++)
        {
            cuts[g] = groups[g].Data.Start;
        }

        return cuts;
    }
}
