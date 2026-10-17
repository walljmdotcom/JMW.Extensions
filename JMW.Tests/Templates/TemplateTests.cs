using JMW.Extensions;
using JMW.Templates.Tags;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace JMW.Templates.Tests
{
    [TestFixture]
    public class InterpreterTests
    {
        [Test]
        public void TestTableVarIf1()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar,ip", "blah,flaw,1.1.1.1", "floo,blue,1.2.3.4"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<var name=""jason"" value=""flaw""/>
<table name=""test"">
<foo/>\n
<if column=""bar"" exp=""x1 !='[var:jason]'""><bar/>\n</if>
<if column=""ip"" oct=""4"" exp=""x1==1""><ip/>\n</if>
<var:walljm />\n
<var:walljm exp=""x1.substr(0,2)"" />\n
</table>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah\r\n1.1.1.1\r\nwooooo\r\nwo\r\nfloo\r\nblue\r\nwooooo\r\nwo\r\n"));
        }

        [Test]
        public void TestTableVarIf2()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");
            var dt = new DataTable("test");
            dt.Columns.Add(new DataColumn("foo"));
            dt.Columns.Add(new DataColumn("bar"));
            dt.Columns.Add(new DataColumn("ip"));
            dt.Rows.Add("blah", "flaw", "1.1.1.1");
            dt.Rows.Add("floo", "blue", "1.2.3.4");
            var data = new TableData(dt);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<var name=""jason"" value=""flaw""/>
<table name=""test"">
<foo/>\n
<if column=""bar, foo"" exp=""x1 !='[var:jason]' && x2 == 'floo'""><bar/>\n</if>
<if column=""ip"" oct=""4"" exp=""x1==1""><ip/>\n</if>
<var:walljm />\n
<var:walljm exp=""x1.substr(0,2)"" />\n
</table>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah\r\n1.1.1.1\r\nwooooo\r\nwo\r\nfloo\r\nblue\r\nwooooo\r\nwo\r\n"));
        }

        [Test]
        public void TestTableVarIf3()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar,ip", "blah,flaw,1.1.1.1", "floo,blue,1.2.3.4"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<if column=""ip,bar"" oct=""4"" exp=""x1==1""><ip/>\n</if>
</table>
"),
                Throws.Exception.With.Message.EqualTo("The 'oct' attribute is currently incompatible with multiple arguments for the 'if' tag."));
        }

        [Test]
        public void TestTableIf4()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar,ip", "blah,flaw,1.1.1.1", "floo,blue,1.2.3.4"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<if column=""ip"" type='eq'><ip/>\n</if>
</table>
"),
                Throws.Exception.With.Message.EqualTo("Missing attribute: 'value' Line: 4 Column: 1"));
        }

        [TestCase("<if exp=\"1 == 1\">y</if>", "yy")]
        [TestCase("<if exp=\"1 == 2\">y</if>", "")]
        [TestCase("<if column=\"foo\" exp=\"x1 == 'blah'\">y</if>", "y")]
        [TestCase("<if column=\"foo, bar\" exp=\"x1 == 'floo' && x2 == 'blue'\">y</if>", "y")]
        [TestCase("<transform exp=\"'z'\"/>", "zz")]
        [TestCase("<transform columns=\"foo\" exp=\"x1\"/>", "blahfloo")]
        [TestCase("<transform columns=\"foo, bar\" exp=\"x2\"/>", "flawblue")]
        public void TestTableColumnListIsOptional(string body, string expected)
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("test", ["foo,bar", "blah,flaw", "floo,blue"]), i));

            i.Eval("<table name=\"test\">" + body + "</table>");

            Assert.That(sb.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TestTableIf5()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar,ip", "blah,flaw,1.1.1.1", "floo,blue,1.2.3.4"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<if column=""ip"" value='foo'><ip/>\n</if>
</table>
"),
                Throws.Exception.With.Message.EqualTo("Missing attribute: 'type' Line: 4 Column: 1"));
        }

        [Test]
        public void TestTableIf6()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar,ip", "blah,flaw,1.1.1.1", "floo,blue,1.2.3.4"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<if column=""ip""><ip/>\n</if>
