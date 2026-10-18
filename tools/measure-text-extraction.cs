#:project ../JMW.TextExtraction/JMW.TextExtraction.csproj

// Measures JMW.TextExtraction against corpora that stay outside this repository.
//
//   dotnet run tools/measure-text-extraction.cs -- golden <folder> [command path...] [--verbose]
//   dotnet run tools/measure-text-extraction.cs -- coverage <folder> [min samples] [max samples]
//
// golden: <folder> holds one folder per command path (show/vlan/brief), each with input_*.json (raw output)
// and output_*.json (the values a hand-written parser read). For every command, infers a spec from its inputs,
// reads each input, and sorts the expected values by how they relate to what was read: exact, coarser (ours
// holds more), finer (theirs holds more), missed, or derived (not in the text at all).
//
// coverage: <folder> holds one folder per device with one file per command, named for the command. For every
// command with enough samples, infers a spec from them, reads each, and reports the content lines nothing read.
//
// Each command prints as soon as it finishes, worst first within the summary.

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using JMW.TextExtraction;
using JMW.TextExtraction.Text;

if (args.Length < 2)
{
    Console.WriteLine("usage: golden <folder> [command path...] | coverage <folder> [min samples] [max samples]");
    return 1;
}

return args[0] switch
{
    "golden" => Golden(args[1], args.Skip(2).Where(static a => a != "--verbose").ToList(), args.Contains("--verbose")),
    "coverage" => Coverage(
        args[1],
        args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 5,
        args.Length > 3 ? int.Parse(args[3], CultureInfo.InvariantCulture) : 40
    ),
    _ => throw new ArgumentException("expected golden or coverage"),
};

static int Golden(string root, List<string> commands, bool verbose)
{
    if (commands.Count == 0)
    {
        commands = Directory.GetFiles(root, "input_*.json", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, Path.GetDirectoryName(f) ?? root).Replace('\\', '/'))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    long exact = 0, coarser = 0, finer = 0, missed = 0, derived = 0;
    foreach (string command in commands)
    {
        string dir = Path.Combine(root, command);
        List<(string Name, string Input, List<string> Expected)> pairs = [];
        foreach (string input in Directory.GetFiles(dir, "input_*.json").Order(StringComparer.Ordinal))
        {
            string output = Path.Combine(dir, "output_" + Path.GetFileName(input)["input_".Length..]);
            if (File.Exists(output))
            {
                pairs.Add((Path.GetFileName(input), Input(input), Expected(output)));
            }
        }

        DocumentSpec spec = TextExtractor.Infer(pairs.Select(static p => p.Input).ToList());
        (int e, int c, int f, int m, int d) = (0, 0, 0, 0, 0);
        List<string> examples = [];
        Dictionary<string, int> missedIn = new(StringComparer.Ordinal);
        foreach ((string name, string input, List<string> expected) in pairs)
        {
            // Each vendor's format often appears in one golden input only, so formats the spec does not cover
            // are read with specs inferred from the input itself, as an application would for unfamiliar output.
            HashSet<string> ours = Values(TextExtractor.Extract(spec, input, inferUnknown: true));
            foreach (string value in expected.Where(static v => v.Length >= 3))
            {
                if (!ContainsWords(input, value))
                {
                    d++;
                }
                else if (ours.Contains(value))
                {
                    e++;
                }
                else if (ours.Any(o => ContainsWords(o, value)))
                {
                    c++;
                }
                else if (ours.Any(o => o.Length >= 3 && ContainsWords(value, o)))
                {
                    f++;
                }
                else
                {
                    m++;
                    missedIn[name] = missedIn.GetValueOrDefault(name) + 1;
                    if (verbose && missedIn[name] <= 4)
                    {
                        Console.WriteLine($"          missed in {name}: {value}");
                    }
                    if (examples.Count < 3)
                    {
                        examples.Add(name + ": " + value);
                    }
                }
            }
        }

        int inText = e + c + f + m;
        Console.WriteLine($"{Share(m, inText),7} missed {Share(e, inText),7} exact {inText,7} values  {command}  {string.Join(" | ", examples)}");
        if (verbose)
        {
            foreach ((string name, int count) in missedIn.OrderByDescending(static kv => kv.Value).Take(5))
            {
                Console.WriteLine($"          {count,6} missed in {name}");
            }
        }
        (exact, coarser, finer, missed, derived) = (exact + e, coarser + c, finer + f, missed + m, derived + d);
    }

    long all = exact + coarser + finer + missed;
    Console.WriteLine($"=== {commands.Count} commands, {all} values in text (+{derived} derived): exact {Share(exact, all)}, coarser {Share(coarser, all)}, finer {Share(finer, all)}, missed {Share(missed, all)}");
    return 0;
}

