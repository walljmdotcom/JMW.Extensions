using System.Collections.Generic;
using System.IO;

namespace JMW
{
    /// <summary>
    /// A wrapper for a TextReader that keeps track line and column as well as allowing
    /// you to unread text using a PushBack method.
    /// </summary>
    public class LineTrackingReader
    {
        private readonly TextReader _reader;

        // Pushed-back characters, the next one to read on top.
        private readonly Stack<char> _buffer = new();

        /// <summary>
        /// The, currently read to, Line in our reader.
        /// </summary>
        public int Line { get; private set; } = 0;

        /// <summary>
        /// The, currently read to, column in our reader.
        /// </summary>
        /// <value>The column.</value>
        public int Column { get; private set; } = 0;

        private readonly Stack<int> _lineLengths = new();

        public LineTrackingReader(TextReader reader)
        {
            _reader = reader;
        }

        public int Read()
        {
            int c = _buffer.Count > 0 ? _buffer.Pop() : _reader.Read();
            if (c != -1)
            {
                advance((char)c);
            }
            return c;
        }

        /// <summary>
        /// Read from the wrapped reader into the provided buffer while updating the line
        /// and column.
        /// </summary>
        /// <param name="buffer">The char buffer to read text into.
        /// Overwrites the contents if there was any.</param>
        public int Read(char[] buffer)
        {
            var n = 0;
            while (n < buffer.Length && _buffer.Count > 0)
            {
                buffer[n++] = _buffer.Pop();
            }
            if (n < buffer.Length)
            {
                n += _reader.Read(buffer, n, buffer.Length - n);
            }
            for (var i = 0; i < n; i++)
            {
                advance(buffer[i]);
            }
            return n;
        }

        /// <summary>
        /// Pushes back text into the reader to be read again while updating the line and
        /// column numbers.
        /// This method assumes you are pushing previously read text back in.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        public void PushBack(char[] buffer)
        {
            for (var i = buffer.Length - 1; i >= 0; i--)
            {
                PushBack(buffer[i]);
            }
        }

        public void PushBack(char c)
        {
            retreat(c);
            _buffer.Push(c);
        }

        private void advance(char c)
        {
            if (c == '\n')
            {
                Line++;
                _lineLengths.Push(Column);
                Column = 0;
            }
            else
            {
                Column++;
            }
        }

        private void retreat(char c)
        {
            if (c == '\n')
            {
                Line--;
                Column = _lineLengths.Pop();
            }
            else
            {
                Column--;
            }
        }
    }
}
