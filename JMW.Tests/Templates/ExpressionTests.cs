using JMW.Templates.Expressions;
using NUnit.Framework;
using System;
using System.Linq;

namespace JMW.Templates.Tests
{
    [TestFixture]
    public class ExpressionTests
    {
        private static string Eval(string expression, params string[] arguments)
        {
            return ScriptEvaluator.Evaluate(expression, arguments).ToString();
        }

        // Expressions taken from the template documentation and the template tests.
        [TestCase("x1 = x1 + 3;  x1 = x1+5", new[] { "2" }, "10")]
        [TestCase("x1 == 1 || x2 == 3 || x5 == 10", new[] { "0", "3", "0", "0", "0" }, "true")]
        [TestCase("x1 == 1 || x2 == 3 || x5 == 10", new[] { "0", "0", "0", "0", "0" }, "false")]
        [TestCase("x1 == 1 && x2 == 2", new[] { "1", "2" }, "true")]
        [TestCase("x3 > 1 && x3 < 9", new[] { "a", "b", "5" }, "true")]
        [TestCase("x3 > 1 && x3 < 9", new[] { "a", "b", "9" }, "false")]
        [TestCase("x3+x4+'0'", new[] { "a", "b", "1", "2" }, "30")]
        [TestCase("x1+\"|\"+x2", new[] { "a", "b" }, "a|b")]
        [TestCase("x1+10", new[] { "5" }, "15")]
        [TestCase("++x1", new[] { "1" }, "2")]
        [TestCase("x1++", new[] { "1" }, "1")]
        [TestCase("x1++; x1", new[] { "1" }, "2")]
        [TestCase("--x1", new[] { "1" }, "0")]
        [TestCase("x1.substr(0,6)", new[] { "1.2.3.4567" }, "1.2.3.")]
        [TestCase("x1.substr(0,x1.indexOf(' '));", new[] { "hello world" }, "hello")]
        [TestCase("x1 !='foo' && x2 == 'floo'", new[] { "bar", "floo" }, "true")]
        [TestCase("x1 != 'blah'", new[] { "blah" }, "false")]
        [TestCase("x1==1", new[] { "1" }, "true")]
        [TestCase("x1", new[] { "jason" }, "jason")]
        public void DocumentedExpressions(string expression, string[] arguments, string expected)
        {
            Assert.That(Eval(expression, arguments), Is.EqualTo(expected));
        }

        // Evaluating one expression text many times, as a table does once per row, sees only that row's arguments.
        [TestCase("x1 = x1 + 3;  x1 = x1+5")]
        [TestCase("y = x1; y++; y")]
        [TestCase("x1++; x1")]
        [TestCase("x1 + '|' + x2")]
        public void RepeatedEvaluationKeepsNoStateBetweenCalls(string expression)
        {
            for (var row = 0; row < 3; row++)
            {
                var arguments = new[] { row.ToString(System.Globalization.CultureInfo.InvariantCulture), "b" };
                var first = Eval(expression, arguments);
                Assert.That(Eval(expression, arguments), Is.EqualTo(first), "row " + row);
                Assert.That(Eval(expression, ["9", "b"]), Is.Not.EqualTo(first), "row " + row);
            }
        }

