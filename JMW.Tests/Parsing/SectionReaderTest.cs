using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JMW.Parsing.Compile;
using JMW.Parsing.Expressions;
using NUnit.Framework;

namespace JMW.Parsing.Tests
{
    [TestFixture]
    public class SectionReaderTest
    {
        [Test]
        public void ParseSectionTest1()
        {
            var text = @"
blah
blah
foo
start
 this is 0 a line
 this is 0 another line
start
 this is 5 foo
 this is 5 more foo
bar
start
 this is 6 foo
 this is 6 more foo
";
            var start = new StartsWith(new Parser().Parse(@"startswith {s:[""foo""]}").First());
            var stop = new StartsWith(new Parser().Parse(@"startswith {s:[""bar""]}").First());
            var lines = new List<string>();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            using (var sr = new StreamReader(ms))
            {
                var rdr = new SectionReader(sr, start, stop);
                string? line;
                while ((line = rdr.ReadLine()) is not null)
                {
                    lines.Add(line);
                }
            }

            Assert.That(lines.Count, Is.EqualTo(6));
            Assert.That(lines[0], Is.EqualTo("start"));
            Assert.That(lines[5], Is.EqualTo(" this is 5 more foo"));
        }

        [Test]
        public void ParseSectionTest2()
        {
            var text = @"
blah
blah
foo
start
 this is 0 a line
 this is 0 another line
start
 this is 5 foo
 this is 5 more foo
bar
start
 this is 6 foo
 this is 6 more foo
";
            var start = new StartsWith(new Parser().Parse(@"startswith {s:[""foo""]}").First());
            var stop = new StartsWith(new Parser().Parse(@"startswith {s:[""bar""]}").First());
            var lines = new List<string>();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            using (var sr = new StreamReader(ms))
            {
                var rdr = new SectionReader(sr, start, stop, true, true);
                string? line;
                while ((line = rdr.ReadLine()) is not null)
                {
                    lines.Add(line);
                }
            }

            Assert.That(lines.Count, Is.EqualTo(8));
            Assert.That(lines[0], Is.EqualTo("foo"));
            Assert.That(lines[7], Is.EqualTo("bar"));
        }


        [Test]
        public void ParseSectionTest3()
        {
            var text = @"
blah
blah
foo
start
 this is 0 a line
 this is 0 another line
start
 this is 5 foo
 this is 5 more foo
bar
start
 this is 6 foo
 this is 6 more foo
";
            var lines = new List<string>();

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            using (var sr = new StreamReader(ms))
            {
                var rdr = new SectionReader(sr, null, null);
                string? line;
                while ((line = rdr.ReadLine()) is not null)
                {
                    lines.Add(line);
                }
            }

            Assert.That(lines.Count, Is.EqualTo(14));
            Assert.That(lines[0], Is.EqualTo(""));
            Assert.That(lines[13], Is.EqualTo(" this is 6 more foo"));
        }
    }
}