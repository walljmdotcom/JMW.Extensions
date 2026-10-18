using JMW.TextExtraction.Records;
using JMW.TextExtraction.Singles;
using JMW.TextExtraction.Tables;
using System.Globalization;
using System.Text;

namespace JMW.TextExtraction;

/// <summary>
/// Writes a <see cref="DocumentSpec"/> as plain text a person can read and correct, and reads it back.
/// </summary>
/// <remarks>
/// One directive per line; <c>#</c> starts a comment, and keywords match regardless of case.
/// <code>
/// table aligned header: Port | Name | Status
/// table words headerless: 4
/// records split by rule
///   line "Device ID:" => Device ID: {Device ID}
///   line "+ Chassis ID :" => + Chassis ID {?Chassis ID qualifier} : {Chassis ID}
///   text "Version :" => Version : {Version}
/// records start at phrase "line protocol is", head
///   line "@head" => {Name} is {Field2} , line protocol is {line protocol}
/// </code>
/// A table names its layout (<c>aligned</c>, <c>ruled</c>, <c>words</c>) and its columns, or with no header,
/// how many columns it has. A records directive says where records split (<c>rule</c>, <c>blank</c>,
/// <c>indent</c>, optionally with the indent records start at) or where they start (<c>key "..."</c>,
/// <c>phrase "..."</c>), and <c>head</c> when a record's first line is keyed by position. A <c>single</c>
/// directive covers output that is one record per sample, such as <c>show version</c>. Under it, each <c>line</c> or <c>text</c> gives a line key and its template:
/// <c>{Name}</c> is a value, <c>{?Name}</c> is a qualifier on the key, and any other word must appear as
/// written. <c>text</c> marks a key whose value is the text below it.
/// </remarks>
public static class SpecText
{
    public static string Write(DocumentSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        StringBuilder sb = new();
        foreach (TableSpec table in spec.Tables)
        {
            sb.Append("table ").Append(table.Layout.ToString().ToLowerInvariant());
            if (table.HasHeader)
            {
                sb.Append(" header: ").AppendJoin(" | ", table.Columns.Select(static c => c.Replace("|", "\\|", StringComparison.Ordinal)));
            }
            else
            {
                sb.Append(" headerless: ").Append(table.Columns.Count.ToString(CultureInfo.InvariantCulture));
            }

            sb.AppendLine();
        }

        foreach (RecordSpec records in spec.Records)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append("records ").Append(records.Boundary switch
            {
                RecordBoundary.Anchor => "start at key " + Quote(records.Anchor ?? ""),
                RecordBoundary.Phrase => "start at phrase " + Quote(records.Anchor ?? ""),
                RecordBoundary.Indent when records.Anchor is not null => "split by indent " + records.Anchor,
                _ => "split by " + records.Boundary.ToString().ToLowerInvariant(),
            });
            if (records.KeyHead)
            {
                sb.Append(", head");
            }

            sb.AppendLine();
            WriteLines(sb, records.Lines);
        }

