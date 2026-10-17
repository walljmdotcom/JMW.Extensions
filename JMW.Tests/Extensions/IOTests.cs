using NUnit.Framework;
using System;
using System.IO;

namespace JMW.Extensions.Tests
{
    [TestFixture]
    public class IOTests
    {
        [Test]
        public void TailTest()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..","..","Extensions","lines.txt");
            using var r = new StreamReader(path);
            var last = r.Tail(5);
            Assert.That(last.Length, Is.EqualTo(5));
            Assert.That(last[0], Is.EqualTo("foo bar 6"));
            Assert.That(last[1], Is.EqualTo("foo bar 7"));
            Assert.That(last[2], Is.EqualTo("foo bar 8"));
            Assert.That(last[3], Is.EqualTo("foo bar 9"));
            Assert.That(last[4], Is.EqualTo("foo bar 10"));
        }

        [TestCase("", 0, new string[0])]
        [TestCase("a\nb\nc", 0, new string[0])]
        [TestCase("", 3, new string[0])]
        [TestCase("a", 1, new[] { "a" })]
        [TestCase("a\nb\nc", 1, new[] { "c" })]
        [TestCase("a\nb\nc", 2, new[] { "b", "c" })]
        [TestCase("a\nb\nc", 3, new[] { "a", "b", "c" })]
        [TestCase("a\nb\nc", 5, new[] { "a", "b", "c" })]
        public void TailCounts(string text, int count, string[] expected)
        {
            using var r = new StringReader(text);
            Assert.That(r.Tail(count), Is.EqualTo(expected));
        }

        [Test]
        public void TailNegativeCount()
        {
            using var r = new StringReader("a");
            Assert.That(() => r.Tail(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TailTestLong()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..","..","Extensions","lines_long.txt");
            using var r = new StreamReader(path);
            var last = r.Tail(5);
            Assert.That(last.Length, Is.EqualTo(5));
            Assert.That(last[0], Is.EqualTo("foo bar 6"));
            Assert.That(last[1], Is.EqualTo("foo bar 7"));
            Assert.That(last[2], Is.EqualTo("foo bar 8"));
            Assert.That(last[3], Is.EqualTo("foo bar 9"));
            Assert.That(last[4], Is.EqualTo("foo bar 10"));
        }
    }
}