namespace JMW.TextExtraction.Tables;

/// <summary>
/// A column found by <see cref="TableDetector"/>. <see cref="Start"/> is the leftmost position the header or
/// any row uses. <see cref="Cut"/> is where a row's cell begins; the cell runs to the next column's cut.
/// </summary>
public sealed record DetectedColumn(string Name, int Start, int Cut);

public enum TableLayout
{
    /// <summary>Cells sit between gaps that are blank in every row.</summary>
    Aligned,

    /// <summary>A dashed rule under the header marks each column's extent.</summary>
    Ruled,

    /// <summary>
    /// Cells are whitespace-separated words, one per header word, and the last column takes the rest of the
    /// line. For output such as <c>ps</c> that drifts out of alignment when a value is wider than its column.
    /// </summary>
    Words,
}

/// <param name="HeaderLine">Zero-based index of the header line, or null when the table has none.</param>
/// <param name="FirstLine">Zero-based index of the first line of the table, header included.</param>
/// <param name="LastLine">Zero-based index of the last line of the table.</param>
/// <param name="RowLines">The lines each row came from, as half-open ranges, one per row.</param>
public sealed record DetectedTable(
    TableLayout Layout,
    int? HeaderLine,
    int FirstLine,
    int LastLine,
    IReadOnlyList<DetectedColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<Span> RowLines
);

/// <summary>
/// Finds whitespace-aligned tables in text with no description of them.
/// </summary>
/// <remarks>
/// Gaps come from the rows, not the header: a position blank in every row is a gap
/// (<see cref="Occupancy"/>). The header then names and joins the columns (<see cref="HeaderAlignment"/>).
/// A table ends where the next line would close most of its gaps at once, as a footer or prose does.
/// </remarks>
public static class TableDetector
{
    private const int HeaderCandidates = 3;

    public static IReadOnlyList<DetectedTable> Detect(string text) => Detect(Lines.Normalize(text));

    public static IReadOnlyList<DetectedTable> Detect(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Detect(document.Texts);
    }

    /// <param name="lines">Lines already passed through <see cref="Lines.Normalize(IReadOnlyList{string})"/>.</param>
    public static IReadOnlyList<DetectedTable> Detect(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        // Tables with indented rows under their outer rows run through blank lines, so they are found first and
        // their lines left out of the search for the rest.
        List<DetectedTable> tables = NestedRows.Find(lines);
        if (tables.Count > 0)
        {
            string[] rest = [.. lines];
            foreach (DetectedTable table in tables)
            {
                Array.Fill(rest, "", table.FirstLine, table.LastLine - table.FirstLine + 1);
            }

            lines = rest;
        }

        foreach (Span block in Blocks.Split(lines).SelectMany(b => Narrow(lines, b)))
        {
            List<Row> rows = JoinOverflow(lines, block);
            int pos = 0;
            while (pos < rows.Count)
            {
                Candidate? best = Choose(rows, pos, out int reach) ?? TryHeaderlessWords(rows, pos);
                if (best is null || MostlyKeyValue(rows, best))
                {
                    // Single-spaced lines that line up only by sharing their first words, as a flat configuration's
                    // do, would be scanned to their end again from every line among them. A long run of them that
                    // gave no table from its start gives none from inside it.
                    bool spaced = rows[pos].Text.AsSpan().TrimStart(' ').IndexOf("  ", StringComparison.Ordinal) >= 0;
                    pos = best is null && !spaced && reach - pos > MaxRescan ? reach : pos + 1;
                    continue;
                }

                tables.Add(Build(rows, best));
                pos = best.End;
            }
        }

        tables.Sort(static (a, b) => a.FirstLine.CompareTo(b.FirstLine));
        return tables;
    }

    private const int MaxRowWidth = 1000;

    // No table row runs a thousand characters, and column work grows with width, so a line that long, as a whole
    // configuration printed on one line, ends the stretch of lines around it.
    private static IEnumerable<Span> Narrow(IReadOnlyList<string> lines, Span block)
    {
        int start = block.Start;
        for (int i = block.Start; i < block.End; i++)
        {
            if (lines[i].Length > MaxRowWidth)
            {
                if (i > start)
                {
                    yield return new Span(start, i);
                }

                start = i + 1;
            }
        }

        if (block.End > start)
        {
            yield return new Span(start, block.End);
        }
    }

