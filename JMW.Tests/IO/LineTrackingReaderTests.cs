using NUnit.Framework;
using System.IO;
using System.Text;

namespace JMW.Tests
{
    [TestFixture]
    public class LineTrackingReaderTests
    {
        // reads `pushed` chars, pushes them back, then drains the reader with a buffer of `bufferSize`
        [TestCase("abcdefgh", 3, 1)]
        [TestCase("abcdefgh", 3, 2)]
        [TestCase("abcdefgh", 3, 3)]
        [TestCase("abcdefgh", 3, 4)]
        [TestCase("abcdefgh", 3, 100)]
        [TestCase("abcdefgh", 5, 2)]
        [TestCase("abcdefgh", 8, 3)]
        [TestCase("abcdefgh", 0, 3)]
        [TestCase("v", 1, 1)]
        [TestCase("", 0, 3)]
        [TestCase("x\ny\nz\na\nb", 5, 2)]
        [TestCase("x\ny\nz\na\nb", 5, 1)]
        public void ReadReturnsPushedBackTextThenTheRest(string text, int pushed, int bufferSize)
        {
            var reader = new LineTrackingReader(new StringReader(text));
            var first = new StringBuilder();
            for (var i = 0; i < pushed; i++)
                first.Append((char)reader.Read());

            reader.PushBack(first.ToString().ToCharArray());
            Assert.That(reader.Line, Is.EqualTo(0));
            Assert.That(reader.Column, Is.EqualTo(0));

            var result = new StringBuilder();
            var buffer = new char[bufferSize];
            int n;
            while ((n = reader.Read(buffer)) > 0)
            {
                Assert.That(n, Is.LessThanOrEqualTo(bufferSize));
                result.Append(buffer, 0, n);
            }

            var expected = new LineTrackingReader(new StringReader(text));
            while (expected.Read() != -1)
            {
            }

            Assert.That(result.ToString(), Is.EqualTo(text));
            Assert.That(reader.Line, Is.EqualTo(expected.Line));
            Assert.That(reader.Column, Is.EqualTo(expected.Column));
        }

        // reads all of `text`, pushes it back in chunks of `chunk` from the end, then reads it again one char at a time
        [TestCase("abcdefgh", 1)]
        [TestCase("abcdefgh", 3)]
        [TestCase("abcdefgh", 8)]
        [TestCase("ab\ncd\n\nef", 1)]
        [TestCase("ab\ncd\n\nef", 2)]
        [TestCase("\n\n\n", 2)]
        public void LayeredPushBacksReadInOriginalOrder(string text, int chunk)
        {
            var reader = new LineTrackingReader(new StringReader(text));
            var lines = new int[text.Length + 1];
            var columns = new int[text.Length + 1];
            for (var i = 0; i < text.Length; i++)
            {
                lines[i] = reader.Line;
                columns[i] = reader.Column;
                reader.Read();
            }
            lines[text.Length] = reader.Line;
            columns[text.Length] = reader.Column;

            for (var end = text.Length; end > 0; end -= chunk)
            {
                var start = end - chunk < 0 ? 0 : end - chunk;
                reader.PushBack(text.Substring(start, end - start).ToCharArray());
                Assert.That(reader.Line, Is.EqualTo(lines[start]));
                Assert.That(reader.Column, Is.EqualTo(columns[start]));
            }

            var result = new StringBuilder();
            int c;
            while ((c = reader.Read()) != -1)
                result.Append((char)c);

            Assert.That(result.ToString(), Is.EqualTo(text));
            Assert.That(reader.Line, Is.EqualTo(lines[text.Length]));
            Assert.That(reader.Column, Is.EqualTo(columns[text.Length]));
        }
    }
}
