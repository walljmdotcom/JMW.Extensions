using System.Collections;

namespace JMW.TextExtraction.Text;

/// <summary>
/// One line of a <see cref="TextDocument"/>, read once: its kind, indent, tokens, and key. Every stage of
/// inference and extraction reads these rather than looking at the text again.
/// </summary>
public sealed class TextLine
{
    internal TextLine(int index, string text)
    {
        this.Index = index;
        this.Text = text;
        this.Kind = Lines.Classify(text);
        this.Indent = Lines.Indent(text);
        this.Tokens = this.Kind == LineKind.Content ? Tokenizer.Tokenize(text) : [];
    }

    public int Index { get; }

    /// <summary>The line with tabs expanded and trailing whitespace removed.</summary>
    public string Text { get; }

    public LineKind Kind { get; }

    public int Indent { get; }

    /// <summary>The line's words, empty unless it is a content line.</summary>
    public IReadOnlyList<Token> Tokens { get; }

    /// <summary>The key that identifies this kind of line across records; see <see cref="LineKeys.Key"/>.</summary>
    public string Key => field ??= this.Tokens.Count == 0 ? "" : LineKeys.Key(this.Tokens);

    /// <summary>The key to fall back on when the line opens with a name; see <see cref="LineKeys.NameLedKey"/>.</summary>
    public string? NameLedKey => this.Tokens.Count == 0 ? null : LineKeys.NameLedKey(this.Tokens);

    public bool IsContent => this.Kind == LineKind.Content;

    /// <summary>
    /// The text between two tokens, inclusive, as the line wrote it, so a value of several words keeps its
    /// spacing. An empty range gives an empty string.
    /// </summary>
    public string Between(int firstToken, int lastToken) =>
        firstToken > lastToken ? "" : this.Text[this.Tokens[firstToken].Start..this.Tokens[lastToken].End];

    public override string ToString() => this.Text;
}

/// <summary>
/// Text split into lines and read once into <see cref="TextLine"/>s. Line indices are stable: a document made
/// from another with some lines removed keeps those lines as blanks, so positions still line up.
/// </summary>
public sealed class TextDocument : IReadOnlyList<TextLine>
{
    private readonly TextLine[] lines;

    private TextDocument(TextLine[] lines)
    {
        this.lines = lines;
        this.OuterIndent = lines.Where(static l => l.IsContent).Select(static l => l.Indent).DefaultIfEmpty(0).Min();
    }

    public static TextDocument Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return From(Text.Lines.Normalize(text));
    }

    /// <param name="lines">Lines already passed through <see cref="Lines.Normalize(IReadOnlyList{string})"/>.</param>
    public static TextDocument From(IReadOnlyList<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        TextLine[] read = new TextLine[lines.Count];
        for (int i = 0; i < read.Length; i++)
        {
            read[i] = new TextLine(i, lines[i]);
        }

        return new TextDocument(read);
    }

    /// <summary>The smallest indent among content lines: where a top-level line starts.</summary>
    public int OuterIndent { get; }

    public int Count => this.lines.Length;

    public TextLine this[int index] => this.lines[index];

    /// <summary>The lines' text, for stages that work on character positions, like table detection.</summary>
    public IReadOnlyList<string> Texts => field ??= this.lines.Select(static l => l.Text).ToArray();

    /// <summary>A copy with the lines in the given half-open ranges blanked out, all indices unchanged.</summary>
    public TextDocument Without(IEnumerable<Span> ranges)
    {
        ArgumentNullException.ThrowIfNull(ranges);
        TextLine[] copy = (TextLine[])this.lines.Clone();
        foreach (Span range in ranges)
        {
            for (int i = range.Start; i < range.End; i++)
            {
                copy[i] = new TextLine(i, "");
            }
        }

        return new TextDocument(copy);
    }

    public IEnumerator<TextLine> GetEnumerator() => ((IEnumerable<TextLine>)this.lines).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.lines.GetEnumerator();
}
