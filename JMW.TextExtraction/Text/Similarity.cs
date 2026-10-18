namespace JMW.TextExtraction.Text;

public static class Similarity
{
    /// <summary>Shared members over all members (Jaccard similarity), from 0 to 1. Two empty sets are alike.</summary>
    public static double Jaccard(HashSet<string> a, HashSet<string> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Count == 0 && b.Count == 0)
        {
            return 1;
        }

        int shared = a.Count(b.Contains);
        return shared / (double)(a.Count + b.Count - shared);
    }
}
