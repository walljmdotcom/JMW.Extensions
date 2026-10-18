namespace JMW.TextExtraction.Text;

/// <summary>A word of a line and where it starts.</summary>
public readonly record struct Token(int Start, string Text)
{
    public int End => this.Start + this.Text.Length;
}

public static class Tokenizer
{
    private static readonly System.Buffers.SearchValues<char> Letters =
        System.Buffers.SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");

    private static readonly System.Buffers.SearchValues<char> LowerLetters =
        System.Buffers.SearchValues.Create("abcdefghijklmnopqrstuvwxyz");

    /// <summary>
    /// Splits a line on spaces. An <c>=</c> inside a word and a trailing <c>,</c> or <c>;</c> become tokens of
    /// their own, so <c>mtu=1500,</c> gives <c>mtu</c>, <c>=</c>, <c>1500</c>, <c>,</c>. A colon with no space
    /// after it splits too when the word before it is all letters, so <c>ID:sw1</c> gives <c>ID:</c>,
    /// <c>sw1</c>, and a leading colon splits off, so <c>:f8b1</c> gives <c>:</c>, <c>f8b1</c>. A colon right
    /// after a closing parenthesis splits off, and so do parentheses that open or close a word, so
    /// <c>(MAC address):</c> gives <c>(</c>, <c>MAC</c>, <c>address</c>, <c>)</c>, <c>:</c>. Times, MAC
    /// addresses, URLs, and parentheses inside a word, as in <c>address(es)</c>, stay whole.
    /// </summary>
    /// <param name="line">A line passed through <see cref="Lines.Normalize(string)"/>, so it holds no tabs.</param>
    public static List<Token> Tokenize(string line)
    {
        ArgumentNullException.ThrowIfNull(line);
        List<Token> tokens = [];
        int i = 0;
        while (i < line.Length)
        {
            if (line[i] == ' ')
            {
                i++;
                continue;
            }

            // Every later stage compares lines word by word, some of it in time that grows with the square of the
            // count, so the rest of a line past a few hundred words, as a configuration printed on one line, is
            // one word.
            if (tokens.Count >= MaxTokens)
            {
                tokens.Add(new Token(i, line[i..].TrimEnd()));
                break;
            }

            int start = i;
            while (i < line.Length && line[i] != ' ')
            {
                i++;
            }

            Split(line, start, i, tokens);
        }

        DropLeaders(tokens);
        return tokens;
    }

    // A dot leader runs from a key to its colon, as "Physical Address. . . . :" or "Host Name.......:", and is
    // padding, not words. It goes, with the dots it leaves on the key's last word, so the key reads "Physical
    // Address :".
    private static void DropLeaders(List<Token> tokens)
    {
        // A leader with no colon after it is the separator, as in "System Name........ c5508", so it reads as one.
        for (int i = 0; i < tokens.Count; i++)
        {
            // Glued to its colon, as "Name.......:", the leader goes and the colon stays.
            string glued = tokens[i].Text;
            int body = glued.Length - 1;
            if (body > 0 && glued[^1] == ':' && glued.AsSpan(0, body).TrimEnd('.').Length is var kept && kept > 0 && body - kept >= 2)
            {
                tokens[i] = tokens[i] with { Text = glued[..kept] + ":" };
            }
        }

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            string text = tokens[i].Text;
            int keep = text.AsSpan().TrimEnd('.').Length;
            if (keep > 0 && text.Length - keep >= MinLeader && tokens[i + 1].Text != ":" && char.IsLetter(text[keep - 1]))
            {
                tokens[i] = tokens[i] with { Text = text[..keep] };
                tokens.Insert(i + 1, new Token(tokens[i].Start + keep, ":"));
                i++;
            }
        }

