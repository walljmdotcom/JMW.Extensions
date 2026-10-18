using JMW.TextExtraction.Text;

namespace JMW.TextExtraction.Records;

/// <summary>
/// A node of a record tree: one line with the key that identifies it across records. <see cref="Path"/> is
/// the node's address, the keys of the groups above it and its own, as in
/// <c>Entry address(es): / IPv4 address:</c>. <see cref="Text"/> holds the lines under a key that has its
/// value below it rather than beside it. A group's <see cref="Children"/> are the indented lines below it.
/// </summary>
/// <param name="LastLine">The last line the node covers, which is past its own line when it holds text below.</param>
/// <param name="Continuation">
/// Deeper lines that carry on the value of a <c>key: value</c> line, as the second row of an
/// <c>MD5 digest : 0x32 0x1E ...</c>, joined by spaces.
/// </param>
/// <param name="KeyOnly">Whether the line is a key alone, as <c>Entry address(es):</c>, holding its children rather than a value.</param>
public sealed record KeyedLine(
    TextLine Line,
    string Path,
    string? Text,
    int LastLine,
    IReadOnlyList<KeyedLine> Children,
    string? Continuation = null,
    bool KeyOnly = false
)
{
    public IReadOnlyList<Token> Tokens => this.Line.Tokens;

    public int FirstLine => this.Line.Index;

    public bool IsNested => this.Path.Contains(RecordLines.PathSeparator, StringComparison.Ordinal);

    /// <summary>A line with indented lines below it that belong to it.</summary>
    public bool IsGroup => this.Children.Count > 0;
}

/// <summary>Reads the lines of one record into a tree of keyed lines, the unit that templates are built from.</summary>
public static class RecordLines
{
    /// <summary>The key of a record's first line when records start at a line whose own key varies, like <c>en0:</c>.</summary>
    public const string Head = "@head";

    public const string PathSeparator = " / ";

    /// <summary>
    /// Reads one record into a tree of keyed lines. A key alone on its line with indented lines below it is a
    /// group, and those lines are its children.
    /// </summary>
    /// <param name="keyHead">Key the record's first line as <see cref="Head"/> rather than by its words.</param>
    /// <param name="keyOf">How to key a line, when not by <see cref="TextLine.Key"/>.</param>
    public static List<KeyedLine> Parse(TextDocument document, Span record, bool keyHead, Func<TextLine, string>? keyOf = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        List<KeyedLine> roots = [];
        Stack<(int Indent, string Path, List<KeyedLine> Children)> groups = new();
        bool first = true;
        int i = record.Start;
        while (i < record.End)
        {
            TextLine line = document[i];
            if (!line.IsContent)
            {
                i++;
                continue;
            }

            while (groups.Count > 0 && line.Indent <= groups.Peek().Indent)
            {
                groups.Pop();
            }

            List<KeyedLine> container = groups.Count > 0 ? groups.Peek().Children : roots;
            bool recordHead = first;
            string key = first && keyHead ? Head : keyOf?.Invoke(line) ?? line.Key;
            first = false;
            string path = groups.Count > 0 ? groups.Peek().Path + PathSeparator + key : key;

            // "Key:" alone on its line has its value below it. Indented lines are a nested group. A line at the
            // same indent that is not itself "key: value" starts a text value, which runs to the next blank line
            // whatever it holds. A "key: value" line at the same indent means the value is empty.
            IReadOnlyList<Token> tokens = line.Tokens;
            bool keyOnly = LineKeys.SeparatorIndex(tokens) == tokens.Count - 1 && tokens[^1].Text != "=";
            TextLine? next = i + 1 < record.End ? document[i + 1] : null;
            bool deeper = next is { IsContent: true } && next.Indent > line.Indent;

            // A record whose first line is its name, as "GigabitEthernet0/1:" over "Tx: enabled", reads that line
            // as a value, and the lines below as the record's own rather than as a group under it.
            if (recordHead && keyHead)
            {
                container.Add(new KeyedLine(line, path, null, i, []));
                i++;
                continue;
            }

            if (!keyOnly)
            {
                // A "key: value" line's value can wrap onto deeper lines that are not keyed themselves.
                int end = i + 1;
                if (LineKeys.IsKeyValue(tokens))
                {
                    while (end < record.End && document[end] is { IsContent: true } more && more.Indent > line.Indent && !LineKeys.IsKeyValue(more.Tokens))
                    {
                        end++;
                    }
                }

                // A line with a value can also head the deeper lines below it, as "Root ID  Priority 24596" heads
                // its "Address" and "Cost". Its own record's first line already heads the record, and a line that
                // opens with a number, as "5 minute output rate", heads nothing.
                if (end == i + 1 && deeper && !recordHead && !Cells.IsNumeric(tokens[0].Text))
                {
                    List<KeyedLine> children = [];
                    container.Add(new KeyedLine(line, path, null, i, children));
                    groups.Push((line.Indent, path, children));
                    i++;
                    continue;
                }

                string? continuation = end > i + 1 ? string.Join(" ", Enumerable.Range(i + 1, end - i - 1).Select(j => document[j].Text.Trim())) : null;
                container.Add(new KeyedLine(line, path, null, end - 1, [], continuation));
                i = end;
            }
            else if (deeper)
            {
                List<KeyedLine> children = [];
                container.Add(new KeyedLine(line, path, null, i, children, KeyOnly: true));
                groups.Push((line.Indent, path, children));
                i++;
            }
            else if (next is not { IsContent: true } || LineKeys.IsKeyValue(next.Tokens))
            {
                container.Add(new KeyedLine(line, path, null, i, []));
                i++;
            }
            else
            {
                int end = i + 1;
                while (end < record.End && document[end].IsContent)
                {
                    end++;
                }

                string text = string.Join("\n", Enumerable.Range(i + 1, end - i - 1).Select(j => document[j].Text.Trim()));
                container.Add(new KeyedLine(line, path, text, end - 1, []));
                i = end;
            }
        }

        return roots;
    }

    /// <summary>
    /// The lines of a record tree that carry values, depth first in document order. A key alone on its line,
    /// which only holds children, is left out; a line with a value that also heads children is kept.
    /// </summary>
    public static IEnumerable<KeyedLine> Values(IEnumerable<KeyedLine> roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        foreach (KeyedLine node in roots)
        {
            if (!node.KeyOnly)
            {
                yield return node;
            }

            foreach (KeyedLine child in Values(node.Children))
            {
                yield return child;
            }
        }
    }
}