    // A title line above the header leaves most columns unnamed, so it scores far below the header. Among
    // close scores the earliest wins, since reading a data row as the header can only add columns.
    private const int MaxRescan = 50;

    /// <param name="reach">How far the scan from <paramref name="pos"/> itself reached.</param>
    private static Candidate? Choose(List<Row> rows, int pos, out int reach)
    {
        reach = pos + 1;
        List<Candidate> candidates = [];
        for (int h = pos; h < rows.Count && h < pos + HeaderCandidates; h++)
        {
            if (rows[h].IsRule)
            {
                continue;
            }

            Candidate? aligned = TryHeader(rows, h, out int scanned);
            if (h == pos)
            {
                reach = scanned;
            }

            if (aligned is { Layout: TableLayout.Ruled })
            {
                return aligned;
            }

            // Prefer alignment, and fall back to words only when alignment gives up rows that words keep.
            Candidate? words = TryWords(rows, h);
            Candidate? better = words is not null
                && (aligned is null || words.End > aligned.End
                    || (words.End == aligned.End && words.Groups.Count > aligned.Groups.Count))
                ? words
                : aligned;
            if (better is not null)
            {
                candidates.Add(better);
            }
        }

        // A reading without a header from inside one with a header splits the same rows by gaps that a few rows
        // leave open by chance, as between the words of "Main board inlet temperature", so it does not compete.
        candidates.RemoveAll(c => !c.HasHeader && candidates.Exists(n => n.HasHeader && n.Header < c.Header && c.Header <= n.FirstData && c.End <= n.End));
        int most = candidates.Count == 0 ? 0 : candidates.Max(static c => c.Groups.Count);
        Candidate? chosen = candidates.FirstOrDefault(c => c.Groups.Count * 4 >= most * 3);

        // A legend line above a header can start a table with no header that runs through the real one, as
        // "InUti/OutUti: input utility" above Huawei's "Interface  PHY  Protocol". When a header a line or two
        // further on reads the same rows, pass this start by so the search reaches it.
        if (chosen is { HasHeader: false })
        {
            for (int h = chosen.Header + 1; h <= chosen.Header + HeaderCandidates && h < chosen.End; h++)
            {
                if (!rows[h].IsRule && TryHeader(rows, h) is { HasHeader: true } named && named.End >= chosen.End - 1)
                {
                    // The caller must step to the next line, not past the scan, or the header is skipped.
                    reach = pos + 1;
                    return null;
                }
            }
        }

        return chosen;
    }

    // Splits each row into as many words as the header has, so it needs a header of single-word names. Rows
    // must agree with the first row on which columns are numeric, allowing one column to differ (a start time
    // that is "9:21AM" today and "Thu12PM" last week), which stops the table at a footer or a line of prose.
    private static Candidate? TryWords(List<Row> rows, int h)
    {
        Row header = rows[h];
        List<Span> words = Lines.Words(header.Text);
        if (header.Prefix is not null || words.Count < 4 || h + 1 >= rows.Count || rows[h + 1].IsRule)
        {
            return null;
        }

        // A sentence separates its words with single spaces, and a header pads at least one name. Header names
        // are not numbers, which rules out a timestamp line such as "Wed Dec  7 12:32:55.933 UTC".
        if (!IsPadded(words) || !IsHeaderLike(header.Text) || words.Any(w => Cells.IsNumeric(w.Of(header.Text))))
        {
            return null;
        }

        bool[]? pattern = null;
        HashSet<string>[] shapes = new HashSet<string>[words.Count];
        for (int c = 0; c < shapes.Length; c++)
        {
            shapes[c] = new HashSet<string>(StringComparer.Ordinal);
        }

        int end = h + 1;
        while (end < rows.Count && !rows[end].IsRule && rows[end].Prefix is null)
        {
            string[] cells = SplitWords(rows[end].Text, words.Count);
            if (cells[^1].Length == 0 || !UnderHeader(rows[end].Text, words))
            {
                break;
            }

            bool[] numeric = cells.Select(Cells.IsNumeric).ToArray();
            pattern ??= numeric;
            int differ = 0;
            for (int c = 0; c < numeric.Length - 1; c++)
            {
                differ += numeric[c] != pattern[c] ? 1 : 0;
            }

            if (numeric[0] != pattern[0] || differ > 1)
            {
                break;
            }

            for (int c = 0; c < cells.Length; c++)
            {
                shapes[c].Add(Cells.Shape(cells[c]));
            }

            end++;
        }

        if (end - (h + 1) < 2 || pattern is null || !pattern.Contains(true))
        {
            return null;
        }

        int alike = 0;
        for (int c = 0; c < words.Count; c++)
        {
            alike += shapes[c].Contains(Cells.Shape(words[c].Of(header.Text))) ? 1 : 0;
        }

        if (alike * 2 > words.Count)
        {
            return null;
        }

        List<ColumnGroup> groups = words.Select(static w => new ColumnGroup(w, [w])).ToList();
        return new Candidate(TableLayout.Words, h, h + 1, end, groups, HasHeader: true);
    }