        for (int i = 1; i < tokens.Count; i++)
        {
            if (tokens[i].Text != ":")
            {
                continue;
            }

            int from = i;
            while (from > 0 && IsDots(tokens[from - 1].Text))
            {
                from--;
            }

            bool leader = from < i;
            tokens.RemoveRange(from, i - from);
            i = from;
            if (i == 0)
            {
                continue;
            }

            // After a leader, one dot on the word goes too ("Address. . :"); on its own it takes a run of them,
            // so an abbreviation such as "No.:" keeps its dot.
            string word = tokens[i - 1].Text;
            int keep = word.AsSpan().TrimEnd('.').Length;
            if (keep > 0 && keep < word.Length && (leader || word.Length - keep >= 2))
            {
                tokens[i - 1] = tokens[i - 1] with { Text = word[..keep] };
            }
        }
    }

    // Longer than an ellipsis, as in "Loading... done".
    private const int MinLeader = 4;

    private static bool IsDots(string text) => text.AsSpan().IndexOfAnyExcept('.') < 0;

    private static void Split(string line, int start, int end, List<Token> tokens)
    {
        if (end - start > 1 && line[start] == ':' && line[start + 1] != ':')
        {
            tokens.Add(new Token(start, ":"));
            start++;
        }

        int colon = line.IndexOf(':', start, end - start);
        if (colon > start + 1 && colon < end - 1 && line[colon + 1] is not ('/' or ':')
            && line.AsSpan(start, colon - start).IndexOfAnyExcept(Letters) < 0
            && !IsHexPairs(line.AsSpan(start, end - start)))
        {
            tokens.Add(new Token(start, line[start..(colon + 1)]));
            start = colon + 1;
        }

        int trail = end - start > 1 && line[end - 1] is ',' or ';' ? end - 1 : end;

        // "address):" closes a parenthetical and then separates, so the colon stands alone.
        if (trail - start > 2 && line[trail - 1] == ':' && line[trail - 2] == ')')
        {
            trail--;
        }

        // Parentheses that open or close a word stand alone, so "(0 IP multicasts)" holds the words 0, IP, and
        // multicasts. One inside a word, as in "address(es)" or "Gi1/0/1(connected)", stays put.
        while (trail - start > 1 && line[start] == '(')
        {
            tokens.Add(new Token(start, "("));
            start++;
        }

        // A word with a parenthetical glued on, as "Gi1/0/1(connected)" or "Po1(SU)", is a value and its status.
        // A short lowercase ending on a plain word, as "address(es)" or "interface(s)", only makes it plural.
        int open = trail - start > 2 && line[trail - 1] == ')' ? line.IndexOf('(', start + 1, trail - start - 1) : -1;
        if (open > start && line.IndexOf('(', open + 1, trail - open - 1) < 0 && !IsPluralEnding(line, start, open, trail))
        {
            Add(line, start, open, tokens);
            tokens.Add(new Token(open, "("));
            if (trail - open > 2)
            {
                tokens.Add(new Token(open + 1, line[(open + 1)..(trail - 1)]));
            }

            tokens.Add(new Token(trail - 1, ")"));
            if (trail < end)
            {
                tokens.Add(new Token(trail, line[trail..end]));
            }

            return;
        }

        int close = trail - start > 1 && line[trail - 1] == ')' && line.IndexOf('(', start, trail - start) < 0 ? trail - 1 : trail;
        Add(line, start, close, tokens);
        if (close < trail)
        {
            tokens.Add(new Token(close, ")"));
        }

        if (trail < end)
        {
            tokens.Add(new Token(trail, line[trail..end]));
        }
    }

    // Adds line[start..end] as a token, split around an "=" inside it.
    private static void Add(string line, int start, int end, List<Token> tokens)
    {
        int eq = line.IndexOf('=', start, end - start);
        if (eq > start && eq < end - 1)
        {
            tokens.Add(new Token(start, line[start..eq]));
            tokens.Add(new Token(eq, "="));
            tokens.Add(new Token(eq + 1, line[(eq + 1)..end]));
        }
        else
        {
            tokens.Add(new Token(start, line[start..end]));
        }
    }

    private const int MaxTokens = 256;

    // A MAC or IPv6 address whose first group is letters, as "ff:ff:ff:ff:ff:ff" or "fdab:cd00::1", is a value,
    // not "ff:" keying the rest.
    private static bool IsHexPairs(ReadOnlySpan<char> word)
    {
        int slash = word.IndexOf('/');
        word = slash >= 0 ? word[..slash] : word;
        int parts = 0;
        foreach (Range range in word.Split(':'))
        {
            ReadOnlySpan<char> part = word[range];
            if (part.Length > 4 || part.IndexOfAnyExcept(HexDigits) >= 0)
            {
                return false;
            }

            parts++;
        }

        return parts >= 3;
    }

    private static readonly System.Buffers.SearchValues<char> HexDigits =
        System.Buffers.SearchValues.Create("0123456789ABCDEFabcdef");

    private const int MaxPluralEnding = 3;

    private static bool IsPluralEnding(string line, int start, int open, int trail)
    {
        int inner = trail - 1 - (open + 1);
        return inner is > 0 and <= MaxPluralEnding
            && line.AsSpan(start, open - start).IndexOfAnyExcept(Letters) < 0
            && line.AsSpan(open + 1, inner).IndexOfAnyExcept(LowerLetters) < 0;
    }
}
