namespace JMW.TextExtraction.Records;

/// <summary>What separates one record from the next.</summary>
public enum RecordBoundary
{
    /// <summary>A rule line such as <c>-------------------------</c>.</summary>
    Rule,

    /// <summary>One or more blank lines.</summary>
    Blank,

    /// <summary>A line with a given key, such as <c>Device ID:</c>, starts each record.</summary>
    Anchor,

    /// <summary>
    /// Each line at a given indent starts a record, and the deeper lines below belong to it. The indent is the
    /// outer one unless the spec names another, as for records set one space in under a title.
    /// </summary>
    Indent,

    /// <summary>
    /// A line containing a given run of words, such as <c>line protocol is</c>, starts each record. For records
    /// whose first line begins with a name rather than a fixed key, and that no blank line or indent sets apart.
    /// </summary>
    Phrase,
}

public static class RecordSegmenter
{
    /// <summary>
    /// Splits lines into records as half-open line ranges, with blank lines trimmed from each end. With
    /// <see cref="RecordBoundary.Anchor"/>, <see cref="RecordBoundary.Phrase"/>, and
    /// <see cref="RecordBoundary.Indent"/>, the lines before the first record come back as a record of their
    /// own, for the caller to judge. Anchors and phrases start records at the shallowest indent they appear at,
    /// and any content line shallower than that closes a record, as a title between groups of records does.
    /// </summary>
    /// <param name="anchor">
    /// The key for <see cref="RecordBoundary.Anchor"/>, the words for <see cref="RecordBoundary.Phrase"/>, or for
    /// <see cref="RecordBoundary.Indent"/>, the indent records start at, which defaults to the outer one.
    /// </param>
    public static List<Span> Segment(TextDocument document, RecordBoundary boundary, string? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        List<Span> raw;
        switch (boundary)
        {
            case RecordBoundary.Rule:
                // A rule indented under the records underlines a heading inside one, as "  Statistics" over
                // "  ----------", and does not separate them.
                raw = SplitAt(document, l => l.Kind == LineKind.Rule && l.Indent <= document.OuterIndent);
                break;
            case RecordBoundary.Blank:
                raw = SplitAt(document, static l => l.Kind == LineKind.Blank);
                break;
            case RecordBoundary.Anchor:
                ArgumentNullException.ThrowIfNull(anchor);
                raw = AtLevel(document, l => string.Equals(l.Key, anchor, StringComparison.OrdinalIgnoreCase));
                break;
            case RecordBoundary.Phrase:
                ArgumentNullException.ThrowIfNull(anchor);
                string[] phrase = anchor.Split(' ');
                raw = AtLevel(document, l => ContainsRun(l.Tokens, phrase));
                break;
            case RecordBoundary.Indent:
                int level = anchor is null ? document.OuterIndent : int.Parse(anchor, System.Globalization.CultureInfo.InvariantCulture);
                raw = FromStarts(document, l => l.Indent <= level);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(boundary));
        }

        List<Span> records = [];
        foreach (Span r in raw)
        {
            int start = r.Start;
            int end = r.End;
            while (start < end && document[start].Kind == LineKind.Blank)
            {
                start++;
            }

            while (end > start && document[end - 1].Kind == LineKind.Blank)
            {
                end--;
            }

            if (end > start)
            {
                records.Add(new Span(start, end));
            }
        }

        if (boundary is RecordBoundary.Rule or RecordBoundary.Blank)
        {
            SplitPreamble(document, records);
        }

