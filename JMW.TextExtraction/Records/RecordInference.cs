namespace JMW.TextExtraction.Records;

/// <summary>
/// Infers a <see cref="RecordSpec"/> from one or more samples of the same output.
/// </summary>
/// <remarks>
/// Records are the units that repeat. Each way of splitting the text (rule lines, blank lines, an anchor key,
/// indentation, a repeated phrase) is scored by how much the resulting records share: the fraction of their
/// lines whose key appears in at least half the records, times the fraction of lines the records cover, times
/// the fraction of records longer than one line. Splitting at the wrong place gives records that do not
/// resemble each other, so the score falls. Leading and trailing stretches unlike the rest, such as a command
/// echo or a summary, are left out. Samples are pooled, so one sample with a single record still counts.
/// </remarks>
public static class RecordInference
{
    private const double MinScore = 0.5;

    private const int MaxAnchors = 8;

    private const double MinKeyShare = 0.01;

    private const int PhraseLength = 3;

    private const int MinSampleLines = 3;

    public static RecordSpec? Infer(params string[] samples) => Infer((IReadOnlyList<string>)samples);

    public static RecordSpec? Infer(IReadOnlyList<string> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return Infer(samples.Select(TextDocument.Parse).ToList());
    }

    /// <summary>
    /// Groups the samples by format with <see cref="SampleFormats.Group(IReadOnlyList{TextDocument}, double)"/>
    /// and infers a spec for each group. Groups whose samples show no repeated records get no spec.
    /// </summary>
    public static List<FormatSpec> InferFormats(IReadOnlyList<string> samples, double similarity = SampleFormats.DefaultSimilarity)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return InferFormats(samples.Select(TextDocument.Parse).ToList(), similarity);
    }

    public static List<FormatSpec> InferFormats(IReadOnlyList<TextDocument> samples, double similarity = SampleFormats.DefaultSimilarity)
    {
        ArgumentNullException.ThrowIfNull(samples);
        List<FormatSpec> formats = [];
        foreach (List<int> group in SampleFormats.Group(samples, similarity))
        {
            if (Infer(group.Select(i => samples[i]).ToList()) is { } spec)
            {
                formats.Add(new FormatSpec(spec, group));
            }
        }

        return formats;
    }

    public static RecordSpec? Infer(IReadOnlyList<TextDocument> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        List<(RecordBoundary Boundary, string? Anchor)> options =
        [
            (RecordBoundary.Rule, null),
            (RecordBoundary.Blank, null),
            .. AnchorKeys(samples).Select(static k => (RecordBoundary.Anchor, (string?)k)),
            (RecordBoundary.Indent, null),
            .. IndentLevels(samples).Select(static l => (RecordBoundary.Indent, (string?)l)),
            .. Phrases(samples).Select(static p => (RecordBoundary.Phrase, (string?)p)),
        ];

        double bestScore = MinScore;
        (RecordBoundary Boundary, string? Anchor, bool KeyHead)? best = null;
        List<List<KeyedLine>> bestRecords = [];
        foreach ((RecordBoundary boundary, string? anchor) in options)
        {
            (double score, List<List<KeyedLine>> records, bool keyHead) = Evaluate(samples, boundary, anchor);
            if (score > bestScore)
            {
                bestScore = score;
                best = (boundary, anchor, keyHead);
                bestRecords = records;
            }
        }

        if (best is not { } chosen)
        {
            return null;
        }

        return new RecordSpec(chosen.Boundary, chosen.Anchor, chosen.KeyHead, Templates(bestRecords));
    }

    /// <summary>How well records split this way resemble each other, from 0 to 1.</summary>
    public static double Score(IReadOnlyList<string> samples, RecordBoundary boundary, string? anchor = null)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return Evaluate(samples.Select(TextDocument.Parse).ToList(), boundary, anchor).Score;
    }

    // Records are compared as sets of keys. A key in too few of them, such as a command echo or a timestamp
    // caught in the first record of a few samples, is noise rather than part of the format. A "key: value" line
    // says what it is, so one record holding it is enough.
    internal static List<LineTemplate> Templates(List<List<KeyedLine>> records)
    {
        Dictionary<string, int> inRecords = new(StringComparer.OrdinalIgnoreCase);
        foreach (List<KeyedLine> record in records)
        {
            foreach (string path in record.Select(static k => k.Path).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                inRecords[path] = inRecords.GetValueOrDefault(path) + 1;
            }
        }

        int minRecords = Math.Max(2, (int)Math.Ceiling(records.Count * MinKeyShare));
        List<LineTemplate> templates = [];
        foreach (IGrouping<string, KeyedLine> group in records
            .SelectMany(static r => r)
            .Where(k => inRecords[k.Path] >= (SaysWhatItIs(k.Tokens) ? 1 : minRecords))
            .GroupBy(static k => k.Path, StringComparer.OrdinalIgnoreCase))
        {
            // A key whose value sits below it in some samples and beside it in others takes the commoner form.
            List<KeyedLine> blocks = group.Where(static k => k.Text is not null).ToList();
            if (blocks.Count * 2 > group.Count())
            {
                templates.Add(LineTemplate.Block(group.Key, blocks[0].Tokens));
            }
            else
            {
                templates.AddRange(LineTemplate.InferVariants(group.Key, group.Where(static k => k.Text is null).Select(static k => k.Tokens).ToList()));
            }
        }

        return Distinct(templates);
    }

    // "key: value" says what it is, and so do a count and what it counts, as "914846830 packets input" or "219k
    // bytes of memory", and a short subject with "is", as "Boot device on node 0/RSP0/CPU0 is disk0:".
    private static bool SaysWhatItIs(IReadOnlyList<Token> tokens)
    {
        if (LineKeys.IsKeyValue(tokens) || (tokens.Count >= 2 && Cells.IsNumeric(tokens[0].Text) && char.IsLetter(tokens[1].Text[0])))
        {
            return true;
        }

        for (int i = 1; i < tokens.Count - 1 && i <= StatementReach; i++)
        {
            if (tokens[i].Text is "," or ";")
            {
                break;
            }

            if (tokens[i].Text is "is" or "are")
            {
                return true;
            }
        }

        return IsPairList(tokens);
    }

    // Comma-separated words and a number, as "Configured hello time 2, max age 20, forward delay 15".
    private static bool IsPairList(IReadOnlyList<Token> tokens)
    {
        int pairs = 0;
        int segments = 0;
        int start = 0;
        for (int i = 0; i <= tokens.Count; i++)
        {
            if (i < tokens.Count && tokens[i].Text != ",")
            {
                continue;
            }

            segments++;
            pairs += i - start >= 2 && Cells.IsNumeric(tokens[i - 1].Text) && char.IsLetter(tokens[start].Text[0]) ? 1 : 0;
            start = i + 1;
        }

        return pairs >= 2 && pairs * 2 > segments;
    }

    private const int StatementReach = 6;

    // Slot names stay unique across a record's lines, so "Neighbor" from the head line and "Neighbor" from
    // "Neighbor is up for 2d03h" do not read as one field. Variants of one line share names on purpose.
    private static List<LineTemplate> Distinct(List<LineTemplate> templates)
    {
        Dictionary<string, string> owner = new(StringComparer.OrdinalIgnoreCase);
        List<LineTemplate> result = [];
        foreach (LineTemplate template in templates)
        {
            List<TemplatePart> parts = [];
            foreach (TemplatePart part in template.Parts)
            {
                if (part is not Slot slot)
                {
                    parts.Add(part);
                    continue;
                }

                string name = slot.Name;
                for (int n = 2; owner.TryGetValue(name, out string? key) && !string.Equals(key, template.Key, StringComparison.OrdinalIgnoreCase); n++)
                {
                    name = slot.Name + " " + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                owner.TryAdd(name, template.Key);
                parts.Add(slot with { Name = name });
            }

            result.Add(template with { Parts = parts });
        }

        return result;
    }

    private const int MaxIndentLevels = 3;

    private const int MaxIndentLevel = 8;

    /// <summary>
    /// Indents deeper than the outer one that records might start at, as when records sit one space in under
    /// a title: the shallowest few that hold more than one line in some sample.
    /// </summary>
    private static IEnumerable<string> IndentLevels(IReadOnlyList<TextDocument> samples) =>
        samples
            .SelectMany(static d => d.Where(l => l.IsContent && l.Indent > d.OuterIndent && l.Indent <= MaxIndentLevel)
                .GroupBy(static l => l.Indent)
                .Where(static g => g.Count() > 1)
                .Select(static g => g.Key))
            .Distinct()
            .Order()
            .Take(MaxIndentLevels)
            .Select(static l => l.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>
    /// Runs of three plain words (letters only) on outer-indent lines that repeat within a sample, ranked by how
    /// many samples repeat them. "line protocol is" finds the first line of each interface in output whose
    /// interface lines start with the interface's own name. The words before the phrase must mostly differ from
    /// line to line, as a name does, or the phrase belongs to a detail line such as "Internet address is not
    /// set", and starting records there would only rotate them.
    /// </summary>
    private static IEnumerable<string> Phrases(IReadOnlyList<TextDocument> samples)
    {
        Dictionary<string, int> repeatedIn = new(StringComparer.OrdinalIgnoreCase);
        foreach (TextDocument document in samples)
        {
            Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Dictionary<string, int>> prefixes = new(StringComparer.OrdinalIgnoreCase);
            foreach (TextLine line in document)
            {
                if (!line.IsContent || line.Indent != document.OuterIndent)
                {
                    continue;
                }

                IReadOnlyList<Token> tokens = line.Tokens;
                Dictionary<string, string> seen = new(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i + PhraseLength <= tokens.Count; i++)
                {
                    if (tokens.Skip(i).Take(PhraseLength).All(static t => t.Text.All(char.IsLetter)))
                    {
                        // What tells head lines apart is the name beside the phrase: before it, as in "Te 0/1 is up,
                        // line protocol is up", or after it when the phrase opens the line, as in "DTP information
                        // for Gi1/0/1:". Numbers are masked, so counters that change do not pass for names.
                        string phrase = string.Join(" ", tokens.Skip(i).Take(PhraseLength).Select(static t => t.Text));
                        IEnumerable<Token> name = i > 0 ? tokens.Take(i) : tokens.Skip(PhraseLength);
                        seen.TryAdd(phrase, string.Join(" ", name.Select(static t => Cells.IsNumber(t.Text) ? "~" : t.Text)));
                    }
                }

                foreach ((string phrase, string prefix) in seen)
                {
                    counts[phrase] = counts.GetValueOrDefault(phrase) + 1;
                    Dictionary<string, int> byPrefix = prefixes.TryGetValue(phrase, out Dictionary<string, int>? p)
                        ? p
                        : prefixes[phrase] = new(StringComparer.OrdinalIgnoreCase);
                    byPrefix[prefix] = byPrefix.GetValueOrDefault(prefix) + 1;
                }
            }

            foreach (string phrase in counts.Where(c => c.Value > 1 && prefixes[c.Key].Values.Max() * 2 < c.Value).Select(static c => c.Key))
            {
                repeatedIn[phrase] = repeatedIn.GetValueOrDefault(phrase) + 1;
            }
        }

        return Ranked(repeatedIn);
    }

    /// <summary>
    /// Keys that occur more than once in a sample, every time at the same indent, ranked by how many samples
    /// repeat them, so a long sample in an odd format cannot flood the list.
    /// </summary>
    private static IEnumerable<string> AnchorKeys(IReadOnlyList<TextDocument> samples)
    {
        Dictionary<string, int> repeatedIn = new(StringComparer.OrdinalIgnoreCase);
        foreach (TextDocument document in samples)
        {
            IEnumerable<string> repeated = document
                .Where(static l => l.IsContent)
                .GroupBy(static l => l.Key, StringComparer.OrdinalIgnoreCase)
                .Where(static g => g.Count() > 1 && g.Select(static l => l.Indent).Distinct().Count() == 1)
                .Select(static g => g.Key);
            foreach (string key in repeated)
            {
                repeatedIn[key] = repeatedIn.GetValueOrDefault(key) + 1;
            }
        }

        return Ranked(repeatedIn);
    }

    // The candidates repeated in the most samples, ties broken by name so the choice is stable.
    private static IEnumerable<string> Ranked(Dictionary<string, int> repeatedIn) =>
        repeatedIn
            .OrderByDescending(static kv => kv.Value)
            .ThenBy(static kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(MaxAnchors)
            .Select(static kv => kv.Key);

    private static (double Score, List<List<KeyedLine>> Records, bool KeyHead) Evaluate(
        IReadOnlyList<TextDocument> samples,
        RecordBoundary boundary,
        string? anchor
    )
    {
        List<Span>[] segments = samples.Select(d => RecordSegmenter.Segment(d, boundary, anchor)).ToArray();
        bool keyHead = HeadsVary(samples, segments);
        List<List<KeyedLine>>[] perSample = new List<List<KeyedLine>>[samples.Count];
        for (int s = 0; s < samples.Count; s++)
        {
            TextDocument document = samples[s];
            perSample[s] = segments[s]
                .Select(r => RecordLines.Values(RecordLines.Parse(document, r, keyHead)).ToList())
                .Where(static k => k.Count > 0)
                .ToList();
        }

        // Each sample is judged against its own common keys, so one platform's format, or one sample that
        // splits into thousands of one-line records, does not decide what is common for the rest. Keys common
        // across the other samples count too, which is all a sample with one record has to go on.
        HashSet<string> pooled = Common(perSample.Where(static s => s.Count >= 2).SelectMany(static s => s).ToList());

        double total = 0;
        int judged = 0;
        List<List<KeyedLine>> kept = [];
        for (int s = 0; s < samples.Count; s++)
        {
            List<List<KeyedLine>> records = perSample[s];
            if (records.Sum(static r => r.Count) < MinSampleLines)
            {
                continue;
            }

            judged++;
            HashSet<string> common = Common(records);
            common.UnionWith(pooled);
            List<List<KeyedLine>> mine = Keep(records, common);
            total += SampleScore(samples[s], records, mine, common, boundary, anchor);
            kept.AddRange(mine);
        }

        // A record repeats within a sample. Sections that only line up from one sample to the next, as the two
        // halves of "show vtp status" split at a rule, are one document, not records.
        bool repeats = perSample.Any(static records => records.Count >= 2 && Common(records).Count > 0);
        if (kept.Count < 2 || judged == 0 || !repeats)
        {
            return (0, [], keyHead);
        }

        return (total / judged, kept, keyHead);
    }

    // Drops leading and trailing records unlike the rest, such as a command echo or a summary.
    private static List<List<KeyedLine>> Keep(List<List<KeyedLine>> records, HashSet<string> common)
    {
        int first = 0;
        int last = records.Count - 1;
        while (first <= last && Share(records[first], common) == 0)
        {
            first++;
        }

        while (last >= first && Share(records[last], common) < 0.5)
        {
            last--;
        }

        List<List<KeyedLine>> kept = [];
        for (int r = first; r <= last; r++)
        {
            kept.Add(r == last ? TrimTrailer(records[r], common) : records[r]);
        }

        return kept;
    }

    private static double SampleScore(
        TextDocument document,
        List<List<KeyedLine>> records,
        List<List<KeyedLine>> kept,
        HashSet<string> common,
        RecordBoundary boundary,
        string? anchor
    )
    {
        if (kept.Count == 0)
        {
            return 0;
        }

        int all = records.Sum(static r => r.Count);
        int covered = kept.Sum(static r => r.Count);
        int tops = kept.Sum(static r => Top(r).Count());
        double share = kept.Sum(r => Share(r, common) * Top(r).Count()) / Math.Max(1, tops);
        double multiLine = kept.Count(static r => r.Count > 1) / (double)kept.Count;

        // Starting records at any key that appears once per record gives records that resemble each other as
        // well as the true split does. The true first line is usually set off by a blank line, a rule, or the
        // start of the text, so an anchor scores by how often it is.
        double anchored = 1;
        if (boundary == RecordBoundary.Anchor)
        {
            // In flat output nothing sets records off, but a rotated anchor leaves the true first lines of the
            // first record ahead of it, where the true one has at most a command echo.
            int setOff = kept.Count(r => r[0].FirstLine == 0 || !document[r[0].FirstLine - 1].IsContent);
            int ahead = document.TakeWhile(l => !string.Equals(l.Key, anchor, StringComparison.OrdinalIgnoreCase)).Count(static l => l.IsContent);
            anchored = ahead <= 1 ? 1 : 0.5 + (0.5 * setOff / kept.Count);
        }

        // A phrase can rotate records too, but flat output such as Dell's sets nothing off at all, so the true
        // start only gets a nudge: set off by a blank line or rule, or heading deeper lines below it.
        if (boundary == RecordBoundary.Phrase)
        {
            int setOff = kept.Count(r =>
            {
                int at = r[0].FirstLine;
                bool before = at == 0 || !document[at - 1].IsContent;
                bool heads = at + 1 < document.Count && document[at + 1].IsContent && document[at + 1].Indent > document[at].Indent;
                return before || heads;
            });
            anchored = 0.9 + (0.1 * setOff / kept.Count);
        }

        // Several records run together repeat nearly all their keys. One record can repeat a few, as an access
        // list repeats its rule lines, so the cost grows with the share of keys repeated, not with the repeats.
        int keysSeen = 0;
        int keysRepeated = 0;
        foreach (List<KeyedLine> record in kept)
        {
            List<IGrouping<string, KeyedLine>> byKey = Top(record).GroupBy(static k => k.Path, StringComparer.OrdinalIgnoreCase).ToList();
            keysSeen += byKey.Count;
            keysRepeated += byKey.Count(static g => g.Count() > 1);
        }

        double repeatedShare = keysSeen == 0 ? 1 : keysRepeated / (double)keysSeen;
        double distinct = 1 - (repeatedShare * repeatedShare);

        // A rule line marks a boundary, so a split that leaves one inside a record put the boundary elsewhere.
        // A rule indented deeper than the record's first line underlines a heading inside it, and does not count.
        int ruled = kept.Count(r =>
        {
            for (int i = r[0].FirstLine + 1; i < r[^1].LastLine; i++)
            {
                if (document[i].Kind == LineKind.Rule && document[i].Indent <= r[0].Line.Indent)
                {
                    return true;
                }
            }

            return false;
        });
        double unruled = 1 - (ruled / (double)kept.Count);

        // Where records indent their bodies, a line back at the first line's indent starts the next record, so a
        // split that leaves one inside a record merged two. Flat output, which indents nothing, is not judged.
        double unmerged = 1;
        if (boundary is RecordBoundary.Anchor or RecordBoundary.Phrase)
        {
            int deeper = 0;
            int body = 0;
            int merged = 0;
            foreach (List<KeyedLine> record in kept)
            {
                int head = record[0].Line.Indent;
                bool swallowed = false;
                for (int i = record[0].FirstLine + 1; i <= record[^1].LastLine; i++)
                {
                    if (document[i].IsContent)
                    {
                        body++;
                        deeper += document[i].Indent > head ? 1 : 0;
                        swallowed |= document[i].Indent <= head;
                    }
                }

                merged += swallowed ? 1 : 0;
            }

            if (deeper * 2 > body)
            {
                unmerged = 1 - (merged / (double)kept.Count);
            }
        }

        return share * covered / all * multiLine * anchored * distinct * unruled * unmerged;
    }

    // A record starts with a head line, keyed by its position rather than its words, when its first lines
    // mostly have different keys, like interface names, or when they are not "key: value" lines and differ
    // from record to record, as "TenGigabitEthernet 0/1 is up" and "TenGigabitEthernet 0/2 is down" do.
    private static bool HeadsVary(IReadOnlyList<TextDocument> samples, List<Span>[] segments)
    {
        Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> distinct = new(StringComparer.OrdinalIgnoreCase);
        int records = 0;
        int plain = 0;
        for (int s = 0; s < samples.Count; s++)
        {
            foreach (Span r in segments[s])
            {
                TextLine? first = null;
                for (int i = r.Start; i < r.End && first is null; i++)
                {
                    first = samples[s][i].IsContent ? samples[s][i] : null;
                }

                if (first is not null)
                {
                    string key = first.Key;
                    counts[key] = counts.GetValueOrDefault(key) + 1;
                    distinct.Add(first.Text.Trim());
                    plain += key.EndsWith(':') || key.EndsWith('=') ? 0 : 1;
                    records++;
                }
            }
        }

        return records >= 2
            && (counts.Values.Max() * 2 < records || (plain * 5 >= records * 4 && distinct.Count * 2 > records));
    }

    // The outer lines of a record, less its head, which every record has by construction.
    private static IEnumerable<KeyedLine> Top(List<KeyedLine> record) =>
        record.Where(static k => !k.IsNested && k.Path != RecordLines.Head);

    // Keys in at least two records and at least half of them. Only records of more than one line are counted
    // in that half, since titles and footers between records split off as one-line stretches of their own.
    private static HashSet<string> Common(List<List<KeyedLine>> records)
    {
        Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
        foreach (List<KeyedLine> record in records)
        {
            foreach (string key in Top(record).Select(static k => k.Path).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
        }

        int multiLine = Math.Max(2, records.Count(static r => r.Count > 1));
        return counts
            .Where(c => c.Value >= 2 && c.Value * 2 >= multiLine)
            .Select(static c => c.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static double Share(List<KeyedLine> record, HashSet<string> common)
    {
        List<KeyedLine> top = Top(record).ToList();
        return top.Count == 0 ? 0 : top.Count(k => common.Contains(k.Path)) / (double)top.Count;
    }

    // A summary line such as "Total cdp entries displayed : 4" often follows the last record with no rule
    // before it. Drops trailing outer lines whose keys no other record has.
    private static List<KeyedLine> TrimTrailer(List<KeyedLine> record, HashSet<string> common)
    {
        int end = record.Count;
        while (end > 1 && !record[end - 1].IsNested && !common.Contains(record[end - 1].Path))
        {
            end--;
        }

        return end == record.Count ? record : record.GetRange(0, end);
    }
}
