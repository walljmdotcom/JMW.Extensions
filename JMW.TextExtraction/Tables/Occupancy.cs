namespace JMW.TextExtraction.Tables;

/// <summary>
/// Which character positions hold data in at least one of a set of lines. The unoccupied positions between
/// occupied runs are the column gaps of an aligned table.
/// </summary>
public sealed class Occupancy
{
    private bool[] occupied;

    public Occupancy()
        : this([])
    {
    }

    private Occupancy(bool[] occupied)
    {
        this.occupied = occupied;
    }

    public int Lines { get; private set; }

    public Occupancy Clone() => new((bool[])this.occupied.Clone()) { Lines = this.Lines };

    public void Add(string line)
    {
        if (line.Length > this.occupied.Length)
        {
            Array.Resize(ref this.occupied, line.Length);
        }

        for (int i = 0; i < line.Length; i++)
        {
            if (!Text.Lines.IsSeparator(line[i]))
            {
                this.occupied[i] = true;
            }
        }

        this.Lines++;
    }

    /// <summary>Maximal runs of occupied positions, left to right.</summary>
    public List<Span> Spans()
    {
        List<Span> spans = [];
        int i = 0;
        while (i < this.occupied.Length)
        {
            if (!this.occupied[i])
            {
                i++;
                continue;
            }

            int start = i;
            while (i < this.occupied.Length && this.occupied[i])
            {
                i++;
            }

            spans.Add(new Span(start, i));
        }

        return spans;
    }
}
