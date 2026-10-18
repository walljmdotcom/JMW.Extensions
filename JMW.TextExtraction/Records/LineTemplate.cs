using System.Globalization;

namespace JMW.TextExtraction.Records;

public abstract record TemplatePart;

/// <summary>A word that appears in every instance of the line.</summary>
public sealed record Literal(string Text) : TemplatePart
{
    public override string ToString() => this.Text;
}

/// <summary>A run of words that changes from one instance of the line to the next.</summary>
/// <param name="IsQualifier">
/// Whether the slot is a parenthetical inside a key, as <c>(MAC address)</c> in <c>Chassis ID (MAC address):</c>,
/// which says what kind of value follows.
/// </param>
public sealed record Slot(string Name, bool IsQualifier = false) : TemplatePart
{
    public override string ToString() => "{" + this.Name + "}";
}

/// <summary>
/// The shape of one kind of line: the words every instance shares, and named slots for the words between them.
/// <c>Platform: {Platform} , Capabilities: {Capabilities}</c>. A block template takes its value from the text
/// below the key line.
/// </summary>
public sealed record LineTemplate(string Key, IReadOnlyList<TemplatePart> Parts, bool IsBlock)
{
    public override string ToString() => string.Join(" ", this.Parts) + (this.IsBlock ? " (text below)" : "");

    /// <summary>The names of the value slots. Qualifiers are not fields of their own.</summary>
    public IEnumerable<string> Fields => this.Parts.OfType<Slot>().Where(static s => !s.IsQualifier).Select(static s => s.Name);

    /// <summary>
    /// Finds the literal words in order and reads each slot as the text between the nearest found literals on
    /// either side. Literals match regardless of case, and a missing or reworded one is skipped, so the line
    /// matches when at least half its literals are found, but not on the line's first word alone, which every line
    /// of the kind shares. A qualifier's value goes onto the field that follows
    /// it rather than becoming a field of its own.
    /// </summary>
    public bool TryMatch(string line, List<ExtractedField> fields) => this.TryMatch(TextDocument.Parse(line)[0], fields);

    public bool TryMatch(TextLine line, List<ExtractedField> fields)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(fields);
        IReadOnlyList<Token> tokens = line.Tokens;
        List<string> literals = this.Parts.OfType<Literal>().Select(static l => l.Text).ToList();
        int[] at = Matches(literals, tokens.Select(static t => t.Text).ToList());
        int found = at.Count(static a => a >= 0);
        bool onlyKey = found == 1 && literals.Count >= 2 && at.Contains(0);
        if (found * 2 < literals.Count || onlyKey)
        {
            return false;
        }

        int literal = 0;
        string? qualifier = null;
        for (int p = 0; p < this.Parts.Count; p++)
        {
            if (this.Parts[p] is not Slot slot)
            {
                literal++;
                continue;
            }

            (int from, int to) = Between(at, literal, tokens.Count);
            string value = Cells.Unwrap(line.Between(from, to - 1).TrimEnd(',', ';', ' '));

            // A name written as a key, as "GigabitEthernet0/1:", is the name. A colon that ends an address, as in
            // "fe80::", has colons before it and stays.
            if (value.Length > 1 && value[^1] == ':' && value.IndexOf(':', StringComparison.Ordinal) == value.Length - 1)
            {
                value = value[..^1];
            }

            if (slot.IsQualifier)
            {
                qualifier = value.Length > 0 ? value : null;
                continue;
            }

            fields.Add(new ExtractedField(slot.Name, value, qualifier, this.UnitAfter(p, value)));
            qualifier = null;
        }

