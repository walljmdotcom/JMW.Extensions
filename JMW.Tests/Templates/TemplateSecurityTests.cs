using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using JMW.Templates.Tags;

namespace JMW.Templates.Tests
{
    [TestFixture]
    public class TemplateSecurityTests
    {
        private string root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            root = Path.Combine(Path.GetTempPath(), "jmw-template-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }

        private (Interpreter Interp, StringBuilder Output) NewInterpreter()
        {
            var sb = new StringBuilder();
            var interp = new Interpreter(new StringWriter(sb)) { FileRoot = root };

            // <var:bar value='...'/> prints its value attribute after substitution, but bar must be declared.
            interp.AddVariable("bar", string.Empty);
            return (interp, sb);
        }

        // Runs on a worker task so a regression that loops forever fails the test instead of hanging the run.
        private static void RunWithin(Action action, int milliseconds = 5000)
        {
            var task = Task.Run(action);
            if (!task.Wait(milliseconds))
                Assert.Fail("The template did not finish within " + milliseconds + " ms.");
        }

        private string Write(string relativePath, string content)
        {
            var path = Path.Combine(root, relativePath);
            var directory = Path.GetDirectoryName(path);
            if (directory is not null)
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, content);
            return path;
        }

        // Variable substitution

        [TestCase("$&$&")]
        [TestCase("$1")]
        [TestCase("$$")]
        [TestCase("a]b")]
        [TestCase("")]
        [TestCase("plain")]
        public void VariableValuesAreSubstitutedLiterally(string value)
        {
            var (interp, output) = NewInterpreter();
            interp.AddVariable("foo", value);

            RunWithin(() => interp.Eval("<var:bar value='[var:foo]'/>"));

            Assert.That(output.ToString(), Is.EqualTo(value));
        }

        [TestCase("[var:foo]")]
        [TestCase("[VAR:FOO]")]
        [TestCase("[Var:Foo]")]
        public void VariableReferencesAreCaseInsensitive(string reference)
        {
            var (interp, output) = NewInterpreter();
            interp.AddVariable("foo", "x");

            interp.Eval("<var:bar value='" + reference + "'/>");

            Assert.That(output.ToString(), Is.EqualTo("x"));
        }

        [Test]
        public void SeveralReferencesInOneValueAreAllReplaced()
        {
            var (interp, output) = NewInterpreter();
            interp.AddVariable("a", "1");
            interp.AddVariable("b", "2");

            interp.Eval("<var:bar value='[var:a]-[var:b]-[var:a]'/>");

            Assert.That(output.ToString(), Is.EqualTo("1-2-1"));
        }

        [Test]
        public void NestedReferencesExpand()
        {
            var (interp, output) = NewInterpreter();
            interp.AddVariable("inner", "deep");
            interp.AddVariable("outer", "[var:inner]!");

            interp.Eval("<var:bar value='[var:outer]'/>");

            Assert.That(output.ToString(), Is.EqualTo("deep!"));
        }

        [TestCase("[var:self]")]
        [TestCase("x[var:self]")]
        [TestCase("[var:self][var:self]")]
        public void SelfReferencingVariableFailsInsteadOfLooping(string value)
        {
            var (interp, _) = NewInterpreter();
            interp.AddVariable("self", value);

            RunWithin(() => Assert.That(
                () => interp.Eval("<var:bar value='[var:self]'/>"),
                Throws.Exception.With.Message.Contains("did not finish expanding")
            ));
        }

        [Test]
        public void MutuallyReferencingVariablesFailInsteadOfLooping()
        {
            var (interp, _) = NewInterpreter();
            interp.AddVariable("a", "[var:b]");
            interp.AddVariable("b", "[var:a]");

            RunWithin(() => Assert.That(
                () => interp.Eval("<var:bar value='[var:a]'/>"),
                Throws.Exception.With.Message.Contains("did not finish expanding")
            ));
        }

        [Test]
        public void UndeclaredVariableThrows()
        {
            var (interp, _) = NewInterpreter();

            Assert.That(
                () => interp.Eval("<var:bar value='[var:missing]'/>"),
                Throws.Exception.With.Message.EqualTo("Reference to variable which has not been declared: missing.")
            );
        }

        [Test]
        public void UnterminatedReferenceIsLeftAlone()
        {
            var (interp, output) = NewInterpreter();
            interp.AddVariable("foo", "x");

            interp.Eval("<var:bar value='[var:foo'/>");

            Assert.That(output.ToString(), Is.EqualTo("[var:foo"));
        }

        // Include paths

        [Test]
        public void IncludeInsideRootWorks()
        {
            Write(Path.Combine("parts", "a.tmpl"), "A");
            var (interp, output) = NewInterpreter();

            interp.Eval("<include path='" + Path.Combine("parts", "a.tmpl") + "'/>");

            Assert.That(output.ToString(), Is.EqualTo("A"));
        }

        [Test]
        public void IncludeNestedInsideIncludedFileWorks()
        {
            Write("a.tmpl", "A<include path='b.tmpl'/>");
            Write("b.tmpl", "B");
            var (interp, output) = NewInterpreter();

            interp.Eval("<include path='a.tmpl'/>");

            Assert.That(output.ToString(), Is.EqualTo("AB"));
        }

        [Test]
        public void SameFileIncludedTwiceSideBySideIsNotACycle()
        {
            Write("a.tmpl", "A");
            var (interp, output) = NewInterpreter();

            interp.Eval("<include path='a.tmpl'/><include path='a.tmpl'/>");

            Assert.That(output.ToString(), Is.EqualTo("AA"));
        }