    // Aligned "Key     : value" lines, as in "show vtp status", line up like a table but are a list of values,
    // which records read with their keys. A ruled header proves a table, and a cell that holds a colon in every
    // row, as "Port:50", repeats one key where a list of values names a different one on each line.
    private static bool MostlyKeyValue(List<Row> rows, Candidate candidate)
    {
        if (candidate.Layout == TableLayout.Ruled)
        {
            return false;
        }

        int data = 0;
        List<string> keys = [];
        for (int r = candidate.FirstData; r < candidate.End; r++)
        {
            if (!rows[r].IsRule)
            {
                data++;
                // "Mem:   15876   4211   6024" labels a row of cells; "Revision   : 0" holds one value.
                List<Token> tokens = Tokenizer.Tokenize(rows[r].Text);
                int separator = LineKeys.SeparatorIndex(tokens);
                bool cells = separator >= 0 && separator + 1 < tokens.Count
                    && rows[r].Text.AsSpan(tokens[separator + 1].Start).TrimEnd().IndexOf("  ", StringComparison.Ordinal) >= 0;
                if (LineKeys.IsKeyValue(tokens) && !cells)
                {
                    keys.Add(LineKeys.Key(tokens));
                }
            }
        }

        return keys.Count * 2 > data && keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() * 2 > keys.Count;
    }

    private static bool IsMarker(char c) => c is '*' or '+' or '#' or '~';

    /// <summary>
    /// Status marks glued to the first cell, as in NTP's <c>*10.1.1.1</c> and <c>~10.60.0.9</c>, become a
    /// <c>Flag</c> column of their own, so the first column holds only its value.
    /// </summary>
    private static void SplitMarkers(List<string[]> cells, List<DetectedColumn> columns)
    {
        bool marked = cells.Any(static r => r[0].Length > 1 && IsMarker(r[0][0]) && char.IsLetterOrDigit(r[0][1]));
        if (!marked)
        {
            return;
        }

        for (int r = 0; r < cells.Count; r++)
        {
            string first = cells[r][0];
            int n = 0;
            while (n < first.Length - 1 && IsMarker(first[n]))
            {
                n++;
            }

            cells[r] = [first[..n], first[n..], .. cells[r][1..]];
        }

        columns.Insert(0, new DetectedColumn("Flag", columns[0].Start, columns[0].Cut));
    }

    private const int MinHeaderlessRows = 3;

