namespace JMW.TextExtraction.Text;

public static class Cells
{
    private static readonly char[] Trim = [' ', '|', '│', '┃', '║'];

    /// <summary>
    /// Cuts a line at the given positions. Cell i runs from cuts[i] to cuts[i + 1], the last to the end of the line.
    /// </summary>
    public static string[] Slice(string line, IReadOnlyList<int> cuts)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(cuts);
        string[] cells = new string[cuts.Count];
        for (int c = 0; c < cuts.Count; c++)
        {
            int start = Math.Min(cuts[c], line.Length);
            int end = c + 1 < cuts.Count ? Math.Min(cuts[c + 1], line.Length) : line.Length;
            cells[c] = line[start..end].Trim(Trim);
        }

        return cells;
    }

    /// <summary>
    /// In a table whose first column is almost always filled, a row with an empty first cell holds text that
    /// wrapped from the row above. Appends each such row to the one above it.
    /// </summary>
    /// <param name="lines">When given, the line range of each row, kept in step: a merged row's range grows to cover the row it absorbed.</param>
    public static void MergeContinuations(List<string[]> rows, List<Span>? lines = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        int filled = rows.Count(static r => r[0].Length > 0);
        if (filled < 2 || filled * 5 < rows.Count * 4)
        {
            return;
        }

        for (int r = rows.Count - 1; r > 0; r--)
        {
            if (rows[r][0].Length > 0)
            {
                continue;
            }

            string[] above = rows[r - 1];
            for (int c = 1; c < above.Length; c++)
            {
                if (rows[r][c].Length > 0)
                {
                    above[c] = above[c].Length == 0 ? rows[r][c] : above[c] + " " + rows[r][c];
                }
            }

            rows.RemoveAt(r);
            if (lines is not null)
            {
                lines[r - 1] = lines[r - 1] with { End = lines[r].End };
                lines.RemoveAt(r);
            }
        }
    }

    /// <summary>
    /// Strips one pair of enclosing quotes, parentheses, or brackets, so <c>"sw1"</c>, <c>(MAC address)</c>, and
    /// <c>[ethernet]</c> give their contents. A pair that does not enclose the whole value, as in <c>(a) (b)</c>,
    /// is left alone.
    /// </summary>
    public static string Unwrap(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        string v = value.Trim();
        if (v.Length < 2)
        {
            return v;
        }

        (char open, char close) = v[0] switch
        {
            '"' => ('"', '"'),
            '\'' => ('\'', '\''),
            '(' => ('(', ')'),
            '[' => ('[', ']'),
            _ => ('\0', '\0'),
        };

        if (open == '\0' || v[^1] != close)
        {
            return v;
        }

        string inner = v[1..^1];
        return inner.Contains(open, StringComparison.Ordinal) || inner.Contains(close, StringComparison.Ordinal) ? v : inner.Trim();
    }

    /// <summary>
    /// The value's character classes with repeats collapsed: upper case is <c>A</c>, lower case <c>a</c>,
    /// digits <c>9</c>, and anything else stands for itself. <c>Gi1/0/1</c> becomes <c>Aa9/9/9</c>.
    /// </summary>
    public static string Shape(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        System.Text.StringBuilder sb = new(value.Length);
        foreach (char c in value)
        {
            char k = char.IsUpper(c) ? 'A' : char.IsLower(c) ? 'a' : char.IsDigit(c) ? '9' : c;
            if (sb.Length == 0 || sb[^1] != k)
            {
                sb.Append(k);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// True when the value is all digits and number punctuation, as <c>1500</c>, <c>0/1</c>, <c>10.0.0.1</c>, or
    /// <c>04:00:00</c>. Unlike <see cref="IsNumeric"/>, a word that only starts with a digit, like <c>64-byte</c>,
    /// is not a number.
    /// </summary>
    public static bool IsNumber(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Any(char.IsAsciiDigit) && value.All(static c => char.IsAsciiDigit(c) || c is '.' or ',' or ':' or '/' or '%' or '-' or '+');
    }

    /// <summary>True when the value starts like a number: a digit, or a sign or point followed by one.</summary>
    public static bool IsNumeric(string cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        if (cell.Length == 0)
        {
            return false;
        }

        char c = cell[0];
        return char.IsAsciiDigit(c) || (c is '-' or '+' or '.' && cell.Length > 1 && char.IsAsciiDigit(cell[1]));
    }
}