static int Coverage(string root, int minSamples, int maxSamples)
{
    List<IGrouping<string, string>> commands = Directory.GetFiles(root, "*", SearchOption.AllDirectories)
        .Where(static f => Path.GetExtension(f) is not (".json" or ".tsv" or ".txt"))
        .GroupBy(static f => Path.GetFileName(f).Replace('_', ' ').Trim(), StringComparer.OrdinalIgnoreCase)
        .Where(g => g.Count() >= minSamples)
        .OrderByDescending(static g => g.Count())
        .ToList();

    long content = 0, unclaimed = 0;
    foreach (IGrouping<string, string> command in commands)
    {
        Stopwatch sw = Stopwatch.StartNew();
        List<TextDocument> documents = command.Order(StringComparer.Ordinal).Take(maxSamples).Select(static f => TextDocument.Parse(File.ReadAllText(f))).ToList();
        DocumentSpec spec = TextExtractor.Infer(documents);
        int c = 0, u = 0, rows = 0, records = 0;
        foreach (TextDocument document in documents)
        {
            ExtractedDocument read = TextExtractor.Extract(spec, document, inferUnknown: false);
            c += document.Count(static l => l.IsContent);
            u += read.Unclaimed.Count;
            rows += read.Tables.Sum(static t => t.Rows.Count);
            records += read.Records.Count;
        }

        Console.WriteLine($"{Share(u, c),7} unclaimed {documents.Count,4} samples {c,8} lines {rows,7} rows {records,6} records {sw.ElapsedMilliseconds,6} ms  {command.Key}");
        content += c;
        unclaimed += u;
    }

    Console.WriteLine($"=== {commands.Count} commands, {content} content lines, unclaimed {Share(unclaimed, content)}");
    return 0;
}

static HashSet<string> Values(ExtractedDocument document)
{
    HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);
    IEnumerable<ExtractedRecord> single = document.Single is { } one ? [one] : [];
    foreach (ExtractedRecord record in document.Tables.SelectMany(static t => t.Rows).Concat(document.Records).Concat(single))
    {
        foreach (ExtractedField field in record.Fields)
        {
            values.Add(field.Value.Trim());
            if (field.Qualifier is not null)
            {
                values.Add(field.Qualifier);
            }

            if (field.Unit is not null)
            {
                values.Add(field.Unit);
                values.Add(field.Value.Trim() + " " + field.Unit);
            }
        }
    }

    return values;
}

static string Input(string path)
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
    return doc.RootElement.TryGetProperty("commandInput", out JsonElement ci) ? ci.GetString() ?? "" : "";
}

static List<string> Expected(string path)
{
    using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
    List<string> values = [];
    if (!doc.RootElement.TryGetProperty("commandOutput", out JsonElement outputs) || outputs.ValueKind != JsonValueKind.Array)
    {
        return values;
    }

    foreach (JsonElement output in outputs.EnumerateArray())
    {
        if (!output.TryGetProperty("relatedEntities", out JsonElement entities) || entities.ValueKind != JsonValueKind.Array)
        {
            continue;
        }

        foreach (JsonElement entity in entities.EnumerateArray())
        {
            foreach (JsonProperty property in entity.GetProperty("properties").EnumerateObject())
            {
                values.Add(((property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() : property.Value.ToString()) ?? "").Trim());
            }
        }
    }

    return values;
}

static string Share(long part, long whole) => whole == 0 ? "-" : (part / (double)whole).ToString("P1", CultureInfo.InvariantCulture);

static bool ContainsWords(string text, string value)
{
    int at = text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
    while (at >= 0)
    {
        bool left = at == 0 || !char.IsLetterOrDigit(text[at - 1]);
        bool right = at + value.Length == text.Length || !char.IsLetterOrDigit(text[at + value.Length]);
        if (left && right)
        {
            return true;
        }

        at = text.IndexOf(value, at + 1, StringComparison.OrdinalIgnoreCase);
    }

    return false;
}
