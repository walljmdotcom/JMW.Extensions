namespace JMW.TextExtraction.Records;

/// <summary>
/// Line templates looked up by key, and the reading of keyed lines with them. Records and single documents
/// both read their lines this way.
/// </summary>
public sealed class TemplateSet
{
    private readonly Dictionary<string, List<LineTemplate>> byKey = new(StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyList<LineTemplate> lines;

    public TemplateSet(IReadOnlyList<LineTemplate> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        this.lines = lines;
        foreach (LineTemplate line in lines)
        {
            (this.byKey.TryGetValue(line.Key, out List<LineTemplate>? variants) ? variants : this.byKey[line.Key] = []).Add(line);
        }
    }

    public bool Knows(string key) => this.byKey.ContainsKey(key);

    /// <summary>
    /// The key the set knows a line by: its path, or for a line that opens with a name, the key of the words
    /// after the name. Null when the set knows neither.
    /// </summary>
    public string? KeyOf(KeyedLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        if (this.byKey.ContainsKey(line.Path))
        {
            return line.Path;
        }

        return line.Line.NameLedKey is { } named && this.byKey.ContainsKey(Rekey(line.Path, named)) ? Rekey(line.Path, named) : null;
    }

    /// <summary>
    /// Reads one keyed line into fields with the variant that fits it best. Returns false when no template
    /// accounts for the line. A group key with nothing under it this time, as "Management address(es):" for a
    /// neighbor that sent none, counts as read: the group is empty.
    /// </summary>
    public bool Read(KeyedLine line, List<ExtractedField> fields)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(fields);
        if (this.KeyOf(line) is not { } key)
        {
            return line.Text is null
                && line.Tokens.Count > 0
                && LineKeys.SeparatorIndex(line.Tokens) == line.Tokens.Count - 1
                && this.lines.Any(l => l.Key.StartsWith(line.Path + RecordLines.PathSeparator, StringComparison.OrdinalIgnoreCase));
        }

        List<LineTemplate> variants = this.byKey[key];
        if (variants[0].IsBlock)
        {
            fields.Add(new ExtractedField(variants[0].Fields.Single(), line.Text ?? ""));
            return true;
        }

        int before = fields.Count;
        if (!variants.OrderByDescending(v => v.Fit(line.Line)).First().TryMatch(line.Line, fields))
        {
            return false;
        }

        // A value that wraps onto deeper lines carries on in the line's last field.
        if (line.Continuation is { } more && fields.Count > before)
        {
            fields[^1] = fields[^1] with { Value = fields[^1].Value + " " + more };
        }

        return true;
    }

    /// <summary>The path with its last key replaced, for a line found by the words after its name.</summary>
    public static string Rekey(string path, string key)
    {
        ArgumentNullException.ThrowIfNull(path);
        int group = path.LastIndexOf(RecordLines.PathSeparator, StringComparison.Ordinal);
        return group < 0 ? key : path[..(group + RecordLines.PathSeparator.Length)] + key;
    }
}