        return true;
    }

    private const int MaxUnitLength = 12;

    // The word after a number, when it reads as a unit: letters, perhaps with one "/", as "bytes", "Mbit", or
    // "packets/sec", ending its phrase, so "{0} packets input" does not make "packets" a unit of "input". A
    // word the slot is named for, as "runts" in "{0} runts", is the field, not its unit.
    private string? UnitAfter(int slot, string value)
    {
        if (!Cells.IsNumber(value) || slot + 1 >= this.Parts.Count || this.Parts[slot + 1] is not Literal { Text: var unit })
        {
            return null;
        }

        if (this.Parts[slot] is Slot { Name: var name } && name.Split(' ', '/').Intersect(unit.Split('/'), StringComparer.OrdinalIgnoreCase).Any())
        {
            return null;
        }

        // A word followed by another value, as "at" in "by 192.0.2.5 at 3-7-22", is that value's key.
        bool ends = slot + 2 >= this.Parts.Count || this.Parts[slot + 2] is Literal { Text: "," or ";" or ")" };
        bool shaped = unit.Length <= MaxUnitLength && unit.Count(static c => c == '/') <= 1 && unit.All(static c => char.IsLetter(c) || c == '/');
        return ends && shaped ? unit : null;
    }

    // For each literal, the position of the word it matches in a longest common subsequence, or -1.
    private static int[] Matches(List<string> literals, List<string> words)
    {
        int[,] len = LcsTable(literals, words);
        int[] at = new int[literals.Count];
        Array.Fill(at, -1);
        int x = 0;
        int y = 0;
        while (x < literals.Count && y < words.Count)
        {
            if (string.Equals(literals[x], words[y], StringComparison.OrdinalIgnoreCase))
            {
                at[x++] = y++;
            }
            else if (len[x + 1, y] >= len[x, y + 1])
            {
                x++;
            }
            else
            {
                y++;
            }
        }

        return at;
    }

    public static LineTemplate Block(string key, IReadOnlyList<Token> keyTokens)
    {
        ArgumentNullException.ThrowIfNull(keyTokens);
        List<TemplatePart> parts = keyTokens.Select(static t => (TemplatePart)new Literal(t.Text)).ToList();
        parts.Add(new Slot(""));
        Name(key, parts);
        return new LineTemplate(key, parts, IsBlock: true);
    }

    /// <summary>
    /// Builds a template from every instance of a line. The literals are the words all instances share in the
    /// same order (their longest common subsequence), less any number. Every gap where some instance has words
    /// becomes a slot, and so does the gap after a separator, since <c>key: value</c> has a value even where
    /// every sample happens to show the same one.
    /// </summary>
    public static LineTemplate Infer(string key, IReadOnlyList<IReadOnlyList<Token>> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);
        List<List<string>> texts = instances.Select(static i => i.Select(static t => t.Text).ToList()).ToList();
        List<string> literals = Literals(texts);

        // A head line opens with the record's name, which can hold a space, as in "TenGigabitEthernet 0/2 is
        // up", so the words before its first "is" or separator are the name even where most records share them.
        // A name written as a key, as "eth0:" in "eth0: flags=4163  mtu 1500", is one word, and the words after it
        // are the line's own.
        if (key == RecordLines.Head && !instances.All(static i => i.Count > 0 && LineKeys.IsSeparator(i[0].Text)))
        {
            int opener = literals.FindIndex(static l => l is "is" or "are" || LineKeys.IsSeparator(l));
            literals.RemoveRange(0, Math.Max(0, opener));
        }

        HashSet<string> setOff = SetOff(instances);

        // A parenthetical right before a separator qualifies the value, as "(MAC address)" in "Chassis ID
        // (MAC address):", so it is a slot even where every sample shows the same words.
        HashSet<int> qualifierGaps = [];
        for (int c = 0; c < literals.Count; c++)
        {
            if (!literals[c].StartsWith('('))
            {
                continue;
            }

            int close = c;
            while (close < literals.Count && !literals[close].EndsWith(')'))
            {
                close++;
            }

            if (close + 1 < literals.Count && LineKeys.IsSeparator(literals[close + 1]))
            {
                literals.RemoveRange(c, close - c + 1);
                qualifierGaps.Add(c);
            }
        }

        // After a separator, or "is", the words up to the next key are a value, however constant: "line protocol
        // is up" has a value even where every sample is up. A word followed by "=" or "is" is the next key, as
        // "OUI" in "Protocol Hello:  OUI=0x00000C" or "BDR" in "DR is 192.0.2.20 BDR is 192.0.2.19". After "is", a
        // comma ends the value, as in "State is FULL, 6 state changes". After a separator, a comma ends it only
        // when a key follows, as in "Platform: cisco WS-C3850, Capabilities: Router"; otherwise the comma is part
        // of the value, as in "Description: Cisco IOS Software, Version 15.2". A word ending in a colon that ends
        // the line after "is" is the value, as "disk0:" in "Boot device on node 0/RSP0/CPU0 is disk0:".
        for (int c = 0; c < literals.Count - 1; c++)
        {
            bool separator = LineKeys.IsSeparator(literals[c]);
            if (!separator && literals[c] is not ("is" or "are"))
            {
                continue;
            }

            int d = c + 1;
            while (d < literals.Count
                && !(LineKeys.IsSeparator(literals[d]) && (separator || d < literals.Count - 1))
                && !(literals[d] is "," or ";" && (!separator || KeyFollows(literals, d, texts)))
                && !(d + 1 < literals.Count && literals[d + 1] is "=" or "is" or "are")
                && !setOff.Contains(literals[d]))
            {
                d++;
            }

            literals.RemoveRange(c + 1, d - c - 1);
        }

        // A gap holds a value when instances that have both of its literals put words there. Instances missing
        // one tell nothing about it, and a gap filled in only a few of the rest is an odd line, not a field.
        int[][] at = texts.Select(t => Matches(literals, t)).ToArray();
        List<TemplatePart> parts = [];
        for (int gap = 0; gap <= literals.Count; gap++)
        {
            int filled = 0;
            int framed = 0;
            bool parenthesized = true;
            for (int i = 0; i < texts.Count; i++)
            {
                bool left = gap == 0 || at[i][gap - 1] >= 0;
                bool right = gap == literals.Count || at[i][gap] >= 0;
                if (left && right)
                {
                    framed++;
                    (int from, int to) = Between(at[i], gap, texts[i].Count);
                    if (to > from)
                    {
                        filled++;
                        parenthesized &= texts[i][from].StartsWith('(') && texts[i][to - 1].EndsWith(')');
                    }
                }
            }

            // Qualifiers that vary from record to record lose the vote for literals, so a gap in the key whose
            // words are always parenthesized is one too.
            bool used = filled > 0 && filled >= framed * MinGapUse;
            if (qualifierGaps.Contains(gap) || (used && parenthesized && gap < literals.Count && LineKeys.IsSeparator(literals[gap])))
            {
                parts.Add(new Slot("", IsQualifier: true));
            }
            else if (used || (gap > 0 && (LineKeys.IsSeparator(literals[gap - 1]) || literals[gap - 1] is "is" or "are")))
            {
                parts.Add(new Slot(""));
            }

            if (gap < literals.Count)
            {
                parts.Add(new Literal(literals[gap]));
            }
        }

        Name(key, parts);
        return new LineTemplate(key, parts, IsBlock: false);
    }

    // Plain words that every line holding them sets off with two or more spaces, as "mtu" in "flags=4163<UP>  mtu
    // 1500". Such a word starts the next pair rather than continuing a value. A word padded out from a separator,
    // as "no" in "Enabled:     no", is that separator's value.
    private static HashSet<string> SetOff(IReadOnlyList<IReadOnlyList<Token>> instances)
    {
        Dictionary<string, bool> wide = new(StringComparer.OrdinalIgnoreCase);
        foreach (IReadOnlyList<Token> tokens in instances)
        {
            for (int i = 1; i < tokens.Count; i++)
            {
                // From one line, only a word that opens a key, as "RD" in "Table ID: 0x0   RD version: 0", counts.
                bool opensKey = (i + 1 < tokens.Count && LineKeys.IsSeparator(tokens[i + 1].Text))
                    || (i + 2 < tokens.Count && LineKeys.IsSeparator(tokens[i + 2].Text));
                if (!tokens[i].Text.All(char.IsLetter) || (instances.Count < 2 && !opensKey))
                {
                    continue;
                }

                bool gap = tokens[i].Start - tokens[i - 1].End >= 2 && !LineKeys.IsSeparator(tokens[i - 1].Text);
                wide[tokens[i].Text] = wide.GetValueOrDefault(tokens[i].Text, true) && gap;
            }
        }

        return new HashSet<string>(wide.Where(static w => w.Value).Select(static w => w.Key), StringComparer.OrdinalIgnoreCase);
    }

    // Whether a key follows the comma at the given position: a word every instance shares, before the next
    // comma. "TTL" in "Neighbor: 0200.5e50.c230, TTL 114 seconds" is one; the words after the comma in "Cisco
    // IOS Software, C3750E Software" change from line to line, so they are part of the value. A word that ends
    // every line has no value after it, so "IGMP" in "Capabilities: Router, Switch, IGMP" is not a key.
    private static bool KeyFollows(List<string> literals, int comma, List<List<string>> texts)
    {
        if (comma + 1 >= literals.Count || literals[comma + 1] is "," or ";")
        {
            return false;
        }

        // "bytes" in "167010 packets, 34103102 bytes" ends the line too, but its value sits between it and the comma.
        string next = literals[comma + 1];
        return comma + 2 < literals.Count || LineKeys.IsSeparator(next)
            || !texts.All(t => t.Count > 1 && string.Equals(t[^1], next, StringComparison.OrdinalIgnoreCase) && t[^2] is "," or ";");
    }

    private const double VariantSimilarity = 0.6;

    private const int MaxVariants = 6;

    /// <summary>
    /// One template per shape the line comes in. Lines that share a key can still differ, as "Last input
    /// never, output never" and "Last clearing of counters never" share "Last", or "Encapsulation ARPA" and
    /// "Encapsulation 802.1Q Virtual LAN, Vlan ID 100". Instances whose words mostly agree (a common subsequence
    /// of at least 60% of their words) form one variant, most common first.
    /// </summary>
    public static List<LineTemplate> InferVariants(string key, IReadOnlyList<IReadOnlyList<Token>> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);

        // Heads that all end in a colon are a name, after any words they all open with: "Ethernet adapter
        // Ethernet 2:" is all name, and "DTP information for Gi1/0/1:" is a name after "DTP information for".
        if (key == RecordLines.Head && instances.Count > 0 && instances.All(static i => i.Count > 0 && i[^1].Text.EndsWith(':')))
        {
            int shared = 0;
            while (instances.All(i => shared < i.Count - 1
                && string.Equals(i[shared].Text, instances[0][shared].Text, StringComparison.OrdinalIgnoreCase)))
            {
                shared++;
            }

            List<TemplatePart> name = [.. instances[0].Take(shared).Select(static t => new Literal(t.Text)), new Slot("")];
            Name(key, name);
            return [new LineTemplate(key, name, IsBlock: false)];
        }

        List<(List<string> Reference, List<IReadOnlyList<Token>> Members)> variants = [];
        foreach (IReadOnlyList<Token> instance in instances)
        {
            // A head line opens with the record's name, which differs in every record and says nothing of its shape.
            IEnumerable<Token> shape = key == RecordLines.Head ? instance.Skip(1) : instance;
            List<string> words = shape.Select(static t => t.Text).Where(static w => !Cells.IsNumber(w)).ToList();
            int match = variants.FindIndex(v => Overlap(v.Reference, words) >= VariantSimilarity);
            if (match < 0)
            {
                variants.Add((words, [instance]));
            }
            else
            {
                variants[match].Members.Add(instance);
            }
        }

        // Variants whose values differ but whose words agree, as two descriptions, infer the same template.
        return variants
            .OrderByDescending(static v => v.Members.Count)
            .Take(MaxVariants)
            .Select(v => Infer(key, v.Members))
            .DistinctBy(static t => t.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // Twice the common subsequence over both lengths: 1 when the word lists agree, 0 when they share nothing.
    private static double Overlap(List<string> a, List<string> b) =>
        a.Count + b.Count == 0 ? 1 : 2.0 * LcsTable(a, b)[0, 0] / (a.Count + b.Count);

    /// <summary>The share of this template's literals the line holds in order, from 0 to 1.</summary>
    public double Fit(TextLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        List<string> literals = this.Parts.OfType<Literal>().Select(static l => l.Text).ToList();
        return literals.Count == 0 ? 1 : Matches(literals, line.Tokens.Select(static t => t.Text).ToList()).Count(static a => a >= 0) / (double)literals.Count;
    }

    private const double LiteralVote = 0.8;

    private const double MinGapUse = 0.05;

    /// <summary>
    /// The words shared by nearly every instance, in order. The reference is the commonest sequence of
    /// non-numeric words, and a word of it stays when at least 80% of instances contain it in a common
    /// subsequence, so one instance from an odd format does not erase the rest.
    /// </summary>
    private static List<string> Literals(List<List<string>> texts)
    {
        List<string> reference = texts
            .Select(static t => t.Where(static w => !Cells.IsNumber(w)).ToList())
            .GroupBy(static t => string.Join("\u0001", t), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static g => g.Count())
            .First()
            .First();

        int[] votes = new int[reference.Count];
        foreach (List<string> text in texts)
        {
            int[] at = Matches(reference, text);
            for (int r = 0; r < at.Length; r++)
            {
                votes[r] += at[r] >= 0 ? 1 : 0;
            }
        }

        return reference.Where((_, r) => votes[r] >= texts.Count * LiteralVote).ToList();
    }

    // The words between the nearest found literals on either side of a gap, as a half-open word range.
    private static (int From, int To) Between(int[] at, int gap, int count)
    {
        int before = gap - 1;
        while (before >= 0 && at[before] < 0)
        {
            before--;
        }

        int after = gap;
        while (after < at.Length && at[after] < 0)
        {
            after++;
        }

        return (before < 0 ? 0 : at[before] + 1, after < at.Length ? at[after] : count);
    }

    /// <summary>
    /// Names each slot by the literal words before it, back to the previous slot or comma, without separators:
    /// <c>Port ID (outgoing port): {}</c> names <c>Port ID (outgoing port)</c>, and a trailing "is" is dropped,
    /// so <c>Hardware is {}</c> names <c>Hardware</c>. A slot with no words before it takes the words after it,
    /// for values written ahead of their key, as in <c>{} packets input</c>. A qualifier is named for its key,
    /// as <c>Chassis ID qualifier</c>, and the value after it looks past it for its name. Nested lines carry
    /// their group's name, and repeated names get a number.
    /// </summary>
    private static void Name(string key, List<TemplatePart> parts)
    {
        int group = key.LastIndexOf(RecordLines.PathSeparator, StringComparison.Ordinal);
        string prefix = group < 0 ? "" : CleanPath(key[..group]) + RecordLines.PathSeparator;
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        int slots = 0;
        for (int p = 0; p < parts.Count; p++)
        {
            if (parts[p] is not Slot slot)
            {
                continue;
            }

            slots++;
            List<string> words = Words(parts, p, -1);
            if (slot.IsQualifier && words.Count > 0)
            {
                words.Add("qualifier");
            }

            bool afterIs = words.Count > 0 && words[^1] is "is" or "are";
            if (afterIs)
            {
                words.RemoveAt(words.Count - 1);
            }

            if (words.Count == 0)
            {
                words = Words(parts, p, 1).SkipWhile(static w => w is "is" or "are").ToList();
            }

            // A record's head line usually opens with the record's name and its state, as in "Vlan1 is up", and a
            // line keyed by the words after a name opens with that name, as in "dist-sw1 uptime is 2 years".
            bool named = key == RecordLines.Head || key.StartsWith("* ", StringComparison.Ordinal);
            if (named && slots == 1 && p == 0)
            {
                words = [];
            }

            string fallback = named && slots == 1 ? "Name"
                : key == RecordLines.Head && afterIs ? "Status"
                : "Field" + slots.ToString(CultureInfo.InvariantCulture);
            string name = prefix + (words.Count > 0 ? string.Join(" ", words) : fallback);

            // A name taken by the value before, as in "{5} minute input rate {0} bits/sec", gains the words after
            // it: "minute input rate bits/sec". Only then does it take a number.
            List<string> after = Words(parts, p, 1);
            string unique = !seen.Contains(name) || after.Count == 0 ? name : name + " " + string.Join(" ", after);
            for (int n = 2; !seen.Add(unique); n++)
            {
                unique = name + " " + n.ToString(CultureInfo.InvariantCulture);
            }

            parts[p] = slot with { Name = unique };
        }
    }

    // A group path as a name: each key's words without separators, bullets, or other words with no letters or
    // digits, so "+ System capabilities : / Enabled capabilities:" reads "System capabilities / Enabled capabilities".
    private static string CleanPath(string path) =>
        string.Join(
            RecordLines.PathSeparator,
            path.Split(RecordLines.PathSeparator).Select(static k => string.Join(" ", k.Split(' ').Select(static w => w.TrimEnd(':')).Where(static w => w.Any(char.IsLetterOrDigit))))
        );

    // The literal words next to a slot, walking away from it until another value slot or a comma. Qualifier
    // slots are stepped over, since they sit inside the key.
    private static List<string> Words(List<TemplatePart> parts, int slot, int step)
    {
        List<string> words = [];
        for (int p = slot + step; p >= 0 && p < parts.Count; p += step)
        {
            if (parts[p] is Slot { IsQualifier: true })
            {
                continue;
            }

            if (parts[p] is not Literal { Text: not ("," or ";" or "(" or ")") } literal)
            {
                break;
            }

            // Separators and bullets such as "+" or "*" are not part of a name.
            if (!literal.Text.Any(char.IsLetterOrDigit))
            {
                continue;
            }

            string word = literal.Text.TrimEnd(':');
            if (step < 0)
            {
                words.Insert(0, word);
            }
            else
            {
                words.Add(word);
            }
        }

        return words;
    }

    // len[i, j] is the length of the longest common subsequence of a[i..] and b[j..].
    private static int[,] LcsTable(List<string> a, List<string> b)
    {
        int[,] len = new int[a.Count + 1, b.Count + 1];
        for (int i = a.Count - 1; i >= 0; i--)
        {
            for (int j = b.Count - 1; j >= 0; j--)
            {
                len[i, j] = string.Equals(a[i], b[j], StringComparison.OrdinalIgnoreCase)
                    ? len[i + 1, j + 1] + 1
                    : Math.Max(len[i + 1, j], len[i, j + 1]);
            }
        }

        return len;
    }
}