</table>
"),
                Throws.Exception.With.Message.EqualTo("Missing attribute.  You must have either a 'type' and a 'value' attribute or an 'exp' attribute. Line: 4 Column: 1"));
        }

        [Test]
        public void TestTableIf7()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("", ["foo,bar,ip", "blah,flaw,1.1.1.1", "floo,blue,1.2.3.4"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table>
<if column=""ip"" type='blah' value='foo'><ip/>\n</if>
</table>
"),
                Throws.Exception.With.Message.EqualTo("Invalid property value for 'type' property on 'if' tag. Allowed values: 'equals', 'notequals', 'contains', 'notcontains', 'startswith', 'notstartswith', 'endswith', 'notendswith', 'eq', 'neq', 'cnt', 'ncnt', 'strt', 'nstrt', 'ends', 'nends'. Line: 4 Column: 1"));
        }

        [Test]
        public void TestTableIf()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar,ip", "blah,flaw,1.1.1.1", "floo,blue,1.2.3.4"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            var output = RunTableIf(sb, i, "equals", "flaw");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = RunTableIf(sb, i, "eq", "flaw");
            Assert.That(output, Is.EqualTo("flaw\r\n"));

            output = RunTableIf(sb, i, "notequals", "blue");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = RunTableIf(sb, i, "neq", "blue");
            Assert.That(output, Is.EqualTo("flaw\r\n"));

            output = RunTableIf(sb, i, "strt", "fl");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = RunTableIf(sb, i, "startswith", "fl");
            Assert.That(output, Is.EqualTo("flaw\r\n"));

            output = RunTableIf(sb, i, "nstrt", "bl");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = RunTableIf(sb, i, "notstartswith", "bl");
            Assert.That(output, Is.EqualTo("flaw\r\n"));

            output = output = RunTableIf(sb, i, "ends", "aw");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = output = RunTableIf(sb, i, "endswith", "aw");
            Assert.That(output, Is.EqualTo("flaw\r\n"));

            output = output = RunTableIf(sb, i, "nends", "ue");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = output = RunTableIf(sb, i, "notendswith", "ue");
            Assert.That(output, Is.EqualTo("flaw\r\n"));

            output = output = RunTableIf(sb, i, "contains", "la");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = output = RunTableIf(sb, i, "cnt", "la");
            Assert.That(output, Is.EqualTo("flaw\r\n"));

            output = output = RunTableIf(sb, i, "notcontains", "lu");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
            output = output = RunTableIf(sb, i, "ncnt", "lu");
            Assert.That(output, Is.EqualTo("flaw\r\n"));
        }

        private static string RunTableIf(StringBuilder sb, Interpreter i, string type, string value)
        {
            sb.Clear();

            i.Eval(@"
<table name=""test"">
<if column=""bar"" value=""" + value + @""" type=""" + type + @"""><bar/>\n</if>
</table>
");

            return sb.ToString();
        }

        [Test]
        public void TestTableWhere()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"" where=""x1 != 'blah'"">
<foo/>\n
</table>
|||\n
<table name=""test"">
<foo/>\n
</table>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("floo\r\nfloo\r\n|||\r\nblah\r\nfloo\r\nblah\r\nfloo\r\n"));
        }

        [Test]
        public void TestTableDistinct()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"" distinct=""foo, bar"">
<foo/>\n
</table>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah\r\nfloo\r\n"));
        }

        [Test]
        public void TestTableOrderByAsc()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "ablah,flaw", "bfloo,blue", "cblah,flaw", "dfloo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"" orderby=""foo"">
<foo/>\n
</table>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("ablah\r\nbfloo\r\ncblah\r\ndfloo\r\n"));
        }

        [Test]
        public void TestTableOrderByDesc()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "ablah,flaw", "bfloo,blue", "cblah,flaw", "dfloo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"" orderby=""foo"" order=""desc"">
<foo/>\n
</table>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("dfloo\r\ncblah\r\nbfloo\r\nablah\r\n"));
        }

        [Test]
        public void TestAnonTable()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("", ["foo,bar", "blah,1.1.1.1", "floo,1.2.3.4", "blah,10.130.0.12", "floo,192.168.12.56"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<tab where=""x1 != 'blah'"">
<foo exp=""x1.substr(0,1)""/>\n
</tab>
|||\n
<table>
<bar oct=""3"" exp=""++x1""/>\n
</table>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("f\r\nf\r\n|||\r\n1.1.2.1\r\n1.2.4.4\r\n10.130.1.12\r\n192.168.13.56\r\n"));
        }

        [Test]
        public void TestTab()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,1.1.1.1", "floo,1.2.3.4", "blah,10.130.0.12", "floo,192.168.12.56"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<tab name=""test"" where=""x1 != 'blah'"">
