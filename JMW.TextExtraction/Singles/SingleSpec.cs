using JMW.TextExtraction.Records;

namespace JMW.TextExtraction.Singles;

/// <summary>
/// How to read output that is one record per sample, such as <c>show version</c> or <c>show ntp status</c>:
/// a template for each line worth reading. Nothing repeats within such output, so its lines are found by
/// comparing samples, and a line whose words never change is still a template, one with no slots, so it is
/// recognized and passed over.
/// </summary>
public sealed record SingleSpec(IReadOnlyList<LineTemplate> Lines)
{
    public override string ToString() =>
        "single" + Environment.NewLine + string.Join(Environment.NewLine, this.Lines.Select(static l => "  " + l));
}

public static class SingleInference
{
    private const int MinShared = 2;

    public static SingleSpec? Infer(params string[] samples) => Infer(samples.Select(TextDocument.Parse).ToList());

    /// <summary>
    /// Infers a spec from samples of the same command, each read as one record. A line is identified by its
    /// key; a line that opens with a name, as <c>dist-sw1 uptime is 2 years</c>, is identified by the words
    /// after the name when its own key turns up in only one sample and those words turn up in several. With
    /// one sample there is nothing to compare, so only its <c>key: value</c> lines are kept.
    /// </summary>
    public static SingleSpec? Infer(IReadOnlyList<TextDocument> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        Dictionary<string, int> keyIn = SamplesHolding(samples, static l => l.Key);
        Dictionary<string, int> nameIn = SamplesHolding(samples, static l => l.NameLedKey);
        string KeyOf(TextLine line) =>
            keyIn.GetValueOrDefault(line.Key) < MinShared && line.NameLedKey is { } named && nameIn.GetValueOrDefault(named) >= MinShared
                ? named
                : line.Key;

        List<List<KeyedLine>> records = samples
            .Select(d => RecordLines.Values(RecordLines.Parse(d, new Span(0, d.Count), keyHead: false, KeyOf)).ToList())
            .Where(static r => r.Count > 0)
            .ToList();
        List<LineTemplate> templates = records.Count == 0 ? [] : RecordInference.Templates(records);
        return templates.Count == 0 ? null : new SingleSpec(templates);
    }

    private static Dictionary<string, int> SamplesHolding(IReadOnlyList<TextDocument> samples, Func<TextLine, string?> key)
    {
        Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);
        foreach (TextDocument document in samples)
        {
            foreach (string k in document.Where(static l => l.IsContent).Select(key).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
            {
                counts[k] = counts.GetValueOrDefault(k) + 1;
            }
        }

        return counts;
    }
}

public static class SingleExtractor
{
    public static ExtractedRecord? Extract(SingleSpec spec, string text) => Extract(spec, TextDocument.Parse(text));

    /// <summary>
    /// Reads the document as one record. Returns null when no line of it fits the spec. Lines the spec does not
    /// account for are listed in <see cref="ExtractedRecord.Unmatched"/>.
    /// </summary>
    public static ExtractedRecord? Extract(SingleSpec spec, TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(document);
        TemplateSet templates = new(spec.Lines);
        List<KeyedLine> keyed = RecordLines.Values(RecordLines.Parse(document, new Span(0, document.Count), keyHead: false)).ToList();
        List<ExtractedField> fields = [];
        List<int> unmatched = [];
        int read = 0;
        foreach (KeyedLine line in keyed)
        {
            if (templates.Read(line, fields))
            {
                read++;
            }
            else
            {
                unmatched.Add(line.FirstLine);
            }
        }

        if (read == 0)
        {
            return null;
        }

        int first = keyed.Min(static k => k.FirstLine);
        int last = keyed.Max(static k => k.LastLine);
        return new ExtractedRecord(first, last, fields, unmatched);
    }
}
