namespace JMW.TextExtraction.Text;

public static class Blocks
{
    /// <summary>Runs of consecutive non-blank lines, as half-open line ranges.</summary>
    public static List<Span> Split(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        List<Span> blocks = [];
        int start = 0;
        while (start < lines.Count)
        {
            if (Lines.Classify(lines[start]) == LineKind.Blank)
            {
                start++;
                continue;
            }

            int end = start;
            while (end < lines.Count && Lines.Classify(lines[end]) != LineKind.Blank)
            {
                end++;
            }

            blocks.Add(new Span(start, end));
            start = end;
        }

        return blocks;
    }
}
