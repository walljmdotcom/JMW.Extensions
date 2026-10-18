namespace JMW.TextExtraction.Tables;

/// <summary>
/// How to read one kind of table: its layout and its column names. Column positions are not part of the spec,
/// since they shift with the widths of the values in each sample; extraction finds the table again with
/// <see cref="TableDetector"/> and matches its columns to the spec by name.
/// </summary>
/// <param name="Columns">Column names, or <c>Column1</c>, <c>Column2</c>, ... for a table with no header.</param>
public sealed record TableSpec(TableLayout Layout, bool HasHeader, IReadOnlyList<string> Columns)
{
    public static TableSpec From(DetectedTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        return new TableSpec(table.Layout, table.HeaderLine is not null, table.Columns.Select(static c => c.Name).ToList());
    }

    /// <summary>
    /// How well a detected table fits: the share of column names the two have in common (Jaccard similarity),
    /// or for tables with no header, 1 when the column counts agree. Zero when one has a header and the other
    /// does not.
    /// </summary>
    public double Fit(DetectedTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (this.HasHeader != table.HeaderLine is not null)
        {
            return 0;
        }

        if (!this.HasHeader)
        {
            return this.Columns.Count == table.Columns.Count ? 1 : 0;
        }

        HashSet<string> mine = new(this.Columns, StringComparer.OrdinalIgnoreCase);
        HashSet<string> theirs = new(table.Columns.Select(static c => c.Name), StringComparer.OrdinalIgnoreCase);
        return Similarity.Jaccard(mine, theirs);
    }
}

/// <summary>A table read with a spec: which spec, where it sits, and one record per row.</summary>
public sealed record ExtractedTable(TableSpec Spec, int FirstLine, int LastLine, IReadOnlyList<ExtractedRecord> Rows);

public static class TableInference
{
    /// <summary>
    /// The kinds of table in the samples, most common first. Tables are the same kind when their column names
    /// match, or for tables with no header, when their column counts do. With three or more samples, a kind
    /// must turn up in at least two of them.
    /// </summary>
    public static List<TableSpec> Infer(IReadOnlyList<string> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return Infer(samples.Select(static s => TableDetector.Detect(s)).ToList(), samples.Count);
    }

    internal static List<TableSpec> Infer(IReadOnlyList<IReadOnlyList<DetectedTable>> detected, int samples)
    {
        Dictionary<string, List<DetectedTable>> kinds = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> seenIn = new(StringComparer.OrdinalIgnoreCase);
        foreach (IReadOnlyList<DetectedTable> tables in detected)
        {
            HashSet<string> here = new(StringComparer.OrdinalIgnoreCase);
            foreach (DetectedTable table in tables)
            {
                string kind = Kind(table);
                (kinds.TryGetValue(kind, out List<DetectedTable>? list) ? list : kinds[kind] = []).Add(table);
                here.Add(kind);
            }

            foreach (string kind in here)
            {
                seenIn[kind] = seenIn.GetValueOrDefault(kind) + 1;
            }
        }

        int minSamples = samples >= 3 ? 2 : 1;
        return kinds
            .Where(k => seenIn[k.Key] >= minSamples)
            .OrderByDescending(k => seenIn[k.Key])
            .ThenBy(static k => k.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static k =>
            {
                TableLayout layout = k.Value.GroupBy(static t => t.Layout).OrderByDescending(static g => g.Count()).First().Key;
                return TableSpec.From(k.Value[0]) with { Layout = layout };
            })
            .ToList();
    }

    private static string Kind(DetectedTable table) =>
        table.HeaderLine is null
            ? "#" + table.Columns.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : string.Join("\u0001", table.Columns.Select(static c => c.Name));
}

public static class TableExtractor
{
    private const double MinFit = 0.5;

    /// <summary>
    /// Finds the tables in the text and reads each with the spec it fits best. Columns take the spec's name
    /// where the names match regardless of case, and keep their own otherwise. A table that fits no spec is
    /// left out, or with <paramref name="includeUnknown"/>, read with a spec made from the table itself.
    /// </summary>
    public static List<ExtractedTable> Extract(IEnumerable<TableSpec> specs, string text, bool includeUnknown = false) =>
        Extract(specs, TextDocument.Parse(text), includeUnknown);

    public static List<ExtractedTable> Extract(IEnumerable<TableSpec> specs, TextDocument document, bool includeUnknown = false)
    {
        ArgumentNullException.ThrowIfNull(specs);
        ArgumentNullException.ThrowIfNull(document);
        List<TableSpec> known = specs.ToList();
        List<ExtractedTable> result = [];
        foreach (DetectedTable table in TableDetector.Detect(document))
        {
            TableSpec? spec = known.Select(s => (Spec: s, Fit: s.Fit(table))).Where(static x => x.Fit >= MinFit).OrderByDescending(static x => x.Fit).Select(static x => x.Spec).FirstOrDefault();
            if (spec is null && !includeUnknown)
            {
                continue;
            }

            spec ??= TableSpec.From(table);
            Dictionary<string, string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (string column in spec.Columns)
            {
                names.TryAdd(column, column);
            }
            List<ExtractedRecord> rows = [];
            for (int r = 0; r < table.Rows.Count; r++)
            {
                List<ExtractedField> fields = [];
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    string name = names.TryGetValue(table.Columns[c].Name, out string? n) ? n : table.Columns[c].Name;
                    fields.Add(new ExtractedField(name, table.Rows[r][c]));
                }

                rows.Add(new ExtractedRecord(table.RowLines[r].Start, table.RowLines[r].End - 1, fields, []));
            }

            result.Add(new ExtractedTable(spec, table.FirstLine, table.LastLine, rows));
        }

        return result;
    }
}
