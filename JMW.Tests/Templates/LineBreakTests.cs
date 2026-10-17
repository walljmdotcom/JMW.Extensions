using NUnit.Framework;
using System;
using System.IO;
using System.Text;

namespace JMW.Template.Tests
{
    [TestFixture]
    public class LineBreakTests
    {
        private string root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "jmw-template-linebreak-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        private static readonly string[] endings = ["lf", "crlf", "cr", "mixed"];

        // Rewrites every LF in an LF-authored template to the named line ending.
        private static string WithEndings(string lfTemplate, string ending)
        {
            switch (ending)
            {
                case "lf":
                    return lfTemplate;
                case "crlf":
                    return lfTemplate.Replace("\n", "\r\n", StringComparison.Ordinal);
                case "cr":
                    return lfTemplate.Replace("\n", "\r", StringComparison.Ordinal);
            }

            string[] cycle = ["\r\n", "\n", "\r"];
            var sb = new StringBuilder();
            var n = 0;
            foreach (var c in lfTemplate)
            {
                if (c == '\n')
                {
                    sb.Append(cycle[n % cycle.Length]);
                    n++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private string Eval(string template)
        {
            var sb = new StringBuilder();
            var interp = new Interpreter(new StringWriter(sb)) { FileRoot = root };
            interp.AddVariable("foo", "x");
            interp.AddVariable("bar", string.Empty);
            interp.Eval(template);
            return sb.ToString();
        }

        [TestCase("", "")]
        [TestCase("\n", "")]
        [TestCase("\n\n\n", "")]
        [TestCase("a\nb", "ab")]
        [TestCase("\na\n\nb\n", "ab")]
        [TestCase("<var:foo/>\n<var:foo/>\n", "xx")]
        [TestCase("a\\n\nb\\n\n", "a\r\nb\r\n")]
        [TestCase("a\\n\\nb", "a\r\n\r\nb")]
        [TestCase("\\n", "\r\n")]
        [TestCase("# comment\nA\n# another\nB", "AB")]
        [TestCase("A # trailing\nB", "A B")]
        [TestCase("<var:foo\n/>", "x")]
        [TestCase("<var:bar\nvalue='v'\n/>", "v")]
        [TestCase("<var:bar value='a\nb'/>", "ab")]
        [TestCase("<var:bar value='a\\nb'/>", "a\r\nb")]
        [TestCase("<var:bar value='a\\n\nb'/>", "a\r\nb")]
        public void SourceLineBreaksAreDroppedWhateverTheEnding(string lfTemplate, string expected)
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var ending in endings)
                {
                    Assert.That(Eval(WithEndings(lfTemplate, ending)), Is.EqualTo(expected), ending);
                }
            }
        }

        [Test]
        public void IncludedFileGivesTheSameOutputWhateverTheEnding()
        {
            using (Assert.EnterMultipleScope())
            {
                foreach (var ending in endings)
                {
                    File.WriteAllText(Path.Combine(root, "inc.tmpl"), WithEndings("A\n<var:foo/>\\n\n# note\nB\n", ending));
                    Assert.That(Eval("<include path='inc.tmpl'/>\n"), Is.EqualTo("Ax\r\nB"), ending);
                }
            }
        }

        [TestCase("a\nb")]
        [TestCase("a\r\nb")]
        [TestCase("a\rb")]
        [TestCase("<foo\nbar='x'\r\n/>\\n")]
        [TestCase("")]
        public void ParseFromReaderMatchesParseFromString(string template)
        {
            var fromReader = new Parser().Parse(new StringReader(template));
            var fromString = new Parser().Parse(template);

            Assert.That(fromReader.ConvertAll(t => t.TokenText), Is.EqualTo(fromString.ConvertAll(t => t.TokenText)));
            Assert.That(fromReader.ConvertAll(t => t.Name), Is.EqualTo(fromString.ConvertAll(t => t.Name)));
        }
    }
}
