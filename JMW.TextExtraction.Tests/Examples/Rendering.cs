using System.Globalization;
using System.Text;

namespace JMW.TextExtraction.Tests.Examples;

/// <summary>Writes what a document read as, one value per line, so an expected file reads like the input.</summary>
public static class Rendering
{
    public static string Render(ExtractedDocument document, string text)
    {
        ArgumentNullException.ThrowIfNull(document);
        string[] lines = Lines.Normalize(text);
        StringBuilder sb = new();
        foreach (ExtractedTable table in document.Tables)
        {
            sb.Append("table ").Append(table.Spec.Layout.ToString().ToLowerInvariant())
                .Append(" lines ").Append(Range(table.FirstLine, table.LastLine)).Append(": ")
                .AppendJoin(" | ", table.Spec.Columns).Append('\n');
            foreach (ExtractedRecord row in table.Rows)
            {
                sb.Append("  row ").Append(Range(row.FirstLine, row.LastLine)).Append(": ")
                    .AppendJoin(" | ", row.Fields.Select(static f => f.Value)).Append('\n');
            }
        }

        foreach (ExtractedRecord record in document.Records)
        {
            sb.Append("record lines ").Append(Range(record.FirstLine, record.LastLine)).Append('\n');
            Fields(sb, record);
        }

        if (document.Single is { } single)
        {
            sb.Append("single lines ").Append(Range(single.FirstLine, single.LastLine)).Append('\n');
            Fields(sb, single);
        }

        foreach (int line in document.Unclaimed)
        {
            sb.Append("unclaimed ").Append(Number(line)).Append(": ").Append(lines[line].Trim()).Append('\n');
        }

        return sb.ToString();
    }

    private static void Fields(StringBuilder sb, ExtractedRecord record)
    {
        foreach (ExtractedField field in record.Fields)
        {
            sb.Append("  ").Append(field.Key).Append(" = ").Append(field.Value.Replace("\n", " \\n ", StringComparison.Ordinal));
            if (field.Unit is not null)
            {
                sb.Append(" [").Append(field.Unit).Append(']');
            }

            sb.Append('\n');
        }
    }

    // Line numbers are written one-based, as an editor shows them.
    private static string Range(int first, int last) => first == last ? Number(first) : Number(first) + "-" + Number(last);

    private static string Number(int line) => (line + 1).ToString(CultureInfo.InvariantCulture);
}