<foo exp=""x1.substr(0,1)""/>\n
</tab>
|||\n
<tab name=""test"">
<bar oct=""3"" exp=""++x1""/>\n
</tab>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("f\r\nf\r\n|||\r\n1.1.2.1\r\n1.2.4.4\r\n10.130.1.12\r\n192.168.13.56\r\n"));
        }

        // each where filters only its own side
        [TestCase("", "greenyellow\r\nbluepurple\r\nredpink\r\nblackbrown\r\n")]
        [TestCase(@"left_where=""x2 == 'flaw'""", "greenyellow\r\nredpink\r\n")]
        [TestCase(@"right_where=""x1 != 'blue'""", "greenyellow\r\nredpink\r\nblackbrown\r\n")]
        [TestCase(@"left_where=""x2 == 'flaw'"" right_where=""x1 != 'red'""", "greenyellow\r\n")]
        public void JoinWhereFiltersItsOwnSide(string where, string expected)
        {
            var sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);
                i.AddHandler(new Table(new TableData("test1", ["foo,bar", "green,flaw", "blue,blue", "red,flaw", "black,blue"]), i));
                i.AddHandler(new Table(new TableData("test2", ["baz,bar", "green,yellow", "blue,purple", "red,pink", "black,brown"]), i));

                i.Eval(@"<join left_table=""test1"" right_table=""test2"" left_key=""foo"" right_key=""baz"" " + where + @">
<right_baz/><right_bar/>\n
</join>");
            }

            Assert.That(sb.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TestJoin()
        {
            var sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);
                i.AddVariable("walljm", "wooooo");

                var data1 = new TableData("test1", ["foo,bar", "green,flaw", "blue,blue", "red,flaw", "black,blue"]);
                var tbl1 = new Table(data1, i);

                i.AddHandler(tbl1);

                var data2 = new TableData("test2", ["baz,bar", "green,yellow", "blue,purple", "red,pink", "black,brown"]);
                var tbl2 = new Table(data2, i);
                i.AddHandler(tbl2);

                i.Eval(@"
<join left_table=""test1"" right_table=""test2"" left_key_exp=""x1"" right_key=""baz"">
<left_foo/><left_bar/>\n
</join>
|||\n
<join left_table=""test1"" right_table=""test2"" left_key_exp=""x1"" right_key=""baz"">
<right_baz/><right_bar/>\n
</join>
");

                var output = sb.ToString();
                Assert.That(output, Is.EqualTo("greenflaw\r\nblueblue\r\nredflaw\r\nblackblue\r\n|||\r\ngreenyellow\r\nbluepurple\r\nredpink\r\nblackbrown\r\n"));
            }

            sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);
                i.AddVariable("walljm", "wooooo");

                var data1 = new TableData("test1", ["foo,bar", "green,flaw", "blue,blue", "red,flaw", "black,blue"]);
                var tbl1 = new Table(data1, i);

                i.AddHandler(tbl1);

                var data2 = new TableData("test2", ["baz,bar", "green,yellow", "blue,purple", "red,pink", "black,brown"]);
                var tbl2 = new Table(data2, i);
                i.AddHandler(tbl2);

                i.Eval(@"
<join left_table=""test1"" right_table=""test2"" left_key=""foo"" right_key_exp=""x1"">
<left_foo/><left_bar/>\n
</join>
|||\n
<join left_table=""test1"" right_table=""test2"" left_key_exp=""x1"" right_key=""baz"">
<right_baz/><right_bar/>\n
</join>
");

                var output = sb.ToString();
                Assert.That(output, Is.EqualTo("greenflaw\r\nblueblue\r\nredflaw\r\nblackblue\r\n|||\r\ngreenyellow\r\nbluepurple\r\nredpink\r\nblackbrown\r\n"));
            }

            sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);
                i.AddVariable("walljm", "wooooo");

                var data1 = new TableData("test1", ["foo,bar", "green,1.1.1.1", "blue,1.1.1.1", "red,1.1.1.1", "black,1.1.1.1"]);
                var tbl1 = new Table(data1, i);

                i.AddHandler(tbl1);

                var data2 = new TableData("test2", ["baz,bar", "green,yellow", "blue,purple", "red,pink", "black,brown"]);
                var tbl2 = new Table(data2, i);
                i.AddHandler(tbl2);

                i.Eval(@"
<join left_table=""test1"" right_table=""test2"" left_key=""foo"" right_key_exp=""x1"">
<left_foo/><left_bar oct=""3"" exp=""++x1""/>\n
</join>
");

                var output = sb.ToString();
                Assert.That(output, Is.EqualTo("green1.1.2.1\r\nblue1.1.2.1\r\nred1.1.2.1\r\nblack1.1.2.1\r\n"));
            }
        }

        private const string JoinTables = "left_table=\"t\" right_table=\"u\" left_key=\"num\" right_key=\"num\"";

        [TestCase("<if column=\"left_name\" exp=\"x1 == 'b'\">B</if>,", ",B,,")]
        [TestCase("<if column=\"left_name, right_label\" exp=\"x1 == 'c' && x2 == 'r'\">C</if>,", ",,C,")]
        [TestCase("<if column=\"right_label\" type=\"eq\" value=\"p\">P</if>,", "P,P,,")]
        [TestCase("<transform columns=\"right_label\" exp=\"x1\"/>,", "p,p,r,")]
        [TestCase("<transform columns=\"left_num, right_label\" exp=\"x1 + x2\"/>,", "10p,20p,30r,")]
        public void TestJoinColumnLists(string body, string expected)
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("t", ["num,name", "10,a", "20,b", "30,c"]), i));
            i.AddHandler(new Table(new TableData("u", ["num,label", "10,p", "20,p", "30,r"]), i));

            i.Eval("<join " + JoinTables + ">" + body + "</join>");

            Assert.That(sb.ToString(), Is.EqualTo(expected));
        }

        // A second join over the same tables and keys starts from every joined row, whatever the first one filtered.
        [TestCase("where=\"x2 != 'b'\"", "ac|abc")]
        [TestCase("orderby=\"left_name\" order=\"desc\"", "cba|abc")]
        [TestCase("distinct=\"right_label\"", "ac|abc")]
        [TestCase("left_where=\"x1 != '20'\"", "ac|abc")]
        [TestCase("right_where=\"x1 != '20'\"", "ac|abc")]
        [TestCase("", "abc|abc")]
        public void TestJoinRepeated(string first_filter, string expected)
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("t", ["num,name", "10,a", "20,b", "30,c"]), i));
            i.AddHandler(new Table(new TableData("u", ["num,label", "10,p", "20,p", "30,r"]), i));

            i.Eval("<join " + JoinTables + " " + first_filter + "><left_name/></join>|<join " + JoinTables + "><left_name/></join>");

            Assert.That(sb.ToString(), Is.EqualTo(expected));
        }

        // Only the table's own columns belong to it. Every other tag in the body keeps its own handler.
        [TestCase("<tab name=\"t\"><name/><timestamp format=\"'T'\"/><num/>,</tab>", "aT10,bT20,cT30,")]
        [TestCase("<tab name=\"u\"><label/><lookup table=\"t\" key_col=\"num\"><row key=\"20\"><name/></row></lookup>,</tab>", "pb,pb,rb,")]
        [TestCase("<tab name=\"u\"><label/><join " + JoinTables + "><left_name/></join>,</tab>", "pabc,pabc,rabc,")]
        [TestCase("<tab name=\"t\" where=\"x1 == '10'\"><join " + JoinTables + ">[<left_name/>]</join></tab><join " + JoinTables + "><left_name/></join>", "[a]abc")]
        [TestCase("<join " + JoinTables + "><left_name/><timestamp format=\"'T'\"/><right_label/>,</join>", "aTp,bTp,cTr,")]
        [TestCase("<lookup table=\"t\" key_col=\"num\"><row key=\"20\"><name/><timestamp format=\"'T'\"/><num/></row></lookup>", "bT20")]
        public void TestTagsNestedInTable(string template, string expected)
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("t", ["num,name", "10,a", "20,b", "30,c"]), i));
            i.AddHandler(new Table(new TableData("u", ["num,label", "10,p", "20,p", "30,r"]), i));

            i.Eval(template);

            Assert.That(sb.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TestVersionNestedInTable()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("t", ["num,name", "10,a", "20,b"]), i));

            i.Eval("<tab name=\"t\"><name/><version prefix=\"V\"/><num/>,</tab>");

            Assert.That(sb.ToString(), Does.Match("^aV[0-9.]+10,bV[0-9.]+20,$"));
        }

        // Column names in orderby and distinct match whatever their case, as column tags do.
        [TestCase("<tab name=\"t\" orderby=\"Name\" order=\"desc\"><name/></tab>", "cba")]
        [TestCase("<tab name=\"t\" orderby=\"name\"><Name/></tab>", "abc")]
        [TestCase("<tab name=\"t\" distinct=\"Label\"><name/></tab>", "ab")]
        [TestCase("<join left_table=\"t\" right_table=\"u\" left_key=\"Num\" right_key=\"Num\" orderby=\"left_Name\" order=\"desc\"><left_name/></join>", "cba")]
        [TestCase("<join left_table=\"t\" right_table=\"u\" left_key=\"Num\" right_key=\"Num\" orderby=\"left_name\"><left_Name/></join>", "abc")]
        [TestCase("<join left_table=\"t\" right_table=\"u\" left_key=\"Num\" right_key=\"Num\" distinct=\"Right_Code\"><left_name/></join>", "ac")]
        public void TestMixedCaseColumnNames(string template, string expected)
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(new TableData("t", ["Num,Name,Label", "10,a,x", "20,b,y", "30,c,x"]), i));
            i.AddHandler(new Table(new TableData("u", ["Num,Code", "10,p", "20,p", "30,r"]), i));

            i.Eval(template);

            Assert.That(sb.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TestJoinError1()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data1 = new TableData("test1", ["foo,bar", "green,flaw", "blue,blue", "red,flaw", "black,blue"]);
            var tbl1 = new Table(data1, i);

            i.AddHandler(tbl1);

            var data2 = new TableData("test2", ["baz,bar", "green,yellow", "blue,purple", "red,pink", "black,brown"]);
            var tbl2 = new Table(data2, i);
            i.AddHandler(tbl2);

            Assert.That(
                () => i.Eval(@"
<join left_table=""test3"" right_table=""test2"" left_key_exp=""x1"" right_key=""baz"">
<left_foo/><left_bar/>\n
</join>
"),
                Throws.Exception.With.Message.EqualTo("Specified table: 'test3' in 'left_table' attribute does not exist."));

            Assert.That(
                () => i.Eval(@"
<join left_table=""test1"" right_table=""test3"" left_key_exp=""x1"" right_key=""baz"">
<left_foo/><left_bar/>\n
</join>
"),
                Throws.Exception.With.Message.EqualTo("Specified table: 'test3' in 'right_table' attribute does not exist."));
        }

        [Test]
        public void TestJoinError2()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data1 = new TableData("test1", ["foo,bar", "foo,flaw", "foo,blue", "red,flaw", "black,blue"]);
            var tbl1 = new Table(data1, i);

            i.AddHandler(tbl1);

            var data2 = new TableData("test2", ["baz,bar", "green,yellow", "blue,purple", "red,pink", "black,brown"]);
            var tbl2 = new Table(data2, i);
            i.AddHandler(tbl2);

            Assert.That(
                () => i.Eval(@"
<join left_table=""test1"" right_table=""test2"" left_key_exp=""x1"" right_key=""baz"">
<left_foo/><left_bar/>\n
</join>
"),
                Throws.Exception.With.Message.EqualTo("Key expression does not produce a unique key.  Please provide a key expression that produces a unique key.  Duplicate key: 'foo'"));

            var sb2 = new StringBuilder();
            using var wr2 = new StringWriter(sb2);
            var i2 = new Interpreter(wr2);
            i2.AddVariable("walljm", "wooooo");

            var uniqueData1 = new TableData("test1", ["foo,bar", "green,flaw", "blue,blue", "red,flaw", "black,blue"]);
            i2.AddHandler(new Table(uniqueData1, i2));

            var uniqueData2 = new TableData("test2", ["baz,bar", "green,yellow", "blue,purple", "red,pink", "black,brown"]);
            i2.AddHandler(new Table(uniqueData2, i2));

            Assert.That(
                () => i2.Eval(@"
<join left_table=""test1"" right_table=""test2"" right_key=""baz"">
<left_foo/><left_bar/>\n
</join>
"),
                Throws.Exception.With.Message.EqualTo("Require attribute is missing. A 'left_key_exp' or a 'left_key' attribute is required."));

            Assert.That(
                () => i2.Eval(@"
<join left_table=""test1"" right_table=""test2"">
<left_foo/><left_bar/>\n
</join>
"),
                Throws.Exception.With.Message.EqualTo("Require attribute is missing. A 'left_key_exp' or a 'left_key' attribute is required."));
        }

        [Test]
        public void TestTableOutput()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            "foo.txt".IfExists(File.Delete);

            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"">
<output filename=""foo.txt""><foo/>\n</output>
</table>
");
            var output = File.ReadAllText("foo.txt");
            Assert.That(output, Is.EqualTo("blah\r\nfloo\r\nblah\r\nfloo\r\n"));
            "foo.txt".IfExists(File.Delete);
        }

        [Test]
        public void TestTableOutputError1()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            "foo.txt".IfExists(File.Delete);

            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<output filename=""foo.txt"" mode='blah'><foo/>\n</output>
</table>
"),
                Throws.Exception.With.Message.EqualTo("Unknown attribute file for 'mode'.  Allowed values are: 'new' and 'append'"));
        }

        [Test]
        public void TestTableOutputError2()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            "foo.txt".IfExists(File.Delete);

            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<output mode='blah'><foo/>\n</output>
</table>
"),
                Throws.Exception.With.Message.EqualTo("The 'filename' attribute is required."));
        }

        [Test]
        public void TestTableOutput_withNew()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            "foo.txt".IfExists(File.Delete);

            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"">