        [TestCase("x1 +")]
        [TestCase("x3")]
        public void InvalidExpressionFailsEveryTime(string expression)
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.That(() => Eval(expression, "1"), Throws.Exception);
            }
        }

        // Arguments bind by number, not by order of appearance, and only identifiers are variables.
        [TestCase("x2 + '-' + x1", new[] { "A", "B" }, "B-A")]
        [TestCase("x3 + x1", new[] { "a", "b", "c" }, "ca")]
        [TestCase("X1", new[] { "upper" }, "upper")]
        [TestCase("'x1'", new string[0], "x1")]
        [TestCase("\"x1 and x2\"", new string[0], "x1 and x2")]
        [TestCase("x1", new[] { "007" }, "7")]
        [TestCase("x1", new[] { "1,000" }, "1,000")]
        [TestCase("x1", new[] { "1.1.1.1" }, "1.1.1.1")]
        [TestCase("x1 + 1", new[] { " 5 " }, "6")]
        [TestCase("x1", new[] { "" }, "")]
        public void ArgumentBinding(string expression, string[] arguments, string expected)
        {
            Assert.That(Eval(expression, arguments), Is.EqualTo(expected));
        }

        [TestCase("x2", new[] { "a" })]
        [TestCase("x1.substr(0,2); x2", new[] { "jason" })]
        [TestCase("x1", new string[0])]
        public void MissingArgumentThrows(string expression, string[] arguments)
        {
            Assert.Throws<ArgumentException>(() => Eval(expression, arguments));
        }

        [TestCase("1/0", "Infinity")]
        [TestCase("-1/0", "-Infinity")]
        [TestCase("0/0", "NaN")]
        [TestCase("0.1+0.2", "0.30000000000000004")]
        [TestCase("1e21", "1e+21")]
        [TestCase("1e20", "100000000000000000000")]
        [TestCase("123456789012345680000", "123456789012345680000")]
        [TestCase("0.000001", "0.000001")]
        [TestCase("0.0000001", "1e-7")]
        [TestCase("1.5e-10", "1.5e-10")]
        [TestCase("-1.5", "-1.5")]
        [TestCase("100", "100")]
        [TestCase(".5", "0.5")]
        [TestCase("-0", "0")]
        [TestCase("7 % 3", "1")]
        [TestCase("-7 % 3", "-1")]
        [TestCase("2 * 3 + 4", "10")]
        [TestCase("2 * (3 + 4)", "14")]
        [TestCase("10 - 4 - 3", "3")]
        [TestCase("24 / 4 / 2", "3")]
        [TestCase("'5' * '2'", "10")]
        [TestCase("'5' - 2", "3")]
        [TestCase("'5' + 2", "52")]
        [TestCase("1 + 2 + '3'", "33")]
        [TestCase("'a' * 2", "NaN")]
        [TestCase("true + 1", "2")]
        [TestCase("(3.14159).toFixed(2)", "3.14")]
        [TestCase("(42).toString()", "42")]
        public void Numbers(string expression, string expected)
        {
            Assert.That(Eval(expression), Is.EqualTo(expected));
        }

        [TestCase("1 == '1'", "true")]
        [TestCase("1 === '1'", "false")]
        [TestCase("1 !== '1'", "true")]
        [TestCase("'' == 0", "true")]
        [TestCase("null == undefined", "true")]
        [TestCase("null === undefined", "false")]
        [TestCase("null == 0", "false")]
        [TestCase("true == 1", "true")]
        [TestCase("NaN == NaN", "false")]
        [TestCase("'b' > 'a'", "true")]
        [TestCase("'10' < '9'", "true")]
        [TestCase("'10' < 9", "false")]
        [TestCase("10 <= 10", "true")]
        [TestCase("'a' < 1", "false")]
        [TestCase("'a' >= 1", "false")]
        [TestCase("!0", "true")]
        [TestCase("!''", "true")]
        [TestCase("!'a'", "false")]
        [TestCase("true && 'a'", "a")]
        [TestCase("0 || 'b'", "b")]
        [TestCase("'' && 'b'", "")]
        [TestCase("1 ? 'y' : 'n'", "y")]
        [TestCase("0 ? 'y' : 'n'", "n")]
        [TestCase("1 || 2 && 0", "1")]
        [TestCase("1 == 1 && 2 > 1", "true")]
        public void ComparisonAndLogic(string expression, string expected)
        {
            Assert.That(Eval(expression), Is.EqualTo(expected));
        }

        [TestCase("'abc'.length", "3")]
        [TestCase("'Hello'.toUpperCase()", "HELLO")]
        [TestCase("'Hello'.toLowerCase()", "hello")]
        [TestCase("'  pad  '.trim()", "pad")]
        [TestCase("'abcdef'.substr(2)", "cdef")]
        [TestCase("'abcdef'.substr(-2)", "ef")]
        [TestCase("'abcdef'.substr(1, 100)", "bcdef")]
        [TestCase("'abcdef'.substr(10, 2)", "")]
        [TestCase("'abcdef'.substring(2, 0)", "ab")]
        [TestCase("'abcdef'.substring(-5, 2)", "ab")]
        [TestCase("'abcdef'.slice(-2)", "ef")]
        [TestCase("'abcdef'.slice(1, -1)", "bcde")]
        [TestCase("'abcabc'.indexOf('c')", "2")]
        [TestCase("'abcabc'.indexOf('c', 3)", "5")]
        [TestCase("'abcabc'.indexOf('z')", "-1")]
        [TestCase("'abcabc'.lastIndexOf('a')", "3")]
        [TestCase("'abcabc'.lastIndexOf('a', 2)", "0")]
        [TestCase("'abc'.charAt(1)", "b")]
        [TestCase("'abc'.charAt(5)", "")]
        [TestCase("'a-b-c'.replace('-', '+')", "a+b-c")]
        [TestCase("'abc'.includes('bc')", "true")]
        [TestCase("'abc'.startsWith('ab')", "true")]
        [TestCase("'abc'.endsWith('ab')", "false")]
        [TestCase("'5'.padStart(3, '0')", "005")]
        [TestCase("'5'.padEnd(3)", "5  ")]
        [TestCase("'it\\'s'", "it's")]
        [TestCase("\"say \\\"hi\\\"\"", "say \"hi\"")]
        [TestCase("'a\\tb'", "a\tb")]
        [TestCase("'\\u0041'", "A")]
        public void Strings(string expression, string expected)
        {
            Assert.That(Eval(expression), Is.EqualTo(expected));
        }

        [TestCase("", "undefined")]
        [TestCase(";;", "undefined")]
        [TestCase("1;", "1")]
        [TestCase("1; 2; 3", "3")]
        [TestCase("y = 5; y * 2", "10")]
        [TestCase("y = 5; y += 2; y", "7")]
        [TestCase("y = 'a'; y += 'b'", "ab")]
        [TestCase("a = b = 3; a + b", "6")]
        public void StatementsAndAssignment(string expression, string expected)
        {
            Assert.That(Eval(expression), Is.EqualTo(expected));
        }

        [TestCase("x1 +")]
        [TestCase("'unterminated")]
        [TestCase("\"unterminated")]
        [TestCase("'trailing backslash\\")]
        [TestCase("x1 = ")]
        [TestCase("(1")]
        [TestCase("1)")]
        [TestCase("1 2")]
        [TestCase("foo")]
        [TestCase("5 = 3")]
        [TestCase("++5")]
        [TestCase("x1.bogus()")]
        [TestCase("x1.")]
        [TestCase("@")]
        [TestCase("1e")]
        [TestCase("12abc")]
        [TestCase("'\\uZZZZ'")]
        [TestCase("null.length")]
        [TestCase("true ? 1")]
        [TestCase("while (true) {}")]
        [TestCase("function f() { return 1; }")]
        public void MalformedExpressionsThrowExpressionException(string expression)
        {
            Assert.Throws<ExpressionException>(() => Eval(expression, "1"));
        }

        [TestCase("(", ")", 1000)]
        [TestCase("!", "", 10000)]
        [TestCase("-", "", 10000)]
        [TestCase("++", "", 10000)]
        [TestCase("'a'.substr(", ")", 1000)]
        public void DeepNestingThrowsInsteadOfOverflowingTheStack(string open, string close, int depth)
        {
            string expression = string.Concat(Enumerable.Repeat(open, depth)) + "1" + string.Concat(Enumerable.Repeat(close, depth));
            Assert.Throws<ExpressionException>(() => Eval(expression));
        }

        [Test]
        public void LongFlatExpressionsDoNotCountAsNesting()
        {
            string expression = string.Join(" + ", Enumerable.Repeat("1", 5000));
            Assert.That(Eval(expression), Is.EqualTo("5000"));
        }

        [Test]
        public void AssignmentsDoNotLeakBetweenEvaluations()
        {
            Assert.That(Eval("x1 = x1 + 1", "1"), Is.EqualTo("2"));
            Assert.That(Eval("x1", "1"), Is.EqualTo("1"));
            Assert.Throws<ExpressionException>(() => Eval("y = 1; z"));
        }
    }
}