    /// <summary>
    /// Rows with no header and no alignment, such as <c>mgmt 192.168.42.2 10f3.1160.435d 0</c>: consecutive
    /// lines with the same number of words, where each position holds one kind of value (number, word, or a
    /// mix like a MAC address) in at least 80% of rows. Prose can share a word count, so at least one position
    /// must hold digits in most rows and at least two positions must vary.
    /// </summary>
    private static Candidate? TryHeaderlessWords(List<Row> rows, int pos)
    {
        Row first = rows[pos];
        int count = Lines.Words(first.Text).Count;
        if (first.IsRule || first.Prefix is not null || count < 3)
        {
            return null;
        }

        int end = pos;
        while (end < rows.Count && !rows[end].IsRule && rows[end].Prefix is null && Lines.Words(rows[end].Text).Count == count)
        {
            end++;
        }

        if (end - pos < MinHeaderlessRows)
        {
            return null;
        }

        if (rows.Skip(pos).Take(end - pos).Count(static r => r.Text.Contains(", ", StringComparison.Ordinal)) * 2 > end - pos)
        {
            return null;
        }

        List<string[]> cells = rows.Skip(pos).Take(end - pos).Select(r => SplitWords(r.Text, count)).ToList();
        bool digits = false;
        int varying = 0;
        for (int c = 0; c < count; c++)
        {
            List<string> column = cells.Select(r => r[c]).ToList();
            int consistent = column.GroupBy(Kind).Max(static g => g.Count());
            if (consistent * 5 < column.Count * 4)
            {
                return null;
            }

            digits |= column.Count(static v => v.Any(char.IsAsciiDigit)) * 5 >= column.Count * 4;
            varying += column.Distinct(StringComparer.Ordinal).Skip(1).Any() ? 1 : 0;
        }

        if (!digits || varying < 2)
        {
            return null;
        }

        List<ColumnGroup> groups = Lines.Words(first.Text).Select(static w => new ColumnGroup(w, [])).ToList();
        return new Candidate(TableLayout.Words, pos, pos, end, groups, HasHeader: false);

        static char Kind(string value) =>
            Cells.IsNumber(value) ? 'n' : value.All(static ch => char.IsLetter(ch) || ch is '-' or '_') ? 'w' : 'm';
    }

    // Drift moves a value a few characters from its header word, not across the line. Prose that happens to
    // have as many words as a header puts most of them far from the matching word.
    private static bool UnderHeader(string line, List<Span> header)
    {
        const int MaxDrift = 3;
        List<Span> cells = Lines.Words(line);
        int near = 0;
        for (int c = 0; c < header.Count - 1; c++)
        {
            near += cells[c].DistanceTo(header[c]) <= MaxDrift ? 1 : 0;
        }

        return near * 5 >= (header.Count - 1) * 4;
    }

    private static string[] SplitWords(string line, int count)
    {
        string[] cells = new string[count];
        int i = 0;
        for (int c = 0; c < count; c++)
        {
            while (i < line.Length && line[i] == ' ')
            {
                i++;
            }

            int start = i;
            if (c == count - 1)
            {
                cells[c] = line[start..].Trim();
                break;
            }

            while (i < line.Length && line[i] != ' ')
            {
                i++;
            }

            cells[c] = line[start..i];
        }

        return cells;
    }

    /// <summary>
    /// A header names columns, so it holds no <c>key:</c> separators, no plain numbers, and no words ending in
    /// a comma. <c>Interface: Gi1/0/1,  Port ID: Gi0/1</c> or <c>MTU 1500 bytes, BW 1000000 Kbit/sec</c> lines up
    /// with the lines below it by chance, and is a record line, not a header.
    /// </summary>
    internal static bool IsHeaderLike(string line)
    {
        // One "|" in a single-spaced line pipes a command, as in "show route instance | no-more". A header divided
        // by a pipe pads its names, as "Port   Status  | Sys-id" does.
        int pipe = line.IndexOf(" | ", StringComparison.Ordinal);
        if (pipe >= 0 && line.IndexOf('|', pipe + 2) < 0
            && line.AsSpan().TrimStart(' ').IndexOf("  ", StringComparison.Ordinal) < 0)
        {
            return false;
        }

        List<Span> words = Lines.Words(line);
        foreach (Span word in words)
        {
            string w = word.Of(line);
            if (LineKeys.IsSeparator(w) || Cells.IsNumber(w) || w == "-" || (w.Length > 1 && w.EndsWith(',')))
            {
                return false;
            }
        }

        // A header pads at least one name from the next. A line of single-spaced words is a sentence, such as
        // "Spanning tree enabled protocol rstp", or a legend such as "H - Hot-standby (LACP only)".
        return IsPadded(words);
    }

