namespace JMW.TextExtraction.Tables;

/// <summary>A column as a run of data positions plus the header words that name it.</summary>
public sealed record ColumnGroup(Span Data, IReadOnlyList<Span> Words);

public static class HeaderAlignment
{
    /// <summary>
    /// Joins data spans into columns using header words. A word over several spans joins them. A word over
    /// none goes with the word it is single-spaced to, as "Age" goes with "(min)", or else to the nearest span.
    /// Spans left under no word join whichever neighbor sits closer. With no words, every span is its own column.
    /// </summary>
    /// <remarks>
    /// This makes multi-word names, right-aligned numbers, and cells with spaces in them come out as one
    /// column each. A header word over a column that is empty in every row cannot be told from the second
    /// word of its neighbor's name, so it joins the neighbor.
    /// </remarks>
    public static List<ColumnGroup> Align(IReadOnlyList<Span> spans, IReadOnlyList<Span>? words)
    {
        ArgumentNullException.ThrowIfNull(spans);
        if (spans.Count == 0)
        {
            return [];
        }

        if (words is null)
        {
            return spans.Select(static s => new ColumnGroup(s, [])).ToList();
        }

        spans = SplitAtPhrases(spans, words);

        // joined[i] means span i and span i + 1 belong to one column.
        bool[] joined = new bool[spans.Count];
        List<Span>[] owned = new List<Span>[spans.Count];
        for (int i = 0; i < owned.Length; i++)
        {
            owned[i] = [];
        }

        (int First, int Last)[] covered = words.Select(w => Covered(spans, w)).ToArray();
        for (int w = 0; w < words.Count; w++)
        {
            (int first, int last) = covered[w];
            if (first < 0)
            {
                bool right = w + 1 < words.Count && words[w + 1].Start - words[w].End == 1 && covered[w + 1].First >= 0;
                bool left = w > 0 && words[w].Start - words[w - 1].End == 1 && covered[w - 1].Last >= 0;
                first = right ? covered[w + 1].First : left ? covered[w - 1].Last : Nearest(spans, words[w]);
                last = first;
            }

            owned[first].Add(words[w]);
            for (int i = first; i < last; i++)
            {
                joined[i] = true;
            }
        }

        List<(int First, int Last, List<Span> Words)> runs = [];
        for (int i = 0; i < spans.Count; i++)
        {
            if (i > 0 && joined[i - 1])
            {
                (int first, _, List<Span> ws) = runs[^1];
                ws.AddRange(owned[i]);
                runs[^1] = (first, i, ws);
            }
            else
            {
                runs.Add((i, i, [.. owned[i]]));
            }
        }

        for (int r = 0; r < runs.Count && runs.Count > 1; r++)
        {
            if (runs[r].Words.Count > 0)
            {
                continue;
            }

            int leftGap = r > 0 ? spans[runs[r].First].Start - spans[runs[r - 1].Last].End : int.MaxValue;
            int rightGap = r + 1 < runs.Count ? spans[runs[r + 1].First].Start - spans[runs[r].Last].End : int.MaxValue;

            // Row labels under no name, as "Mem:" and "Swap:" in "free", are a column of their own.
            if (r == 0 && rightGap >= 2 && words.Count > 0 && spans[runs[r].Last].End < words[0].Start)
            {
                continue;
            }
            if (leftGap <= rightGap)
            {
                runs[r - 1] = (runs[r - 1].First, runs[r].Last, runs[r - 1].Words);
            }
            else
            {
                runs[r + 1] = (runs[r].First, runs[r + 1].Last, runs[r + 1].Words);
            }

            runs.RemoveAt(r);
            r--;
        }

        List<ColumnGroup> groups = new(runs.Count);
        foreach ((int first, int last, List<Span> ws) in runs)
        {
            ws.Sort(static (x, y) => x.Start.CompareTo(y.Start));
            groups.Add(new ColumnGroup(new Span(spans[first].Start, spans[last].End), ws));
        }

        return groups;
    }

    private static (int First, int Last) Covered(IReadOnlyList<Span> spans, Span word)
    {
        int first = -1;
        int last = -1;
        for (int i = 0; i < spans.Count; i++)
        {
            if (spans[i].Overlaps(word))
            {
                if (first < 0)
                {
                    first = i;
                }

                last = i;
            }
        }

        return (first, last);
    }

    /// <summary>
    /// Header names padded apart by two or more spaces are separate columns whatever the data shows. Splits a
    /// span that two names share at the start of the later name, as when a value truncated to its column width
    /// runs into the next column. Adds the name's own extent as a span when no data sits under it, as for a
    /// column that is empty in every row.
    /// </summary>
    public static List<Span> SplitAtPhrases(IReadOnlyList<Span> spans, IReadOnlyList<Span> words)
    {
        ArgumentNullException.ThrowIfNull(spans);
        ArgumentNullException.ThrowIfNull(words);
        List<Span> phrases = [];
        foreach (Span w in words)
        {
            if (phrases.Count > 0 && w.Start - phrases[^1].End <= 1)
            {
                phrases[^1] = phrases[^1] with { End = w.End };
            }
            else
            {
                phrases.Add(w);
            }
        }

        List<Span> result = [.. spans];
        for (int p = 1; p < phrases.Count; p++)
        {
            int cut = phrases[p].Start;
            int s = result.FindIndex(x => x.Start < cut && cut < x.End && x.Overlaps(phrases[p - 1]));
            if (s >= 0)
            {
                Span span = result[s];
                result[s] = span with { End = cut };
                result.Insert(s + 1, span with { Start = cut });
            }
        }

        foreach (Span phrase in phrases)
        {
            if (!result.Exists(x => x.Overlaps(phrase)))
            {
                int at = result.FindIndex(x => x.Start > phrase.Start);
                result.Insert(at < 0 ? result.Count : at, phrase);
            }
        }

        return result;
    }

    private static int Nearest(IReadOnlyList<Span> spans, Span word)
    {
        int best = 0;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < spans.Count; i++)
        {
            int distance = spans[i].DistanceTo(word);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return best;
    }
}
