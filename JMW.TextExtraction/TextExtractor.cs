using System.Text.Json;
using System.Text.Json.Nodes;
using JMW.TextExtraction.Records;
using JMW.TextExtraction.Singles;
using JMW.TextExtraction.Tables;

namespace JMW.TextExtraction;

/// <summary>
/// Everything known about one command's output: the kinds of table it prints, a record spec for each format of
/// its repeated records, and a spec for the rest read as one record per sample. Write it out with
/// <see cref="SpecText"/> to read and correct it.
/// </summary>
public sealed record DocumentSpec(IReadOnlyList<TableSpec> Tables, IReadOnlyList<RecordSpec> Records, SingleSpec? Single = null)
{
    public static DocumentSpec Empty { get; } = new([], []);
}

/// <param name="Records">Repeated records read outside the tables, by <paramref name="RecordSpec"/>.</param>
/// <param name="Single">The rest of the document read as one record, when the spec says how.</param>
/// <param name="Unclaimed">
/// Lines with content that nothing accounts for, including lines inside a record that its spec could not
/// match. An empty list means every line was read.
/// </param>
public sealed record ExtractedDocument(
    IReadOnlyList<ExtractedTable> Tables,
    RecordSpec? RecordSpec,
    IReadOnlyList<ExtractedRecord> Records,
    ExtractedRecord? Single,
    IReadOnlyList<int> Unclaimed
)
{
    /// <summary>
    /// The document as JSON: <c>tables</c> as arrays of row objects, <c>records</c> as an array of objects,
    /// <c>single</c> as an object or null, and <c>unclaimed</c> as zero-based line numbers. Each object is shaped
    /// as <see cref="ExtractedRecord.ToDictionary"/> shapes it.
    /// </summary>
    public string ToJson(bool indented = true)
    {
        JsonObject json = new()
        {
            ["tables"] = new JsonArray([.. this.Tables.Select(static t => (JsonNode?)new JsonArray([.. t.Rows.Select(static r => (JsonNode?)r.ToJson())]))]),
            ["records"] = new JsonArray([.. this.Records.Select(static r => (JsonNode?)r.ToJson())]),
            ["single"] = this.Single?.ToJson(),
            ["unclaimed"] = new JsonArray([.. this.Unclaimed.Select(static i => (JsonNode?)JsonValue.Create(i))]),
        };
        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = indented });
    }
}

/// <summary>
/// The whole pipeline over a document: tables first, then repeated records in the lines the tables leave, then
/// what is left as one record.
/// </summary>
public static class TextExtractor
{
    /// <summary>Infers a spec from one or more samples.</summary>
    public static DocumentSpec Infer(params string[] samples) => Infer((IReadOnlyList<string>)samples);

    /// <summary>Infers the tables, record formats, and single-record lines of one command from samples of its output.</summary>
    public static DocumentSpec Infer(IReadOnlyList<string> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return Infer(samples.Select(TextDocument.Parse).ToList());
    }

    public static DocumentSpec Infer(IReadOnlyList<TextDocument> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        List<IReadOnlyList<DetectedTable>> detected = samples.Select(static d => TableDetector.Detect(d)).ToList();
        List<TableSpec> tables = TableInference.Infer(detected, samples.Count);

        // Each stage looks only at what the stages before it left, so a table's rows do not become record lines
        // and a record's lines do not become a single document's.
        List<TextDocument> rest = samples.Select((d, s) => d.Without(detected[s].Select(static t => new Span(t.FirstLine, t.LastLine + 1)))).ToList();
        List<RecordSpec> records = RecordInference.InferFormats(rest).Select(static f => f.Spec).ToList();
        List<TextDocument> left = rest.Select(d => d.Without(Claimed(RecordExtractor.ExtractBest(records, d)?.Records ?? []))).ToList();
        return new DocumentSpec(tables, records, SingleInference.Infer(left));
    }

    public static ExtractedDocument Extract(DocumentSpec spec, string text, bool inferUnknown = true) =>
        Extract(spec, TextDocument.Parse(text), inferUnknown);

    /// <summary>
    /// Reads a document with a spec. With <paramref name="inferUnknown"/>, tables and record formats the spec
    /// does not know are still read, with specs inferred from the document itself.
    /// </summary>
    public static ExtractedDocument Extract(DocumentSpec spec, TextDocument document, bool inferUnknown = true)
    {
        ArgumentNullException.ThrowIfNull(spec);
        ArgumentNullException.ThrowIfNull(document);
        List<ExtractedTable> tables = TableExtractor.Extract(spec.Tables, document, inferUnknown);
        TextDocument rest = document.Without(tables.Select(static t => new Span(t.FirstLine, t.LastLine + 1)));

        (RecordSpec Spec, List<ExtractedRecord> Records)? read = inferUnknown
            ? RecordExtractor.ExtractOrInfer(spec.Records, rest)
            : RecordExtractor.ExtractBest(spec.Records, rest);
        List<ExtractedRecord> records = read?.Records ?? [];

        TextDocument left = rest.Without(Claimed(records));
        SingleSpec? singleSpec = spec.Single ?? (inferUnknown ? SingleInference.Infer([left]) : null);
        ExtractedRecord? single = singleSpec is null ? null : SingleExtractor.Extract(singleSpec, left);

        bool[] claimed = new bool[document.Count];
        foreach (ExtractedTable table in tables)
        {
            Array.Fill(claimed, true, table.FirstLine, table.LastLine - table.FirstLine + 1);
        }

        foreach (ExtractedRecord record in single is null ? records : [.. records, single])
        {
            Array.Fill(claimed, true, record.FirstLine, record.LastLine - record.FirstLine + 1);
            foreach (int line in record.Unmatched)
            {
                claimed[line] = false;
            }
        }

        List<int> unclaimed = document.Where(l => !claimed[l.Index] && l.IsContent).Select(static l => l.Index).ToList();
        return new ExtractedDocument(tables, read?.Spec, records, single, unclaimed);
    }

    // The lines records cover, less the ones they could not read, which are left for what comes after.
    private static IEnumerable<Span> Claimed(IEnumerable<ExtractedRecord> records)
    {
        foreach (ExtractedRecord record in records)
        {
            HashSet<int> unmatched = [.. record.Unmatched];
            for (int i = record.FirstLine; i <= record.LastLine; i++)
            {
                if (!unmatched.Contains(i))
                {
                    yield return new Span(i, i + 1);
                }
            }
        }
    }
}