        if (spec.Single is { } single)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.AppendLine("single");
            WriteLines(sb, single.Lines);
        }

        return sb.ToString();
    }

    private static void WriteLines(StringBuilder sb, IEnumerable<LineTemplate> lines)
    {
        foreach (LineTemplate line in lines)
        {
            sb.Append("  ").Append(line.IsBlock ? "text " : "line ").Append(Quote(line.Key)).Append(" => ");
            sb.AppendJoin(" ", line.Parts.Select(static p => p switch
            {
                Slot { IsQualifier: true } s => "{?" + s.Name + "}",
                Slot s => "{" + s.Name + "}",
                Literal l => l.Text.StartsWith('{') || l.Text.StartsWith('\\') ? "\\" + l.Text : l.Text,
                _ => throw new InvalidOperationException(),
            }));
            sb.AppendLine();
        }
    }

    public static string Write(RecordSpec spec) => Write(new DocumentSpec([], [spec]));

    /// <exception cref="FormatException">A line is not a directive this format knows, with its line number.</exception>
    public static DocumentSpec Read(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        List<TableSpec> tables = [];
        List<RecordSpec> records = [];
        RecordSpec? current = null;
        bool inSingle = false;
        SingleSpec? single = null;
        List<LineTemplate> lines = [];
        string[] input = text.Split('\n');
        for (int n = 0; n < input.Length; n++)
        {
            string line = input[n].Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            try
            {
                if (StartsWord(line, "table"))
                {
                    tables.Add(ReadTable(line["table".Length..].Trim()));
                }
                else if (StartsWord(line, "records"))
                {
                    Flush();
                    current = ReadRecords(line["records".Length..].Trim());
                }
                else if (StartsWord(line, "single"))
                {
                    if (single is not null || inSingle || line.Length > "single".Length)
                    {
                        throw new FormatException("a spec has at most one single directive, with nothing after it on the line");
                    }

                    Flush();
                    inSingle = true;
                }
                else if (StartsWord(line, "line") || StartsWord(line, "text"))
                {
                    if (current is null && !inSingle)
                    {
                        throw new FormatException("a line template must follow a records or single directive");
                    }

                    lines.Add(ReadTemplate(line[4..].Trim(), isBlock: StartsWord(line, "text")));
                }
                else
                {
                    throw new FormatException("expected table, records, single, line, or text");
                }
            }
            catch (FormatException e)
            {
                throw new FormatException("line " + (n + 1).ToString(CultureInfo.InvariantCulture) + ": " + e.Message, e);
            }
        }

        Flush();
        return new DocumentSpec(tables, records, single);

        void Flush()
        {
            if (inSingle)
            {
                single = new SingleSpec(lines);
                inSingle = false;
            }
            else if (current is not null)
            {
                records.Add(current with { Lines = lines });
            }

            current = null;
            lines = [];
        }
    }

    private static bool StartsWord(string line, string word) =>
        line.StartsWith(word, StringComparison.OrdinalIgnoreCase) && (line.Length == word.Length || char.IsWhiteSpace(line[word.Length]));

    private static TableSpec ReadTable(string rest)
    {
        int colon = rest.IndexOf(':', StringComparison.Ordinal);
        if (colon < 0)
        {
            throw new FormatException("a table needs \"header:\" or \"headerless:\"");
        }

        string[] head = rest[..colon].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (head.Length != 2 || !Enum.TryParse(head[0], ignoreCase: true, out TableLayout layout))
        {
            throw new FormatException("expected \"table <aligned|ruled|words> header: ...\"");
        }

        string columns = rest[(colon + 1)..].Trim();
        if (head[1].Equals("headerless", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(columns, NumberStyles.None, CultureInfo.InvariantCulture, out int count) || count < 1)
            {
                throw new FormatException("a headerless table needs its column count");
            }

            return new TableSpec(layout, false, Enumerable.Range(1, count).Select(static i => "Column" + i.ToString(CultureInfo.InvariantCulture)).ToList());
        }

        if (!head[1].Equals("header", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("expected \"header:\" or \"headerless:\"");
        }

        List<string> names = [];
        StringBuilder name = new();
        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i] == '\\' && i + 1 < columns.Length)
            {
                name.Append(columns[++i]);
            }
            else if (columns[i] == '|')
            {
                names.Add(name.ToString().Trim());
                name.Clear();
            }
            else
            {
                name.Append(columns[i]);
            }
        }

        names.Add(name.ToString().Trim());
        return new TableSpec(layout, true, names);
    }

    private static RecordSpec ReadRecords(string rest)
    {
        bool head = false;
        int comma = rest.LastIndexOf(',');
        if (comma >= 0 && rest[(comma + 1)..].Trim().Equals("head", StringComparison.OrdinalIgnoreCase))
        {
            head = true;
            rest = rest[..comma].Trim();
        }

        string[] words = rest.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 3 && words[0].Equals("split", StringComparison.OrdinalIgnoreCase) && words[1].Equals("by", StringComparison.OrdinalIgnoreCase))
        {
            string[] indent = words[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (indent.Length == 2 && indent[0].Equals("indent", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(indent[1], NumberStyles.None, CultureInfo.InvariantCulture, out int level)
                    ? new RecordSpec(RecordBoundary.Indent, level.ToString(CultureInfo.InvariantCulture), head, [])
                    : throw new FormatException("an indent is a count of spaces");
            }

            return Enum.TryParse(words[2], ignoreCase: true, out RecordBoundary split) && split is RecordBoundary.Rule or RecordBoundary.Blank or RecordBoundary.Indent
                ? new RecordSpec(split, null, head, [])
                : throw new FormatException("records split by rule, blank, or indent");
        }

        if (words.Length == 3 && words[0].Equals("start", StringComparison.OrdinalIgnoreCase) && words[1].Equals("at", StringComparison.OrdinalIgnoreCase))
        {
            string[] kind = words[2].Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            RecordBoundary boundary = kind[0].ToLowerInvariant() switch
            {
                "key" => RecordBoundary.Anchor,
                "phrase" => RecordBoundary.Phrase,
                _ => throw new FormatException("records start at key \"...\" or phrase \"...\""),
            };
            int at = 0;
            string anchor = kind.Length > 1 ? Unquote(kind[1], ref at) : throw new FormatException("missing the quoted key or phrase");
            return new RecordSpec(boundary, anchor, head, []);
        }

        throw new FormatException("expected \"records split by ...\" or \"records start at ...\"");
    }

    private static LineTemplate ReadTemplate(string rest, bool isBlock)
    {
        int at = 0;
        string key = Unquote(rest, ref at);
        string arrow = rest[at..].TrimStart();
        if (!arrow.StartsWith("=>", StringComparison.Ordinal))
        {
            throw new FormatException("expected => after the key");
        }

        string template = arrow[2..].Trim();
        List<TemplatePart> parts = [];
        int i = 0;
        while (i < template.Length)
        {
            if (template[i] == ' ')
            {
                i++;
            }
            else if (template[i] == '{')
            {
                int close = template.IndexOf('}', i);
                if (close < 0)
                {
                    throw new FormatException("a { has no closing }");
                }

                string name = template[(i + 1)..close];
                parts.Add(name.StartsWith('?') ? new Slot(name[1..], IsQualifier: true) : new Slot(name));
                i = close + 1;
            }
            else
            {
                int end = template.IndexOf(' ', i);
                end = end < 0 ? template.Length : end;
                string word = template[i..end];
                parts.Add(new Literal(word.StartsWith('\\') ? word[1..] : word));
                i = end;
            }
        }

        if (isBlock && parts.OfType<Slot>().Count(static s => !s.IsQualifier) != 1)
        {
            throw new FormatException("a text template needs exactly one value slot");
        }

        return new LineTemplate(key, parts, isBlock);
    }

    private static string Quote(string value) =>
        "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    // Reads a quoted string starting at the first quote at or after at, and moves at past its closing quote.
    private static string Unquote(string text, ref int at)
    {
        int open = text.IndexOf('"', at);
        if (open < 0)
        {
            throw new FormatException("expected a quoted key");
        }

        StringBuilder value = new();
        for (int i = open + 1; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
            {
                value.Append(text[++i]);
            }
            else if (text[i] == '"')
            {
                at = i + 1;
                return value.ToString();
            }
            else
            {
                value.Append(text[i]);
            }
        }

        throw new FormatException("a quoted key has no closing quote");
    }
}