    private static Candidate? TryHeader(List<Row> rows, int h) => TryHeader(rows, h, out _);

    /// <param name="reach">The row the scan for aligned rows stopped at, whether or not a table came of it.</param>
    private static Candidate? TryHeader(List<Row> rows, int h, out int reach)
    {
        reach = h + 1;
        Row header = rows[h];
        if (header.Prefix is not null)
        {
            return null;
        }

        List<Span> words = Lines.Words(header.Text);
        int first = h + 1;
        if (first < rows.Count && rows[first].IsRule)
        {
            List<Span> segments = Lines.RuleSegments(rows[first].Text);
            if (segments.Count >= 2)
            {
                return FromRule(rows, h, first + 1, segments, words);
            }

            first++;
        }

        Occupancy occupancy = new();
        List<ColumnGroup> groups = [];
        int end = first;
        int dataRows = 0;
        for (; end < rows.Count; end++)
        {
            if (rows[end].IsRule)
            {
                continue;
            }

            Occupancy next = occupancy.Clone();
            next.Add(rows[end].Text);
            List<ColumnGroup> nextGroups = HeaderAlignment.Align(next.Spans(), words);

            // A real row closes at most a gap an earlier row left open by chance, and runs into the next column
            // at most once, where a value was truncated to fit. A legend or a footer does both many times.
            if (dataRows >= 1
                && (nextGroups.Count < 2 || nextGroups.Count <= groups.Count - 2 || Crossings(rows[end].Text, words) >= 2))
            {
                break;
            }

            occupancy = next;
            groups = nextGroups;
            dataRows++;
        }

        reach = end;
        while (end > first && rows[end - 1].IsRule)
        {
            end--;
        }

        // One row is enough under a padded header when the row holds a value, as in the short sections of
        // "show interface trunk". Without the digit, a heading over a line of prose would pass.
        bool oneRow = dataRows == 1 && IsPadded(words) && rows[first].Text.Any(char.IsAsciiDigit);
        if ((dataRows < 2 && !oneRow) || groups.Count < 2 || !HasEvidence(groups, dataRows))
        {
            return null;
        }

        if (!IsHeaderLike(header.Text) || HeaderIsData(header.Text, groups, rows, first, end))
        {
            if (dataRows < 2)
            {
                return null;
            }

            occupancy.Add(header.Text);
            List<ColumnGroup> unnamed = HeaderAlignment.Align(occupancy.Spans(), words: null);

            // With no header to vouch for the columns, most of them must vary, or this is repeated phrasing.
            // Rows that read as phrases, mostly holding ", ", are sentences that happen to line up.
            int varying = VaryingColumns(unnamed, rows, h, end);
            int phrases = Enumerable.Range(h, end - h).Count(r => !rows[r].IsRule && rows[r].Text.Contains(", ", StringComparison.Ordinal));
            return varying >= 2 && varying * 2 > unnamed.Count && phrases * 2 <= end - h && !Wordy(unnamed, rows, h, end)
                && LooksTabular(unnamed, rows, h, end)
                ? new Candidate(TableLayout.Aligned, h, h, end, unnamed, HasHeader: false)
                : null;
        }

        if (dataRows >= 2 && IsHeaderContinuation(rows[first].Text, groups, rows, first + 1, end))
        {
            return new Candidate(TableLayout.Aligned, h, first + 1, end, groups, HasHeader: true, SubHeader: first);
        }

        return new Candidate(TableLayout.Aligned, h, first, end, groups, HasHeader: true);
    }

    // Counts the padded header names whose first position the line's text runs into without a break.
    private static int Crossings(string line, List<Span> words)
    {
        int crossings = 0;
        for (int w = 1; w < words.Count; w++)
        {
            int p = words[w].Start;
            if (words[w].Start - words[w - 1].End >= 2 && p < line.Length
                && !Lines.IsSeparator(line[p - 1]) && !Lines.IsSeparator(line[p]))
            {
                crossings++;
            }
        }

        return crossings;
    }

    private static bool IsPadded(List<Span> words)
    {
        for (int w = 1; w < words.Count; w++)
        {
            if (words[w].Start - words[w - 1].End >= 2)
            {
                return true;
            }
        }

        return false;
    }