        return records;
    }

    // A command echo with no blank line after it joins the first record, as "show interfaces" above an
    // "  Ethernet 1 is up" whose fellows all start two spaces in. When the other records start at one indent and
    // the first starts at another, the lines before its first line at that indent are split off on their own.
    private static void SplitPreamble(TextDocument document, List<Span> records)
    {
        if (records.Count < 3)
        {
            return;
        }

        int? Opening(Span r)
        {
            for (int i = r.Start; i < r.End; i++)
            {
                if (document[i].IsContent)
                {
                    return document[i].Indent;
                }
            }

            return null;
        }

        int[] openings = records.Skip(1).Select(Opening).OfType<int>().ToArray();
        if (openings.Length == 0)
        {
            return;
        }

        int usual = openings.GroupBy(static i => i).OrderByDescending(static g => g.Count()).First().Key;
        Span first = records[0];
        if (Opening(first) is not { } indent)
        {
            return;
        }

        if (indent != usual)
        {
            for (int i = first.Start; i < first.End; i++)
            {
                if (document[i].IsContent && document[i].Indent == usual)
                {
                    records[0] = new Span(first.Start, i);
                    records.Insert(1, new Span(i, first.End));
                    return;
                }
            }

            return;
        }

        // The same at one indent: when the other records open with one shape of line, as an address followed
        // by "configured", and the first reaches that shape a line or two in, the lines before it are preamble.
        string?[] keys = records.Skip(1).Select(r => First(document, r)?.Key).ToArray();
        string? common = keys.OfType<string>().GroupBy(static k => k, StringComparer.OrdinalIgnoreCase).OrderByDescending(static g => g.Count()).FirstOrDefault()?.Key;
        if (common is null || keys.Count(k => string.Equals(k, common, StringComparison.OrdinalIgnoreCase)) * 2 <= keys.Length
            || string.Equals(First(document, first)?.Key, common, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        for (int i = first.Start, seen = 0; i < first.End && seen < MaxPreambleLines; i++)
        {
            if (!document[i].IsContent)
            {
                continue;
            }

            if (string.Equals(document[i].Key, common, StringComparison.OrdinalIgnoreCase))
            {
                records[0] = new Span(first.Start, i);
                records.Insert(1, new Span(i, first.End));
                return;
            }

            seen++;
        }
    }

    private const int MaxPreambleLines = 3;

    private static TextLine? First(TextDocument document, Span record)
    {
        for (int i = record.Start; i < record.End; i++)
        {
            if (document[i].IsContent)
            {
                return document[i];
            }
        }

        return null;
    }

    public static List<Span> Segment(string text, RecordBoundary boundary, string? anchor = null) =>
        Segment(TextDocument.Parse(text), boundary, anchor);

    public static bool ContainsRun(IReadOnlyList<Token> tokens, IReadOnlyList<string> words)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        ArgumentNullException.ThrowIfNull(words);
        for (int i = 0; i + words.Count <= tokens.Count; i++)
        {
            int w = 0;
            while (w < words.Count && string.Equals(tokens[i + w].Text, words[w], StringComparison.OrdinalIgnoreCase))
            {
                w++;
            }

            if (w == words.Count)
            {
                return true;
            }
        }

        return false;
    }

    private static List<Span> SplitAt(TextDocument document, Func<TextLine, bool> isSeparator)
    {
        List<Span> spans = [];
        int start = 0;
        for (int i = 0; i < document.Count; i++)
        {
            if (isSeparator(document[i]))
            {
                spans.Add(new Span(start, i));
                start = i + 1;
            }
        }

        spans.Add(new Span(start, document.Count));
        return spans;
    }

    // Records start at the lines that pass the test, at the shallowest indent any of them sits at. A content line
    // shallower still starts a stretch of its own, so a title does not join the record above it.
    private static List<Span> AtLevel(TextDocument document, Func<TextLine, bool> starts)
    {
        int level = document.Where(l => l.IsContent && starts(l)).Select(static l => l.Indent).DefaultIfEmpty(document.OuterIndent).Min();
        return FromStarts(document, l => l.Indent < level || (l.Indent == level && starts(l)));
    }

    // Records start at content lines that pass the test. The lines before the first start come back as a record
    // of their own.
    private static List<Span> FromStarts(TextDocument document, Func<TextLine, bool> starts)
    {
        List<int> at = [];
        foreach (TextLine line in document)
        {
            if (line.IsContent && starts(line))
            {
                at.Add(line.Index);
            }
        }

        List<Span> spans = [];
        if (at.Count == 0)
        {
            spans.Add(new Span(0, document.Count));
            return spans;
        }

        spans.Add(new Span(0, at[0]));
        for (int s = 0; s < at.Count; s++)
        {
            spans.Add(new Span(at[s], s + 1 < at.Count ? at[s + 1] : document.Count));
        }

        return spans;
    }
}
