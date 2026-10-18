namespace JMW.TextExtraction.Text;

/// <summary>
/// The key of a line: the words that say what kind of line it is, so the same kind of line can be found again
/// in another record or another sample.
/// </summary>
public static class LineKeys
{
    private const int KeyReach = 6;

    // Enough to part "36 Number of requested variables" from "0 Number of altered variables".
    private const int CountedWords = 4;

    /// <summary>
    /// Every key phrase on the line: the words before each separator (<c>:</c>, <c>=</c>, or a word ending in a
    /// colon), back to the start, a comma, or the previous separator's value. So <c>Interface: Te5/3,  Port ID
    /// (outgoing port): Gi1/0/6</c> is keyed <c>Interface: Port ID :</c> and does not group with a plain
    /// <c>Interface: Te5/3</c>. The first separator must come within the first few words. A line with none is
    /// keyed by its first word. A line starting with a number is keyed by the number's shape and up to four plain
    /// words after it, so <c>5 minute input rate</c> and <c>5 minute output rate</c> stay apart while every
    /// address-led line groups together. A parenthetical in a key phrase qualifies the value rather than naming
    /// the key, so <c>Chassis ID (MAC address):</c> and <c>Chassis ID (locally assigned):</c> share the key
    /// <c>Chassis ID :</c>.
    /// </summary>
    public static string Key(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (SeparatorIndex(tokens) >= 0)
        {
            List<string> phrases = [];
            int start = 0;
            for (int i = 0; i < tokens.Count; i++)
            {
                string t = tokens[i].Text;
                if (t is "," or ";")
                {
                    start = i + 1;
                }
                else if (IsSeparator(t))
                {
                    bool[] qualifier = Qualifiers(tokens, start, i);
                    if (qualifier.Count(static q => !q) <= KeyReach)
                    {
                        IEnumerable<string> phrase = tokens
                            .Skip(start)
                            .Take(i - start + 1)
                            .Where((_, j) => !qualifier[j])
                            .Select(static x => Cells.IsNumeric(x.Text) ? "~" : Masked(x.Text));
                        phrases.Add(string.Join(" ", phrase));
                    }

                    start = i + 1;
                }
            }

            return string.Join(" ", phrases);
        }

        string first = tokens[0].Text;
        if (!Cells.IsNumeric(first))
        {
            // "cisco id is --" and "cisco extended id number is 4" are two kinds of line. A subject holding a
            // name, as "Ethernet1/1 is up", keeps the first word, so every such line groups.
            int verb = 1;
            while (verb < tokens.Count - 1 && verb <= KeyReach && tokens[verb].Text.All(char.IsLetter) && tokens[verb].Text is not ("is" or "are"))
            {
                verb++;
            }

            return verb > 1 && verb < tokens.Count - 1 && tokens[verb].Text is "is" or "are" && first.All(char.IsLetter)
                ? string.Join(" ", tokens.Take(verb).Select(static t => t.Text))
                : first;
        }

        // The plain words after the number name the line; a value among them, as the address in "10 permit
        // 198.51.100.0", would key every line differently.
        IEnumerable<string> words = tokens.Skip(1).Take(CountedWords).TakeWhile(static t => t.Text.All(char.IsLetter)).Select(static t => t.Text);
        return string.Join(" ", words.Prepend("~" + Cells.Shape(first)));
    }

    // A quoted word in a key phrase is a name, as "dmz-a" in 'Traffic Statistics for "dmz-a":', so it varies.
    // An interface or slot name in a key phrase, as "GigabitEthernet1/0/1" in "GigabitEthernet1/0/1 current
    // state:", varies the same way, and holds a digit and a slash where a key word such as "IPv4" or "TOS/TAS"
    // does not hold both.
    private static string Masked(string text)
    {
        if (text.Contains('/', StringComparison.Ordinal) && text.Any(char.IsAsciiDigit) && text.Any(char.IsLetter))
        {
            return "~" + (text.EndsWith(':') ? ":" : "");
        }

        string bare = text.TrimEnd(':');
        return bare.Length >= 2 && bare[0] is '"' or '\'' && bare[^1] == bare[0] ? "~" + text[bare.Length..] : text;
    }

    /// <summary>
    /// Marks the tokens from <paramref name="start"/> up to <paramref name="end"/> that sit inside a
    /// parenthetical, from a word opening with <c>(</c> through the word closing with <c>)</c>.
    /// </summary>
    public static bool[] Qualifiers(IReadOnlyList<Token> tokens, int start, int end)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        bool[] inside = new bool[end - start + 1];
        bool open = false;
        for (int i = start; i <= end; i++)
        {
            string t = tokens[i].Text;
            open |= t.StartsWith('(');
            inside[i - start] = open;
            if (open && t.EndsWith(')'))
            {
                open = false;
            }
        }

        return inside;
    }

    /// <summary>True for <c>:</c>, <c>=</c>, or a word with letters that ends in its only colon, like <c>Port id:</c>.</summary>
    public static bool IsSeparator(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return token is ":" or "="
            || (token.Length > 1 && token.IndexOf(':', StringComparison.Ordinal) == token.Length - 1 && token.Any(char.IsLetter));
    }

    /// <summary>
    /// Whether the line reads as <c>key: value</c>: a separator within the first few words, with no comma before
    /// it. <c>Cisco AP Software, ap3g3-k9w8 Version: 8.10</c> holds a colon, but it is a sentence.
    /// </summary>
    public static bool IsKeyValue(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        int separator = SeparatorIndex(tokens);
        return separator >= 0 && !tokens.Take(separator).Any(static t => t.Text is "," or ";");
    }

    /// <summary>
    /// A second key for a line that opens with a name, as <c>dist-sw1 uptime is 2 years</c>: the two words after
    /// the first, so the line is found again in output from another device. Null for a line with a separator,
    /// one led by a number, or one too short to have the words.
    /// </summary>
    public static string? NameLedKey(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (tokens.Count < 3 || SeparatorIndex(tokens) >= 0 || Cells.IsNumeric(tokens[0].Text))
        {
            return null;
        }

        return "* " + string.Join(" ", tokens.Skip(1).Take(2).Select(static t => Cells.IsNumeric(t.Text) ? "~" : t.Text));
    }

    /// <summary>The index of the first separator within the first few words, or -1 when there is none.</summary>
    public static int SeparatorIndex(IReadOnlyList<Token> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        int words = 0;
        for (int i = 0; i < tokens.Count && words < KeyReach; i++)
        {
            if (IsSeparator(tokens[i].Text))
            {
                return i;
            }

            words += InParenthetical(tokens, i) ? 0 : 1;
        }

        return -1;
    }

    // Words of a parenthetical, such as "(MAC address)" or "(outgoing port)", do not count toward how far into a
    // line a key may reach.
    private static bool InParenthetical(IReadOnlyList<Token> tokens, int index)
    {
        int depth = 0;
        for (int i = 0; i <= index; i++)
        {
            string t = tokens[i].Text;
            if (t.StartsWith('('))
            {
                depth++;
            }

            if (i == index)
            {
                return depth > 0;
            }

            if (t.EndsWith(')') && depth > 0)
            {
                depth--;
            }
        }

        return false;
    }
}
