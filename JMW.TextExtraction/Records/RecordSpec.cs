namespace JMW.TextExtraction.Records;

/// <summary>
/// How to read repeated records: where one ends and the next begins, and a template for each kind of line.
/// Inferred by <see cref="RecordInference"/>, applied by <see cref="RecordExtractor"/>, and meant to be read
/// and corrected by a person in between.
/// </summary>
/// <param name="Anchor">The key that starts each record, for <see cref="RecordBoundary.Anchor"/>.</param>
/// <param name="KeyHead">
/// Whether each record's first line is keyed as <see cref="RecordLines.Head"/> rather than by its words, for
/// records that start with a name, such as an interface, instead of a fixed key.
/// </param>
/// <param name="Lines">One template per line key, in the order the keys first appear.</param>
public sealed record RecordSpec(RecordBoundary Boundary, string? Anchor, bool KeyHead, IReadOnlyList<LineTemplate> Lines)
{
    public override string ToString()
    {
        string head = this.Boundary switch
        {
            RecordBoundary.Anchor => "records start at key \"" + this.Anchor + "\"",
            RecordBoundary.Phrase => "records start at lines containing \"" + this.Anchor + "\"",
            _ => "records split by " + this.Boundary,
        };
        return head + (this.KeyHead ? ", first line is the head" : "") + Environment.NewLine
            + string.Join(Environment.NewLine, this.Lines.Select(static l => "  " + l));
    }
}

/// <summary>A spec and the indices of the samples it was inferred from.</summary>
public sealed record FormatSpec(RecordSpec Spec, IReadOnlyList<int> Samples);

public static class RecordExtractor
{
    /// <summary>
    /// Reads records with whichever spec accounts for the text best: the most fields found, less the lines
    /// left unmatched. Returns null when no spec finds a record.
    /// </summary>
    public static (RecordSpec Spec, List<ExtractedRecord> Records)? ExtractBest(IEnumerable<RecordSpec> specs, string text) =>
        ExtractBest(specs, TextDocument.Parse(text));

    public static (RecordSpec Spec, List<ExtractedRecord> Records)? ExtractBest(IEnumerable<RecordSpec> specs, TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(specs);
        (RecordSpec Spec, List<ExtractedRecord> Records)? best = null;
        int bestScore = 0;
        foreach (RecordSpec spec in specs)
        {
            List<ExtractedRecord> records = Extract(spec, document);
            int score = Score(records);
            if (best is null || score > bestScore)
            {
                best = (spec, records);
                bestScore = score;
            }
        }

        return best is { Records.Count: > 0 } ? best : null;
    }

    /// <summary>
    /// Like <see cref="ExtractBest(IEnumerable{RecordSpec}, TextDocument)"/>, but also infers a spec from the
    /// text itself and uses it when it accounts for more, so output in a format none of the specs has seen
    /// still gets read.
    /// </summary>
    public static (RecordSpec Spec, List<ExtractedRecord> Records)? ExtractOrInfer(IEnumerable<RecordSpec> specs, string text) =>
        ExtractOrInfer(specs, TextDocument.Parse(text));

    public static (RecordSpec Spec, List<ExtractedRecord> Records)? ExtractOrInfer(IEnumerable<RecordSpec> specs, TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(specs);
        (RecordSpec Spec, List<ExtractedRecord> Records)? known = ExtractBest(specs, document);
        if (RecordInference.Infer([document]) is not { } own)
        {
            return known;
        }

        List<ExtractedRecord> records = Extract(own, document);
        return known is null || Score(records) > Score(known.Value.Records) ? (own, records) : known;
    }

    private static int Score(List<ExtractedRecord> records) => records.Sum(static r => r.Fields.Count - r.Unmatched.Count);

    public static List<ExtractedRecord> Extract(RecordSpec spec, string text) => Extract(spec, TextDocument.Parse(text));

    /// <summary>
    /// Reads records from a document with a spec. A stretch with fewer than half its lines known to the spec,
    /// such as a command echo or a summary line, is not a record and is skipped.
    /// </summary>
    public static List<ExtractedRecord> Extract(RecordSpec spec, TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(document);
        TemplateSet templates = new(spec.Lines);
        List<ExtractedRecord> records = [];
        foreach (Span range in RecordSegmenter.Segment(document, spec.Boundary, spec.Anchor))
        {
            List<KeyedLine> keyed = RecordLines.Values(RecordLines.Parse(document, range, spec.KeyHead)).ToList();
            int known = keyed.Count(k => templates.KeyOf(k) is not null);

            // A lone head line, such as a command echo above records that start with a name, is not a record.
            bool headOnly = spec.KeyHead && keyed.Count == 1 && spec.Lines.Any(static l => l.Key != RecordLines.Head);
            if (known == 0 || known * 2 < keyed.Count || headOnly)
            {
                continue;
            }

            List<ExtractedField> fields = [];
            List<int> unmatched = [];
            foreach (KeyedLine line in keyed)
            {
                if (!templates.Read(line, fields))
                {
                    unmatched.Add(line.FirstLine);
                }
            }

            if (fields.Count > 0)
            {
                records.Add(new ExtractedRecord(range.Start, range.End - 1, fields, unmatched));
            }
        }

        return records;
    }
}
