using System.Text;

namespace JMW.TextExtraction.Text;

public enum LineKind
{
    Blank,
    Rule,
    Content,
}

/// <summary>Line-level primitives: normalizing, classifying, and splitting single lines.</summary>
public static class Lines
{
    public const int TabWidth = 8;

    /// <summary>Splits text into lines, expanding tabs and dropping trailing whitespace and carriage returns.</summary>
    public static string[] Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Normalize(text.Split('\n'));
    }

    public static string[] Normalize(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        string[] normalized = new string[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            normalized[i] = ExpandTabs(lines[i]).TrimEnd();
        }

        return normalized;
    }

    public static string ExpandTabs(string line)
    {
        if (!line.Contains('\t', StringComparison.Ordinal))
        {
            return line;
        }

        StringBuilder sb = new(line.Length + 16);
        foreach (char c in line)
        {
            if (c == '\t')
            {
                sb.Append(' ', TabWidth - (sb.Length % TabWidth));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// A separator is a character that never holds data: whitespace, and the vertical bars that box drawn tables.
    /// </summary>
    public static bool IsSeparator(char c) => c is ' ' or '|' or '│' or '┃' or '║';

    private static bool IsRuleMark(char c) => c is '-' or '=' or '_' or '─' or '━' or '═';

    private static bool IsRuleJoint(char c) =>
        c is '+' or ':' or '┼' or '┬' or '┴' or '├' or '┤' or '┌' or '┐' or '└' or '┘' || IsSeparator(c);

    public static LineKind Classify(string line)
    {
        int marks = 0;
        bool content = false;
        foreach (char c in line)
        {
            if (IsRuleMark(c))
            {
                marks++;
            }
            else if (!IsRuleJoint(c))
            {
                content = true;
                break;
            }
        }

        return content ? LineKind.Content
            : marks >= 3 ? LineKind.Rule
            : line.Trim().Length == 0 ? LineKind.Blank
            : LineKind.Content;
    }

    public static int Indent(string line)
    {
        int i = 0;
        while (i < line.Length && line[i] == ' ')
        {
            i++;
        }

        return i;
    }

    /// <summary>Maximal runs of non-separator characters.</summary>
    public static List<Span> Words(string line)
    {
        List<Span> words = [];
        int i = 0;
        while (i < line.Length)
        {
            if (IsSeparator(line[i]))
            {
                i++;
                continue;
            }

            int start = i;
            while (i < line.Length && !IsSeparator(line[i]))
            {
                i++;
            }

            words.Add(new Span(start, i));
        }

        return words;
    }

    /// <summary>The dashed segments of a rule line such as <c>------ ---- -----</c>.</summary>
    public static List<Span> RuleSegments(string line)
    {
        List<Span> segments = [];
        int i = 0;
        while (i < line.Length)
        {
            if (!IsRuleMark(line[i]))
            {
                i++;
                continue;
            }

            int start = i;
            while (i < line.Length && IsRuleMark(line[i]))
            {
                i++;
            }

            segments.Add(new Span(start, i));
        }

        return segments;
    }
}
