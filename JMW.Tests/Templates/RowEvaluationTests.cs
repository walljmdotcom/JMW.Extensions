using NUnit.Framework;
using JMW.Templates.Tags;
using System.IO;
using System.Text;

namespace JMW.Templates.Tests
{
    /// <summary>
    /// Table, join, and lookup bodies run once per row. These check that each row sees the state of that row, not
    /// whatever the first row left behind.
    /// </summary>
    [TestFixture]
    public class RowEvaluationTests
    {
        private const string Counter = "<var:n exp=\"x1 + 1\" store=\"true\" print=\"false\"/><var:k value=\"[var:n]\"/>,";
        private const string Declares = "<var name=\"n\" value=\"0\"/><var name=\"k\" value=\"\"/>";

        private static string Run(string template)
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("t", ["num,name", "10,a", "20,b", "30,c"]), i));
            i.AddHandler(new Table(new TableData("u", ["num,label", "10,p", "20,q", "30,r"]), i));
            i.Eval(template);
            return sb.ToString();
        }

        // A variable reference in an attribute is expanded with the value the variable has on that row.
        [TestCase(Declares + "<tab name=\"t\">" + Counter + "</tab>", "1,2,3,")]
        [TestCase(Declares + "<join left_table=\"t\" right_table=\"u\" left_key=\"num\" right_key=\"num\">" + Counter + "</join>", "1,2,3,")]
        [TestCase(Declares + "<lookup table=\"t\" key_exp=\"'all'\"><row key=\"all\">" + Counter + "</row></lookup>", "1,2,3,")]
        [TestCase(Declares + "<tab name=\"t\">" + Counter + "</tab><tab name=\"t\">" + Counter + "</tab>", "1,2,3,4,5,6,")]
        [TestCase(Declares + "<tab name=\"t\"><var:n exp=\"x1 + 1\" store=\"true\" print=\"false\"/><if column=\"num\" exp=\"x1 == '20' && '[var:n]' == '2'\">[<name/>]</if><if exp=\"'[var:n]' == '3'\"><name/></if></tab>", "[b]c")]
        [TestCase(Declares + "<tab name=\"t\"><var:n exp=\"x1 + 1\" store=\"true\" print=\"false\"/><num exp=\"x1 + '-[var:n]'\"/>,</tab>", "10-1,20-2,30-3,")]
        public void VariableReferencesFollowTheRow(string template, string expected)
        {
            Assert.That(Run(template), Is.EqualTo(expected));
        }

        // A <var> declared inside a repeated body is declared once and reassigned on each row.
        [TestCase("<tab name=\"t\"><var name=\"v\" value=\"x\"/><var:v/>,</tab>", "x,x,x,")]
        [TestCase("<tab name=\"t\"><var name=\"v\" value=\"[var:w]\"/><var:v/>,</tab><var name=\"w\" value=\"w\"/>", "w,w,w,")]
        [TestCase("<join left_table=\"t\" right_table=\"u\" left_key=\"num\" right_key=\"num\"><var name=\"v\" value=\"x\"/><var:v/>,</join>", "x,x,x,")]
        [TestCase("<lookup table=\"t\" key_exp=\"'all'\"><row key=\"all\"><var name=\"v\" value=\"x\"/><var:v/>,</row></lookup>", "x,x,x,")]
        [TestCase("<if exp=\"1 == 1\"><var name=\"v\" value=\"y\"/><var:v/></if>", "y")]
        [TestCase("<tab name=\"t\"><tab name=\"u\"><var name=\"v\" value=\"x\"/><var:v/></tab>,</tab>", "xxx,xxx,xxx,")]
        public void NestedDeclarationsRunOnEveryRow(string template, string expected)
        {
            Assert.That(Run(template), Is.EqualTo(expected));
        }

        [Test]
        public void EvalLeavesParsedAttributesAlone()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("t", ["num,name", "10,a", "20,b", "30,c"]), i));
            var tags = i.Parser.Parse(Declares + "<tab name=\"t\">" + Counter + "</tab>");

            i.Eval(tags);

            var counter = tags[2].Children[1];
            Assert.That(counter.Properties["value"], Is.EqualTo("[var:n]"));
        }

        // Declaring the same variable twice is still an error, wherever the declarations sit.
        [TestCase("<var name=\"v\" value=\"x\"/><var name=\"v\" value=\"y\"/>")]
        [TestCase("<var name=\"v\" value=\"x\"/><tab name=\"t\"><var name=\"v\" value=\"y\"/></tab>")]
        public void DuplicateDeclarationsThrow(string template)
        {
            Assert.That(() => Run(template), Throws.Exception.With.Message.EqualTo("Variable: 'v' already exists."));
        }
    }
}
