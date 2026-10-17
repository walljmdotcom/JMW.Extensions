using System;
using System.IO;
using System.Text;

namespace JMW;

/// <summary>
/// Character-level reading for hand-written tokenizers: peek, read, match, read-while, read-until, and push back,
/// with line and column tracking from <see cref="LineTrackingReader"/>.
/// </summary>
/// <remarks>
/// End of input and U+FFFF are detected in <see cref="TryRead"/> and nowhere else. Every other member reports the
/// end through a <see langword="false"/> return, so callers never compare against the <c>-1</c> sentinel.
/// </remarks>
internal sealed class Scanner : LineTrackingReader
{
    private const int EndOfInput = -1;

    // A decoder emits U+FFFF for bytes it could not map, so the input was read with the wrong encoding.
    private const char InvalidCharacter = (char)0xFFFF;

    private readonly string invalidCharacterMessage;

    /// <param name="reader">The input.</param>
    /// <param name="invalidCharacterMessage">The message thrown when the input contains U+FFFF.</param>
    public Scanner(TextReader reader, string invalidCharacterMessage)
        : base(reader)
    {
        this.invalidCharacterMessage = invalidCharacterMessage;
    }

    /// <summary>True when no characters remain.</summary>
    public bool AtEnd => !this.TryPeek(out _);

    /// <summary>Reads the next character. Returns false at the end of input.</summary>
    /// <exception cref="InvalidDataException">The next character is U+FFFF.</exception>
    public bool TryRead(out char c)
    {
        int next = this.Read();
        if (next == EndOfInput)
        {
            c = default;
            return false;
        }

        if (next == InvalidCharacter)
        {
            throw new InvalidDataException(this.invalidCharacterMessage);
        }

        c = (char)next;
        return true;
    }

    /// <summary>Returns the next character without consuming it. Returns false at the end of input.</summary>
    public bool TryPeek(out char c)
    {
        if (!this.TryRead(out c))
        {
            return false;
        }

        this.PushBack(c);
        return true;
    }

    /// <summary>
    /// Consumes <paramref name="text"/> when the input continues with it. Otherwise leaves the input unchanged.
    /// </summary>
    public bool TryMatch(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (!this.TryRead(out char c))
            {
                this.PushBackRange(text, i);
                return false;
            }

            if (c != text[i])
            {
                this.PushBack(c);
                this.PushBackRange(text, i);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Consumes characters while <paramref name="predicate"/> holds, appending them to <paramref name="into"/> when
    /// given. The first character that fails the predicate is left unread.
    /// </summary>
    /// <returns>The number of characters consumed.</returns>
    public int ReadWhile(Func<char, bool> predicate, StringBuilder? into = null)
    {
        int count = 0;
        while (this.TryRead(out char c))
        {
            if (!predicate(c))
            {
                this.PushBack(c);
                break;
            }

            into?.Append(c);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Consumes characters up to and including <paramref name="terminator"/>, appending everything before it to
    /// <paramref name="into"/>.
    /// </summary>
    /// <returns>True when the terminator was found; false when the input ended first.</returns>
    public bool ReadUntil(char terminator, StringBuilder into)
    {
        while (this.TryRead(out char c))
        {
            if (c == terminator)
            {
                return true;
            }

            into.Append(c);
        }

        return false;
    }

    private void PushBackRange(string text, int length)
    {
        for (int i = length - 1; i >= 0; i--)
        {
            this.PushBack(text[i]);
        }
    }
}
