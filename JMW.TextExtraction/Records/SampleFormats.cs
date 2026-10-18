namespace JMW.TextExtraction.Records;

/// <summary>
/// Groups samples of the same command by format, such as one group per platform, so each group can get its
/// own spec.
/// </summary>
/// <remarks>
/// A sample is reduced to the set of keys its lines carry, and two samples share a format when those sets
/// overlap enough (Jaccard similarity: shared keys over all keys). Keys that hold digits, such as interface
/// names, say which device produced the text rather than which format, so they are left out.
/// </remarks>
public static class SampleFormats
{
    public const double DefaultSimilarity = 0.5;

    /// <summary>
    /// Assigns each sample to the first group whose founding sample it resembles, largest samples first, so
    /// the founder of each group is its most complete example. Returns sample indices per group, largest group
    /// first. Samples with no keys, such as an error message, form no group.
    /// </summary>
    public static List<List<int>> Group(IReadOnlyList<string> samples, double similarity = DefaultSimilarity)
    {
        ArgumentNullException.ThrowIfNull(samples);
        return Group(samples.Select(TextDocument.Parse).ToList(), similarity);
    }

    public static List<List<int>> Group(IReadOnlyList<TextDocument> samples, double similarity = DefaultSimilarity)
    {
        ArgumentNullException.ThrowIfNull(samples);
        HashSet<string>[] keys = samples.Select(Keys).ToArray();
        IEnumerable<int> order = Enumerable.Range(0, samples.Count).OrderByDescending(i => keys[i].Count).ThenBy(static i => i);

        List<(HashSet<string> Founder, List<int> Members)> groups = [];
        foreach (int i in order)
        {
            if (keys[i].Count == 0)
            {
                continue;
            }

            int match = groups.FindIndex(g => Similarity.Jaccard(g.Founder, keys[i]) >= similarity);
            if (match < 0)
            {
                groups.Add((keys[i], [i]));
            }
            else
            {
                groups[match].Members.Add(i);
            }
        }

        return groups.Select(static g => g.Members.Order().ToList()).OrderByDescending(static g => g.Count).ToList();
    }

    public static HashSet<string> Keys(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (TextLine line in document)
        {
            if (line.IsContent && !line.Key.Any(char.IsAsciiDigit))
            {
                keys.Add(line.Key);
            }
        }

        return keys;
    }
}
