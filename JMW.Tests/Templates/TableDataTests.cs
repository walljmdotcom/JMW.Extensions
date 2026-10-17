using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace JMW.Templates.Tests
{
    /// <summary>
    /// The CSV constructor reads RFC 4180 fields, one row per line.
    /// </summary>
    [TestFixture]
    public class TableDataTests
    {
        [TestCase("a,b,c", new[] { "a", "b", "c" })]
        [TestCase("\"a,b\",c", new[] { "a,b", "c" })]
        [TestCase("c,\"a,b\"", new[] { "c", "a,b" })]
        [TestCase("\"a,b\"", new[] { "a,b" })]
        [TestCase("\"a\"\"b\",c", new[] { "a\"b", "c" })]
        [TestCase("\"\"\"\",x", new[] { "\"", "x" })]
        [TestCase("\"\",x", new[] { "", "x" })]
        [TestCase("x,\"\"", new[] { "x", "" })]
        [TestCase("a,", new[] { "a", "" })]
        [TestCase(",", new[] { "", "" })]
        [TestCase("", new[] { "" })]
        [TestCase(" a , b ", new[] { " a ", " b " })]
        [TestCase("\" a \",b", new[] { " a ", "b" })]
        // A quote that does not open the field is part of it.
        [TestCase("a\"b,c", new[] { "a\"b", "c" })]
        [TestCase(" \"a\",b", new[] { " \"a\"", "b" })]
        // Text after a closing quote is kept, as most readers do.
        [TestCase("\"a\"b,c", new[] { "ab", "c" })]
        public void ReadsFields(string line, string[] expected)
        {
            var table = new TableData("t", ["h", line]);

            Assert.That(table.Data, Has.Count.EqualTo(1));
            Assert.That(table.Data[0], Is.EqualTo(expected));
        }

        [TestCase("\"last, first\",id", new[] { "last, first", "id" })]
        [TestCase("\"a\"\"b\",c", new[] { "a\"b", "c" })]
        public void ReadsHeaderFields(string header, string[] expected)
        {
            var table = new TableData("t", [header]);

            Assert.That(table.Columns, Is.EqualTo(expected));
            Assert.That(table.ColumnIndexes[expected[0].ToLowerInvariant()], Is.EqualTo(0));
        }

        [Test]
        public void DropsByteOrderMarkFromHeader()
        {
            var table = new TableData("t", [(char)0xFEFF + "num,name", "10,a"]);

            Assert.That(table.Columns, Is.EqualTo(new List<string> { "num", "name" }));
            Assert.That(table.ColumnIndexes["num"], Is.EqualTo(0));
        }

        // Rows arrive one per line, so a field left open cannot continue onto the next row.
        [TestCase("\"abc")]
        [TestCase("a,\"b")]
        [TestCase("\"a\"\"")]
        [TestCase("\"")]
        public void RejectsUnterminatedQuote(string line)
        {
            Assert.That(() => new TableData("t", ["h", line]), Throws.TypeOf<FormatException>());
        }

        [Test]
        public void RejectsUnterminatedQuoteInHeader()
        {
            Assert.That(() => new TableData("t", ["\"h"]), Throws.TypeOf<FormatException>());
        }
    }
}