        [Test]
        public void IncludeOutsideRootIsRejected()
        {
            var outside = Path.Combine(Path.GetTempPath(), "jmw-outside-" + Guid.NewGuid().ToString("N") + ".tmpl");
            File.WriteAllText(outside, "secret");
            try
            {
                foreach (var path in new[] { outside, Path.Combine("..", Path.GetFileName(outside)) })
                {
                    var (interp, output) = NewInterpreter();
                    Assert.That(() => interp.Eval("<include path='" + path + "'/>"), Throws.InstanceOf<UnauthorizedAccessException>(), path);
                    Assert.That(output.ToString(), Does.Not.Contain("secret"));
                }
            }
            finally
            {
                File.Delete(outside);
            }
        }

        [Test]
        public void SiblingDirectoryWithRootAsPrefixIsRejected()
        {
            var sibling = root + "-sibling";
            Directory.CreateDirectory(sibling);
            try
            {
                File.WriteAllText(Path.Combine(sibling, "a.tmpl"), "secret");
                var (interp, _) = NewInterpreter();

                Assert.That(
                    () => interp.Eval("<include path='" + Path.Combine(sibling, "a.tmpl") + "'/>"),
                    Throws.InstanceOf<UnauthorizedAccessException>()
                );
            }
            finally
            {
                Directory.Delete(sibling, recursive: true);
            }
        }

        [Test]
        public void IncludeOutsideRootIsAllowedWhenRestrictionIsOff()
        {
            var outside = Path.Combine(Path.GetTempPath(), "jmw-outside-" + Guid.NewGuid().ToString("N") + ".tmpl");
            File.WriteAllText(outside, "shared");
            try
            {
                var (interp, output) = NewInterpreter();
                interp.RestrictFilePaths = false;

                interp.Eval("<include path='" + outside + "'/>");

                Assert.That(output.ToString(), Is.EqualTo("shared"));
            }
            finally
            {
                File.Delete(outside);
            }
        }

        [TestCase("a.tmpl", "<include path='a.tmpl'/>")]
        [TestCase("a.tmpl", "x<include path='b.tmpl'/>")]
        public void IncludeCycleFailsInsteadOfLooping(string start, string content)
        {
            Write("a.tmpl", content);
            Write("b.tmpl", "<include path='a.tmpl'/>");
            var (interp, _) = NewInterpreter();

            RunWithin(() => Assert.That(
                () => interp.Eval("<include path='" + start + "'/>"),
                Throws.Exception.With.Message.Contains("include each other in a loop")
            ));
        }

        [Test]
        public void NamedIncludeCycleFailsInsteadOfLooping()
        {
            var (interp, _) = NewInterpreter();

            RunWithin(() => Assert.That(
                () => interp.Eval("<include define='one'><include name='one'/></include><include name='one'/>"),
                Throws.Exception.With.Message.Contains("include each other in a loop")
            ));
        }

        [TestCase("missing.tmpl")]
        [TestCase("missing-dir/missing.tmpl")]
        public void MissingIncludeReportsThatTheTemplateDoesNotExist(string path)
        {
            var (interp, _) = NewInterpreter();

            Assert.That(
                () => interp.Eval("<include path='" + path + "'/>"),
                Throws.Exception.With.Message.Contains("refers to a template which does not exist")
            );
        }

        // Output paths

        [TestCase("new")]
        [TestCase("append")]
        public void OutputOutsideRootIsRejected(string mode)
        {
            var (interp, _) = NewInterpreter();

            Assert.That(
                () => interp.Eval("<output filename='" + Path.Combine("..", "escaped.txt") + "' mode='" + mode + "'>x</output>"),
                Throws.InstanceOf<UnauthorizedAccessException>()
            );
            Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(root) ?? root, "escaped0.txt")), Is.False);
        }

        [Test]
        public void NewModeReplacesAnExistingLongerFile()
        {
            Write("out0.txt", "a much longer previous file");
            var (interp, _) = NewInterpreter();

            interp.Eval("<output filename='out.txt' mode='new'>short</output>");

            Assert.That(File.ReadAllText(Path.Combine(root, "out0.txt")), Is.EqualTo("short"));
        }

        [Test]
        public void NewModeKeepsTheDirectory()
        {
            Directory.CreateDirectory(Path.Combine(root, "sub"));
            var (interp, _) = NewInterpreter();

            interp.Eval("<output filename='" + Path.Combine("sub", "out.txt") + "' mode='new'>x</output>");

            Assert.That(File.ReadAllText(Path.Combine(root, "sub", "out0.txt")), Is.EqualTo("x"));
            Assert.That(File.Exists(Path.Combine(root, "out0.txt")), Is.False);
        }

        [Test]
        public void OutputRestoresTheStreamWhenItsContentThrows()
        {
            var (interp, _) = NewInterpreter();
            var original = interp.OutputStream;

            Assert.That(() => interp.Eval("<output filename='out.txt'>[<var:missing/>]</output>"), Throws.Exception);

            Assert.That(interp.OutputStream, Is.SameAs(original));

            // The file handle was released, so it can be deleted.
            Assert.That(() => File.Delete(Path.Combine(root, "out.txt")), Throws.Nothing);
        }

        // Handler registration

        [Test]
        public void DuplicateTableNameThrows()
        {
            var (interp, _) = NewInterpreter();
            interp.AddHandler(new Table(new TableData("dup", ["a,b", "1,2"]), interp));

            Assert.That(
                () => interp.AddHandler(new Table(new TableData("dup", ["a,b", "3,4"]), interp)),
                Throws.InstanceOf<ArgumentException>()
            );
        }
    }
}
