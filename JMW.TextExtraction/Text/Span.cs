namespace JMW.TextExtraction.Text;

/// <summary>A half-open range of character positions, [Start, End).</summary>
public readonly record struct Span(int Start, int End)
{
    public int Length => this.End - this.Start;

    public bool Overlaps(Span other) => this.Start < other.End && other.Start < this.End;

    /// <summary>Characters between the two spans, or 0 when they touch or overlap.</summary>
    public int DistanceTo(Span other) =>
        other.Start >= this.End ? other.Start - this.End
        : this.Start >= other.End ? this.Start - other.End
        : 0;

    public string Of(string line)
    {
        int start = Math.Min(this.Start, line.Length);
        int end = Math.Min(this.End, line.Length);
        return line[start..end];
    }
}