    // A second header line, like "Mode  Count  Intvl" under "Ver/  Nbr  Query", holds names: no digits, at
    // least two cells, and none shaped like a value in its column.
    private static bool IsHeaderContinuation(string line, List<ColumnGroup> groups, List<Row> rows, int first, int end)
    {
        if (line.Any(char.IsAsciiDigit))
        {
            return false;
        }

        int[] cuts = Cuts(groups);
        string[] cells = Cells.Slice(line, cuts);
        if (cells.Count(static c => c.Length > 0) < 2)
        {
            return false;
        }

        // Header names start with a capital, as "Native" and "Vlan" do. A row of lowercase words, as "mgmt  not
        // set", is data.
        if (cells.Where(static c => c.Length > 0).All(static c => char.IsLower(c[0])))
        {
            return false;
        }

        for (int r = first; r < end; r++)
        {
            if (rows[r].IsRule)
            {
                continue;
            }

            string[] values = Cells.Slice(rows[r].Text, cuts);
            for (int c = 0; c < cells.Length; c++)
            {
                if (cells[c].Length > 0 && values[c].Length > 0 && Cells.Shape(values[c]) == Cells.Shape(cells[c]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // A table with no header needs other evidence that it is one: a column of values with digits in most rows,
    // as addresses, counters, and ports have, and a first column filled in most rows. A legend of letters and
    // meanings has no digits, and the indented attributes under a heading leave the first column empty.
    private static bool LooksTabular(List<ColumnGroup> groups, List<Row> rows, int first, int end)
    {
        int[] cuts = Cuts(groups);
        List<string[]> cells = Enumerable.Range(first, end - first).Where(r => !rows[r].IsRule).Select(r => Cells.Slice(rows[r].Text, cuts)).ToList();
        bool digits = Enumerable.Range(0, cuts.Length).Any(c => cells.Count(r => r[c].Any(char.IsAsciiDigit)) * 5 >= cells.Count * 4);
        bool firstFilled = cells.Count(static r => r[0].Length > 0) * 2 >= cells.Count;
        return digits && firstFilled;
    }

    private const int MaxOverflow = 3;

    // A value a few characters wider than its column, as "eth-switch" under a five-wide "Proto", runs past the
    // cut into the next column. Moves such a cut to the end of the word, so the value stays whole. A word that
    // runs further past belongs to columns packed with no gap between them, and keeps its cut.
    private static int[] Snap(string line, int[] cuts)
    {
        int[] snapped = (int[])cuts.Clone();
        for (int c = 1; c < snapped.Length; c++)
        {
            int p = snapped[c];
            if (p <= 0 || p >= line.Length || Lines.IsSeparator(line[p - 1]) || Lines.IsSeparator(line[p]))
            {
                continue;
            }

            int end = p;
            while (end < line.Length && !Lines.IsSeparator(line[end]))
            {
                end++;
            }

            if (end - p <= MaxOverflow && (c + 1 == snapped.Length || end <= snapped[c + 1]))
            {
                snapped[c] = end;
            }
        }

        return snapped;
    }

    private const double MaxHeaderlessCellWords = 2.5;

    // The cells of a table with no header are values of a word or two. Sentences that happen to line up, as the
    // detail lines of an OSPF neighbor, give cells of many words.
    private static bool Wordy(List<ColumnGroup> groups, List<Row> rows, int first, int end)
    {
        int[] cuts = Cuts(groups);
        int cells = 0;
        int words = 0;
        for (int r = first; r < end; r++)
        {
            if (rows[r].IsRule)
            {
                continue;
            }

            foreach (string cell in Cells.Slice(rows[r].Text, cuts).Where(static c => c.Length > 0))
            {
                cells++;
                words += Lines.Words(cell).Count;
            }
        }

        return cells > 0 && words > cells * MaxHeaderlessCellWords;
    }

    // Lines that share a fixed phrase, such as "? (ip) at mac on en0", line up into a constant column and a
    // varying one. A table needs at least two columns whose values change.
    private static int VaryingColumns(List<ColumnGroup> groups, List<Row> rows, int first, int end)
    {
        int[] cuts = Cuts(groups);
        HashSet<string>[] seen = new HashSet<string>[cuts.Length];
        for (int c = 0; c < seen.Length; c++)
        {
            seen[c] = new HashSet<string>(StringComparer.Ordinal);
        }

        for (int r = first; r < end; r++)
        {
            if (rows[r].IsRule)
            {
                continue;
            }

            string[] cells = Cells.Slice(rows[r].Text, cuts);
            for (int c = 0; c < cells.Length; c++)
            {
                if (cells[c].Length > 0)
                {
                    seen[c].Add(cells[c]);
                }
            }
        }

        return seen.Count(static s => s.Count > 1);
    }

    // Rules like "----- ---- -----" under the header mark the columns outright.
    private static Candidate? FromRule(List<Row> rows, int h, int first, List<Span> segments, List<Span> words)
    {
        int end = first;
        while (end < rows.Count && !rows[end].IsRule && (end == first || !Bridges(rows[end].Text, segments)))
        {
            end++;
        }

        List<ColumnGroup> groups = HeaderAlignment.Align(segments, words);
        return end > first && groups.Count >= 2
            ? new Candidate(TableLayout.Ruled, h, first, end, groups, HasHeader: true)
            : null;
    }

    private static bool Bridges(string line, List<Span> segments)
    {
        for (int s = 1; s < segments.Count; s++)
        {
            int p = segments[s].Start - 1;
            if (p > 0 && p + 1 < line.Length && !Lines.IsSeparator(line[p - 1]) && !Lines.IsSeparator(line[p])
                && !Lines.IsSeparator(line[p + 1]))
            {
                return true;
            }
        }

        return false;
    }

    // In a few lines of prose, single spaces line up by chance. Wider gaps or more rows rule that out.
    private static bool HasEvidence(List<ColumnGroup> groups, int dataRows)
    {
        if (dataRows >= 5)
        {
            return true;
        }

        for (int g = 1; g < groups.Count; g++)
        {
            if (groups[g].Data.Start - groups[g - 1].Data.End >= 2)
            {
                return true;
            }
        }

        return false;
    }

    // A header holds names, so its cells look unlike the values below them: "VSZ" over numbers, "Status" over
    // "up". When most header cells have the same shape as some value in their column, the candidate header is
    // the first row of a table that has none.
    private static bool HeaderIsData(string header, List<ColumnGroup> groups, List<Row> rows, int first, int end)
    {
        int[] cuts = Cuts(groups);
        string[] head = Cells.Slice(header, cuts);
        HashSet<string>[] shapes = new HashSet<string>[cuts.Length];
        for (int c = 0; c < shapes.Length; c++)
        {
            shapes[c] = new HashSet<string>(StringComparer.Ordinal);
        }

        for (int r = first; r < end; r++)
        {
            if (rows[r].IsRule)
            {
                continue;
            }

            string[] cells = Cells.Slice(rows[r].Text, cuts);
            for (int c = 0; c < cells.Length; c++)
            {
                if (cells[c].Length > 0)
                {
                    shapes[c].Add(Cells.Shape(cells[c]));
                }
            }
        }

        int named = 0;
        int alike = 0;
        for (int c = 0; c < head.Length; c++)
        {
            // Names are not numbers: "Wed Dec  7 12:32:55.933 UTC" is a timestamp, not a header.
            if (Cells.IsNumeric(head[c]))
            {
                return true;
            }

            if (head[c].Length > 0)
            {
                named++;
                alike += shapes[c].Contains(Cells.Shape(head[c])) ? 1 : 0;
            }
        }

        return alike * 2 > named;
    }

    private static int[] Cuts(List<ColumnGroup> groups)
    {
        int[] cuts = new int[groups.Count];
        for (int g = 1; g < groups.Count; g++)
        {
            cuts[g] = groups[g].Data.Start;
        }

        return cuts;
    }

    private static DetectedTable Build(List<Row> rows, Candidate candidate)
    {
        string headerText = rows[candidate.Header].Text;
        int[] cuts = Cuts(candidate.Groups);
        List<DetectedColumn> columns = new(cuts.Length);
        for (int g = 0; g < cuts.Length; g++)
        {
            ColumnGroup group = candidate.Groups[g];
            string name = candidate.HasHeader && group.Words.Count > 0
                ? string.Join(" ", group.Words.Select(w => w.Of(headerText)))
                : "Column" + (g + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (candidate.SubHeader is { } sub && Cells.Slice(rows[sub].Text, cuts)[g] is { Length: > 0 } more)
            {
                name = name.Length == 0 ? more : name + " " + more;
            }

            int start = group.Words.Count > 0 ? Math.Min(group.Data.Start, group.Words[0].Start) : group.Data.Start;
            // Headers repeat names, as two "*" columns or "Port" twice, and a row is read by name.
            string unique = name;
            for (int n = 2; columns.Exists(c => string.Equals(c.Name, unique, StringComparison.OrdinalIgnoreCase)); n++)
            {
                unique = name + " " + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            columns.Add(new DetectedColumn(unique, start, cuts[g]));
        }

        List<string[]> cells = [];
        List<Span> rowLines = [];
        for (int r = candidate.FirstData; r < candidate.End; r++)
        {
            if (rows[r].IsRule)
            {
                continue;
            }

            string[] row = candidate.Layout == TableLayout.Words
                ? SplitWords(rows[r].Text, cuts.Length)
                : Cells.Slice(rows[r].Text, Snap(rows[r].Text, cuts));
            if (rows[r].Prefix is { } prefix)
            {
                row[0] = row[0].Length == 0 ? prefix : prefix + " " + row[0];
            }

            cells.Add(row);
            rowLines.Add(new Span(rows[r].FirstLine, rows[r].LastLine + 1));
        }

        Cells.MergeContinuations(cells, rowLines);
        SplitMarkers(cells, columns);
        foreach (string[] row in cells)
        {
            for (int c = 0; c < row.Length; c++)
            {
                row[c] = Cells.Unwrap(row[c]);
            }
        }

        return new DetectedTable(
            candidate.Layout,
            candidate.HasHeader ? rows[candidate.Header].FirstLine : null,
            rows[candidate.Header].FirstLine,
            rows[candidate.End - 1].LastLine,
            columns,
            cells,
            rowLines
        );
    }

    // A first cell too wide for its column often sits alone on a line, with the rest of the row indented
    // below it. Joins the two so the long value does not close every gap it crosses.
    private static List<Row> JoinOverflow(IReadOnlyList<string> lines, Span block)
    {
        int indent = int.MaxValue;
        for (int i = block.Start; i < block.End; i++)
        {
            indent = Math.Min(indent, Lines.Indent(lines[i]));
        }

        int atIndent = 0;
        for (int i = block.Start; i < block.End; i++)
        {
            if (Lines.Indent(lines[i]) == indent && Lines.Classify(lines[i]) != LineKind.Rule)
            {
                atIndent++;
            }
        }

        List<Row> rows = [];
        for (int i = block.Start; i < block.End; i++)
        {
            string line = lines[i];
            if (Lines.Classify(line) == LineKind.Rule)
            {
                rows.Add(new Row(line, null, i, i, IsRule: true));
                continue;
            }

            if (i + 1 < block.End
                && atIndent > 2
                && Lines.Indent(line) == indent
                && Lines.Words(line).Count == 1
                && Lines.Classify(lines[i + 1]) != LineKind.Rule
                && Lines.Indent(lines[i + 1]) >= indent + 2
                && Lines.Words(lines[i + 1]).Count > 1)
            {
                rows.Add(new Row(lines[i + 1], line.Trim(), i, i + 1, IsRule: false));
                i++;
                continue;
            }

            rows.Add(new Row(line, null, i, i, IsRule: false));
        }

        return rows;
    }

    /// <param name="Prefix">The first cell, when it overflowed onto a line of its own.</param>
    private sealed record Row(string Text, string? Prefix, int FirstLine, int LastLine, bool IsRule);

    private sealed record Candidate(
        TableLayout Layout,
        int Header,
        int FirstData,
        int End,
        List<ColumnGroup> Groups,
        bool HasHeader,
        int? SubHeader = null
    );
}