<output filename=""foo.txt"" mode=""new""><foo/>\n</output>
</table>
");
            var output = File.ReadAllText("foo0.txt");
            Assert.That(output, Is.EqualTo("blah\r\n"));
            "foo0.txt".IfExists(File.Delete);

            output = File.ReadAllText("foo1.txt");
            Assert.That(output, Is.EqualTo("floo\r\n"));
            "foo1.txt".IfExists(File.Delete);

            output = File.ReadAllText("foo2.txt");
            Assert.That(output, Is.EqualTo("blah\r\n"));
            "foo2.txt".IfExists(File.Delete);

            output = File.ReadAllText("foo3.txt");
            Assert.That(output, Is.EqualTo("floo\r\n"));
            "foo3.txt".IfExists(File.Delete);
        }

        [Test]
        public void TestNamedIncludes()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<include define=""one"">
<foo/> | <bar/>\n
</include>

<table name=""test"">
<include name=""one""/>
</table>
");
            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah | flaw\r\nfloo | blue\r\nblah | flaw\r\nfloo | blue\r\n"));
        }

        [Test]
        public void TestNamedIncludesError1()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<include name='foo'/>
</table>
"),
                Throws.Exception.With.Message.EqualTo("The <include> tag referenced named include 'foo' which does not exist in the template."));
        }

        [Test]
        public void TestNamedIncludesError2()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<include/>
</table>
"),
                Throws.Exception.With.Message.EqualTo("The <include> tag requires either a 'name' attribute or a 'path' attribute."));
        }

        [Test]
        public void TestPathIncludes()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"">
<include path=""" + Path.Combine(".", "Templates", "testpathinclude.tmpl") + @"""/>
</table>
");
            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah | flaw\r\nfloo | blue\r\nblah | flaw\r\nfloo | blue\r\n"));
        }

        [Test]
        public void TestPathIncludesError()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            Assert.That(
                () => i.Eval(@"
<table name=""test"">
<include path=""" + Path.Combine(".", "Templates", "asdf.tmpl") + @"""/>
</table>
"),
                Throws.Exception.With.Message.Contains("The <include> tag refers to a template which does not exist"));
        }

        [Test]
        public void TestLookup1()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data1 = new TableData("test1", ["foo,bar", "green,flaw", "blue,blue", "red,flaw", "black,blue"]);
            var tbl1 = new Table(data1, i);

            i.AddHandler(tbl1);

            i.Eval(@"
<lookup table=""test1"" key_exp=""x1"">
<row key=""blue"">
<foo/><bar/>\n
</row>
<row key=""black"">
<foo/><bar/>\n
</row>
</lookup>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blueblue\r\nblackblue\r\n"));
        }

        [Test]
        public void TestLookup2()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data1 = new TableData("test1", ["foo,bar,ip", "green,flaw,1.1.1.1", "blue,blue,1.1.1.1", "red,flaw,1.1.1.1", "black,blue,1.1.1.1"]);
            var tbl1 = new Table(data1, i);

            i.AddHandler(tbl1);

            i.Eval(@"
<lookup table=""test1"" key_col=""bar"">
<row key=""blue"">
<foo/><bar/><ip oct=""3"" exp=""++x1""/>\n
</row>
</lookup>
");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blueblue1.1.2.1\r\nblackblue1.1.2.1\r\n"));
        }

        [TestCase("key_col=\"bar\"", "key_col=\"bar\"", "blueblue\r\nblackblue\r\n", "blueblue\r\nblackblue\r\n")]
        [TestCase("key_exp=\"x1\"", "key_exp=\"x1\"", "blueblue\r\n", "blueblue\r\n")]
        [TestCase("key_exp=\"x1\"", "key_col=\"bar\"", "blueblue\r\n", "blueblue\r\nblackblue\r\n")]
        [TestCase("key_col=\"bar\"", "key_exp=\"x1\"", "blueblue\r\nblackblue\r\n", "blueblue\r\n")]
        [TestCase("key_col=\"bar\"", "key_col=\"foo\"", "blueblue\r\nblackblue\r\n", "blueblue\r\n")]
        public void TestLookupRepeated(string first_key, string second_key, string first_output, string second_output)
        {
            var data1 = new TableData("test1", ["foo,bar", "green,flaw", "blue,blue", "red,flaw", "black,blue"]);
            const string row = "<row key=\"blue\"><foo/><bar/>\\n</row>";
            var template = "<lookup table=\"test1\" " + first_key + ">" + row + "</lookup>"
                + "<lookup table=\"test1\" " + second_key + ">" + row + "</lookup>";

            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddHandler(new Table(data1, i));

            i.Eval(template);
            Assert.That(sb.ToString(), Is.EqualTo(first_output + second_output));

            // A second Eval reuses the lookup handler built by the first.
            sb.Clear();
            i.Eval(template);
            Assert.That(sb.ToString(), Is.EqualTo(first_output + second_output));
        }

        [Test]
        public void TestLookupError1()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data1 = new TableData("test1", ["foo,bar,ip", "green,flaw,1.1.1.1", "blue,blue,1.1.1.1", "red,flaw,1.1.1.1", "black,blue,1.1.1.1"]);
            var tbl1 = new Table(data1, i);

            i.AddHandler(tbl1);

            Assert.That(
                () => i.Eval(@"
<lookup table=""test2"" key_col=""bar"">
<row key=""blue"">
<foo/><bar/><ip oct=""3"" exp=""++x1""/>\n
</row>
</lookup>
"),
                Throws.Exception.With.Message.EqualTo("Specified table: 'test2' in 'table' attribute does not exist."));
        }

        [Test]
        public void TestLookupError2()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data1 = new TableData("test1", ["foo,bar,ip", "green,flaw,1.1.1.1", "blue,blue,1.1.1.1", "red,flaw,1.1.1.1", "black,blue,1.1.1.1"]);
            var tbl1 = new Table(data1, i);

            i.AddHandler(tbl1);

            Assert.That(
                () => i.Eval(@"
<lookup table=""test1"">
<row key=""blue"">
<foo/><bar/><ip oct=""3"" exp=""++x1""/>\n
</row>
</lookup>
"),
                Throws.Exception.With.Message.EqualTo("Require attribute is missing. A 'key_exp' or 'key_col' attribute is required."));
        }

        [Test]
        public void TestLookupRowError1()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data1 = new TableData("test1", ["foo,bar,ip", "green,flaw,1.1.1.1", "blue,blue,1.1.1.1", "red,flaw,1.1.1.1", "black,blue,1.1.1.1"]);
            var tbl1 = new Table(data1, i);

            i.AddHandler(tbl1);

            Assert.That(
                () => i.Eval(@"
<lookup table=""test1"" key_col='foo'>
<row>
<foo/><bar/><ip oct=""3"" exp=""++x1""/>\n
</row>
</lookup>
"),
                Throws.Exception.With.Message.EqualTo("The 'key' attribute is required."));
        }

        [Test]
        public void TestVersion()
        {
            var sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);

                i.Eval(@"
version: <version/>
");

                var output = sb.ToString();
                Assert.That(output.Contains("version: Templating Engine Version ", StringComparison.Ordinal), Is.True);
            }
            sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);

                i.Eval(@"
version: <version prefix=""TE: "" />
");

                var output = sb.ToString();
                Assert.That(output.Contains("version: TE: ", StringComparison.Ordinal), Is.True);
            }

            sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);

                i.Eval(@"
version: <version prefix=""TE: "" exp=""x1.substr(0, 7)""/>
");

                var output = sb.ToString();
                Assert.That(output.Contains("version: TE: ", StringComparison.Ordinal), Is.True);
            }
        }

        [Test]
        public void TestTimestamp()
        {
            var sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);

                i.Eval(@"
datetime: <timestamp/>
");

                var output = sb.ToString();
                Assert.That(output.StartsWith("datetime: " + DateTime.Now.ToString("M/d/yyyy hh:mm", CultureInfo.InvariantCulture), StringComparison.Ordinal), Is.True);
            }

            sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);

                i.Eval(@"
datetime: <timestamp format=""M-d-yy""/>
");

                var output = sb.ToString();
                Assert.That(output, Is.EqualTo("datetime: " + DateTime.Now.ToString("M-d-yy", CultureInfo.InvariantCulture)));
            }

            sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);

                i.Eval(@"
datetime: <timestamp format=""M-d-yy hh:mm:ss"" exp=""x1.substr(0,x1.indexOf(' '));""/>
");

                var output = sb.ToString();
                Assert.That(output, Is.EqualTo("datetime: " + DateTime.Now.ToString("M-d-yy", CultureInfo.InvariantCulture)));
            }
        }

        [Test]
        public void TestTimestampError()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);

            Assert.That(
                () => i.Eval(@"datetime: <timestamp format=""123!@*(&^$@(^%"" />"),
                Throws.Exception.With.Message.EqualTo("Unable to interpret tag 'timestamp' with format: '123!@*(&^$@(^%'. Error Message: Input string was not in a correct format."));
        }

        [Test]
        public void TestDefaultHandler()
        {
            var sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);

                i.Eval(@"<jason/>");

                var output = sb.ToString();
                Assert.That(output, Is.EqualTo(string.Empty));
            }

            sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr)
                {
                    DefaultHandler = new Functional.Optional<Handler>((t, h) => { h.OutputStream.Write("jason"); })
                };

                i.Eval(@"<jason/>");

                var output = sb.ToString();
                Assert.That(output, Is.EqualTo("jason"));
            }
        }

        [Test]
        public void TestAdditionalHandler()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(new Parser(), wr, new List<ITagHandler> { new TestHandler() });

            i.Eval(@"<test>jason</test>");

            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("JASON"));
        }

        [Test]
        public void TestVar1()
        {
            var sb = new StringBuilder();
            using (var wr = new StringWriter(sb))
            {
                var i = new Interpreter(wr);
                i.AddVariable("IP", "1.1.1.1");

                Assert.That(
                    () => i.Eval(@"<var:IP oct='2' exp='x+5' store='atrue' print='false'/>"),
                    Throws.Exception.With.Message.EqualTo("The 'store' attribute value 'atrue' was invalid. Value must be either 'true' or 'false'."));

                Assert.That(
                    () => i.Eval(@"<var:IP oct='2' exp='x+5' store='true' print='afalse'/>"),
                    Throws.Exception.With.Message.EqualTo("The 'print' attribute value 'afalse' was invalid. Value must be either 'true' or 'false'."));

                Assert.That(
                    () => i.Eval(@"<var />"),
                    Throws.Exception.With.Message.EqualTo("'var' tag requires both 'name' and 'value' attributes."));
            }

            var sb1 = new StringBuilder();
            using (var wr = new StringWriter(sb1))
            {
                var i = new Interpreter(wr);
                i.AddVariable("IP", "1.1.1.1");

                i.Eval(@"<var:ip exp='x1+10' oct='4' />");
                Assert.That(sb1.ToString(), Is.EqualTo("1.1.1.11"));
            }

            sb1 = new StringBuilder();
            using (var wr = new StringWriter(sb1))
            {
                var i = new Interpreter(wr);
                i.AddVariable("IP", "1.1.1.1");

                i.Eval(@"<var:ip store='true' value='2.2.2.2'/>");
                Assert.That(sb1.ToString(), Is.EqualTo("2.2.2.2"));
            }
        }

        [Test]
        public void TestVar2()
        {
            var v = new Variable("var");
            v.AddVariable("jason", "foo");

            Assert.That(
                () => v.AddVariable("jason", "foo"),
                Throws.Exception.With.Message.EqualTo("Variable: 'jason' already exists."));
        }

        [Test]
        public void TestTransform1()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"">
<transform columns='foo,bar' exp='x1+""|""+x2' />\n
</table>
");
            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah|flaw\r\nfloo|blue\r\nblah|flaw\r\nfloo|blue\r\n"));
        }

        [Test]
        public void TestTransform2()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"">
<trans columns='foo,bar' exp='x1+""|""+x2' />\n
</table>
");
            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah|flaw\r\nfloo|blue\r\nblah|flaw\r\nfloo|blue\r\n"));
        }

        [Test]
        public void TestTransform3()
        {
            var sb = new StringBuilder();
            using var wr = new StringWriter(sb);
            var i = new Interpreter(wr);
            i.AddVariable("walljm", "wooooo");

            var data = new TableData("test", ["foo,bar", "blah,flaw", "floo,blue", "blah,flaw", "floo,blue"]);
            var tbl = new Table(data, i);
            i.AddHandler(tbl);

            i.Eval(@"
<table name=""test"">
<tr columns='foo,bar' exp='x1+""|""+x2' />\n
</table>
");
            var output = sb.ToString();
            Assert.That(output, Is.EqualTo("blah|flaw\r\nfloo|blue\r\nblah|flaw\r\nfloo|blue\r\n"));
        }
    }

    public class TestHandler : TagHandlerBase
    {
        public override string TagName { get; } = "test";
        public override HashSet<string> ALLOWEDPROPS { get; } = [];

        public override void Handler(Tag token, Interpreter interp)
        {
            interp.OutputStream.Write(token.Children.First().TokenText.ToUpperInvariant());
        }
    }
}