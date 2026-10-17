using JMW.Collections;
using JMW.Extensions.String;
using JMW.Parsing.Compile;
using JMW.Parsing.Expressions;
using JMW.Parsing.Extractors;
using JMW.Parsing.Handlers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Base = JMW.Parsing.Expressions.Base;
using Contains = JMW.Parsing.Expressions.Contains;

namespace JMW.Parsing.Tests;

[TestFixture]
public class ParsingTest
{
    [Test]
    public void ParseParagraphTest()
    {
        var json = @"
            para {
                include: section {
                    start: startswith {s:[""foo""]}
                    stop: startswith {s:[""bar""]}
                }
                start: contains {s:[""start""]i} #this is a comment
                stop: contains {s:[""stop""]i} #this is a comment
                props:[
                    prop {
                        name:""jason""
                        line: contains {s:[""is""]i} #this is a comment
                        parsers:[
                            after {
                                s:[""is""]
                            }
                            to {
                                s:[""line""]
                            }

                        ]
                    }
                    prop {
                        name:""wall""
                        line: contains {s:[""an"" ""more""]i} #this is a comment
                        split:""foo""
                        parsers:[
                            to {
                                s:[""is"", ""more""]w
                            }
                        ]
                    }
                    prop {
                        name:""walljm""
                        line: or {
                                    contains {s:[""an"" ""more""]i}
                                    contains {s: [""an""]i}
                                 } #this is a comment
                        split:""foo""
                        parsers:[
                            to {
                                s:[""is"", ""more""]w
                            }
                        ]
                    }
                ]
            }

";
        var text = @"
blah
blah
foo
start
 this is 0 a line
 this is 0 another line
 stop
 this is 0 a line
start
 this is 1 foo
 this is 1 more foo
 stop
 this is 0 a line
start
 this is 2 foo
 this is 2 more foo
 stop
 this is 0 a line
start
 this is 3 foo
 this is 3 more foo
 stop
 this is 0 a line
start
 this is 4 foo
 this is 4 more foo
 stop
 this is 0 a line
start
 this is 5 foo
 this is 5 more foo
 stop
 this is 0 a line
bar
start
 this is 6 foo
 this is 6 more foo
";
        var i = Compiler.Compile(json);
        var output = string.Empty;

        foreach (var o in i.ParseNamed(text))
        {
            output += o["jason"] + "|" + o["wall"] + ";";
        }

        Assert.That("is is 0 a |start\n this;is is 1 foo|\n this;is is 2 foo|\n this;is is 3 foo|\n this;is is 4 foo|\n this;is is 5 foo|\n this;", Is.EqualTo(output));
        var items = ((Paragraph)i).Parse((StreamReader?)null).ToList();

        Assert.That(0, Is.EqualTo(items.Count));

        output = string.Empty;

        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
        using (var sr = new StreamReader(ms))
        {
            foreach (var o in (IEnumerable<object[]>)((IProperty)i).Parse(sr))
            {
                output += o[2] + "|" + o[1] + ";";
            }
        }
        Assert.That("is is 0 a |start\n this;is is 1 foo|\n this;is is 2 foo|\n this;is is 3 foo|\n this;is is 4 foo|\n this;is is 5 foo|\n this;", Is.EqualTo(output));
    }

    [Test]
    public void ParseParagraphTest2()
    {
        var json = @"
            para {
                include: section {
                    start: startswith {s:[""foo""]}
                    stop: startswith {s:[""bar""]}
                }
                start: contains {s:[""start""]i} #this is a comment
                stop: contains {s:[""stop""]i} #this is a comment
            }

";
        try
        {
            Compiler.Compile(json);
            Assert.Fail();
        }
        catch (ParseException)
        {
        }

        json = @"
            para {
                include: section {
                    start: startswith {s:[""foo""]}
                    stop: startswith {s:[""bar""]}
                }
                stop: contains {s:[""stop""]i} #this is a comment
                props:[]
            }

";
        try
        {
            Compiler.Compile(json);
            Assert.Fail();
        }
        catch (ParseException)
        {
        }
    }

    [Test]
    public void ParseTableTest1()
    {
        var json = @"
            table {
                row: contains {s:["" ""]i} #this is a comment
                props:[
                    prop {
                        name: ""C1""
                        parsers:[
                            col { i:""1"" }
                        ]
                    }
                ]
            }
";
        var text = @"
Jason Wall Col1 Col2
this  is   a    row
free  bird is   awesome
";
        var i = Compiler.Compile(json);
        var output = string.Empty;

        foreach (var o in i.Parse(text))
        {
            output += o[0] + ";";
        }

        Assert.That("Wall;is;bird;", Is.EqualTo(output));
    }

    [Test]
    public void ParseTableTest2()
    {
        var json = @"
            table {
                row: contains {s:["" ""]i} #this is a comment
                props:[
                    prop {
                        name: ""C1""
                        parsers:[
                            col { i:""1"" }
                        ]
                    }
                ]
            }
";
        var text = @"
Jason Wall Col1 Col2
this  is   a    row
free  bird is   awesome
";
        var i = Compiler.Compile(json);
        var output = string.Empty;

        foreach (var o in i.ParseNamed(text))
        {
            output += o["C1"] + ";";
        }

        Assert.That("Wall;is;bird;", Is.EqualTo(output));
    }

    [Test]
    public void ParseTableTest3()
    {
        var json = @"
            table {
                header: contains{s:[""col1""]i}
                row: contains {s:[""is""]i} #this is a comment
                props:[
                    prop {
                        name: ""C1""
                        parsers:[
                            col { i:""1"" }
                        ]
                    }
                ],
                validate:'true'
            }
";
        var text = @"
Jason  Wall Col1 Col2
this   is   a    row
free  bird  is   awesome
";
        var i = Compiler.Compile(json);
        var output = string.Empty;

        foreach (var o in i.ParseNamed(text))
        {
            output += o["C1"] + ";";
        }

        Assert.That(" is   ;bird  ;", Is.EqualTo(output));
    }

    [Test]
    public void ParseTableTest4()
    {
        var json = @"
            table {
                include: section {
                    start: startswith {s:[""foo""]}
                    stop: startswith {s:[""bar""]}
                }
                row: contains {s:["" ""]i} #this is a comment
                props:[
                    prop {
                        name: ""C1""
                        parsers:[
                            col { i:""1"" }
                        ]
                    }
                ]
            }
";
        var text = @"
this  is   a    row
this  is   a    row
foo
Jason Wall Col1 Col2
this  is   a    row
free  bird is   awesome
bar
this  is   a    row
this  is   a    row
";
        var i = Compiler.Compile(json);
        var output = string.Empty;

        using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
        using (var sr = new StreamReader(ms))
        {
            foreach (var o in (IEnumerable<object[]>)((IProperty)i).Parse(sr))
            {
                output += o[0] + ";";
            }
        }
        Assert.That("Wall;is;bird;", Is.EqualTo(output));
        var items = ((Table)i).Parse((StreamReader?)null).ToList();

        Assert.That(0, Is.EqualTo(items.Count));
    }

    [Test]
    public void ParseTableTest5()
    {
        var json = @"
            table {
                row: contains {s:["" ""]i} #this is a comment
                props:[
                    prop {
                        name: ""C1""
                        parsers:[
                            col { i:""1"" }
                        ]
                    }
                ]
                validate:'blah'
            }
";
        try
        {
            Compiler.Compile(json);
            Assert.Fail();
        }
        catch (ParseException)
        { }

        json = @"
            table {
                props:[
                    prop {
                        name: ""C1""
                        parsers:[
                            col { i:""1"" }
                        ]
                    }
                ]
            }
";
        try
        {
            Compiler.Compile(json);
            Assert.Fail();
        }
        catch (ParseException)
        { }

        json = @"
            table {
                row: contains {s:["" ""]i} #this is a comment
            }
";
        try
        {
            Compiler.Compile(json);
            Assert.Fail();
        }
        catch (ParseException)
        { }
    }

    [Test]
    public void Table_DeriveColumnPositions1()
    {
        var text = @"
Jason Wall Col1 Col2
this  is   a    row
free  bird is   awesome
".ToLines().ToList();
        var pos = Table.DeriveColumnPositions(text);
        Assert.That(0, Is.EqualTo(pos.Positions["Jason"].Start));
        Assert.That(6, Is.EqualTo(pos.Positions["Wall"].Start));
        Assert.That(11, Is.EqualTo(pos.Positions["Col1"].Start));
        Assert.That(16, Is.EqualTo(pos.Positions["Col2"].Start));
        Assert.That(6, Is.EqualTo(pos.Positions["Jason"].Length));
        Assert.That(5, Is.EqualTo(pos.Positions["Wall"].Length));
        Assert.That(5, Is.EqualTo(pos.Positions["Col1"].Length));
        Assert.That(-1, Is.EqualTo(pos.Positions["Col2"].Length));
        Assert.That(4, Is.EqualTo(pos.Positions.Count));
    }

    [Test]
    public void Table_DeriveColumnPositions2()
    {
        var text = @"
Jason Wall Col1 Col2
this  is   a    row
free  bird is   awesome
".ToLines().ToList();
        var pos = Table.DeriveColumnPositions(text, true);
        Assert.That(0, Is.EqualTo(pos.Positions["Jason"].Start));
        Assert.That(6, Is.EqualTo(pos.Positions["Wall"].Start));
        Assert.That(11, Is.EqualTo(pos.Positions["Col1"].Start));
        Assert.That(16, Is.EqualTo(pos.Positions["Col2"].Start));
        Assert.That(6, Is.EqualTo(pos.Positions["Jason"].Length));
        Assert.That(5, Is.EqualTo(pos.Positions["Wall"].Length));
        Assert.That(5, Is.EqualTo(pos.Positions["Col1"].Length));
        Assert.That(-1, Is.EqualTo(pos.Positions["Col2"].Length));
        Assert.That(4, Is.EqualTo(pos.Positions.Count));
    }

    [Test]
    public void Table_DeriveColumnPositions3()
    {
        var text = @"
Jason Wall Col1    Col2
this  is      the  row
free  bird  is    awesome
".ToLines().ToList();
        var pos = Table.DeriveColumnPositions(text, true);
        Assert.That(0, Is.EqualTo(pos.Positions["Jason"].Start));
        Assert.That(6, Is.EqualTo(pos.Positions["Wall"].Start));
        Assert.That(11, Is.EqualTo(pos.Positions["Col1"].Start));
        Assert.That(18, Is.EqualTo(pos.Positions["Col2"].Start));
        Assert.That(6, Is.EqualTo(pos.Positions["Jason"].Length));
        Assert.That(5, Is.EqualTo(pos.Positions["Wall"].Length));
        Assert.That(7, Is.EqualTo(pos.Positions["Col1"].Length));
        Assert.That(-1, Is.EqualTo(pos.Positions["Col2"].Length));
        Assert.That(4, Is.EqualTo(pos.Positions.Count));
        Assert.That(pos.Positions["Jason"].GetColumnValue("this  is      the  row"), Is.EqualTo("this  "));
        Assert.That(pos.Positions["Wall"].GetColumnValue("this  is      the  row"), Is.EqualTo("is   "));
        Assert.That(pos.Positions["Col1"].GetColumnValue("this  is      the  row"), Is.EqualTo("   the "));
        Assert.That(pos.Positions["Col2"].GetColumnValue("this  is      the  row"), Is.EqualTo(" row"));

        Assert.That(pos.Positions["Jason"].GetColumnValue("free  bird  is    awesome"), Is.EqualTo("free  "));
        Assert.That(pos.Positions["Wall"].GetColumnValue("free  bird  is    awesome"), Is.EqualTo("bird "));
        Assert.That(pos.Positions["Col1"].GetColumnValue("free  bird  is    awesome"), Is.EqualTo(" is    "));
        Assert.That(pos.Positions["Col2"].GetColumnValue("free  bird  is    awesome"), Is.EqualTo("awesome"));
    }

    [Test]
    public void Table_DeriveColumnPositions4()
    {
        var text = @"
The Jason Wall 1 Col 1    Col 1
--------- ------ ------ --------
this      is      the    row
free      bird   is      awesome
".ToLines().ToList();
        var pos = Table.DeriveColumnPositions(text, true);
        Assert.That(0, Is.EqualTo(pos.Positions["The Jason"].Start));
        Assert.That(10, Is.EqualTo(pos.Positions["The Jason"].Length));

        Assert.That(10, Is.EqualTo(pos.Positions["Wall 1"].Start));
        Assert.That(7, Is.EqualTo(pos.Positions["Wall 1"].Length));

        Assert.That(17, Is.EqualTo(pos.Positions["Col 1"].Start));
        Assert.That(7, Is.EqualTo(pos.Positions["Col 1"].Length));

        Assert.That(24, Is.EqualTo(pos.Positions["Col 11"].Start));
        Assert.That(-1, Is.EqualTo(pos.Positions["Col 11"].Length));

        Assert.That(4, Is.EqualTo(pos.Positions.Count));
        Assert.That(" the   ", Is.EqualTo(pos.Positions["Col 1"].GetColumnValue("this      is      the    row")));
    }

    [Test]
    public void Include()
    {
        var exp_text = @" prop {
                        line: contains {s:[""an"" ""more""]i} #this is a comment
                        split:""foo""
                        parsers:[
                            to {
                                s:[""is"", ""more""]w
                            }
                        ]
                    }";

        try
        {
            // ReSharper disable once ObjectCreationAsStatement
            _ = new Property(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ParseException) { }

        exp_text = @" prop {
                        name:""wall""
                        line: contains {s:[""an"" ""more""]i} #this is a comment
                        split:""foo""
                    }";

        try
        {
            // ReSharper disable once ObjectCreationAsStatement
            _ = new Property(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ParseException) { }
    }

    [Test]
    public void PropertyTest1()
    {
        var exp_text = @" section {
                    start: startswith {s:[""foo""]}
                    stop: startswith {s:[""bar""]}
                    include_stop: 'true'
                    include_start: 'true'
                }";
        var inc = new Include(new Parser().Parse(exp_text).First());
        Assert.That(true, Is.EqualTo(inc.IncludeStart));
        Assert.That(true, Is.EqualTo(inc.IncludeStop));
    }

    [Test]
    public void ExpresssionTest_Base()
    {
        var exp_text = @" reg {
                                s:[""foo""]i #required
                              }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        exp_text = @" starts {
                                s:[""foo""]i #required
                              }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        exp_text = @" endswith {
                                s:[""foo""]i #required
                              }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        exp_text = @" contains {
                                s:[""foo""]i #required
                              }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        exp_text = @" count {
                                s:[""foo""]i #required
                                rng: ""1-4,6-10"" #required
                            }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        exp_text = @" look {
                                    a:[""bar""]i # optional
                                    s:[""foo""]ni #required
                                    b:[""baz""]i # optional
                                  }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        exp_text = @" and {
                                    contains {s:[""foo""]i}
                                    contains {s: [""bar""]i}
                                 }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        exp_text = @"or {
                                    contains {s:[""foo""]i}
                                    contains {s: [""bar""]i}
                                 }";
        Base.ToExpression(new Parser().Parse(exp_text).First());
        try
        {
            exp_text = @"jason {a:[""Foo""]ni }";

            Base.ToExpression(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ArgumentException)
        {
        }
    }

    [Test]
    public void ExpresssionTest_Regex()
    {
        var exp = new RegularExpression(["foo"], "i");
        Assert.That(exp.Test("i am Foo"), Is.True);
        exp = new RegularExpression(["foo"], "in");
        Assert.That(exp.Test("i am Foo"), Is.False);
        exp = new RegularExpression(["foo"], "");
        Assert.That(exp.Test("i am Foo"), Is.False);
        exp = new RegularExpression(["foo"], "n");
        Assert.That(exp.Test("i am Foo"), Is.True);

        var exp_text = @" reg {
                                s:[""foo""]i #required
                              }";
        exp = new RegularExpression(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("i am Foo"), Is.True);
        exp_text = @" regex {
                                s:[""foo""]in #required
                              }";
        exp = new RegularExpression(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("i am Foo"), Is.False);

        exp_text = @" reg {
                                s:[""foo""] #required
                              }";
        exp = new RegularExpression(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("i am Foo"), Is.False);

        exp_text = @" regex {
                                s:[""foo""]n #required
                              }";
        exp = new RegularExpression(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("i am Foo"), Is.True);
    }

    [Test]
    public void ExpresssionTest_StartsWith()
    {
        var exp = new StartsWith(["foo"], "i");
        Assert.That(exp.Test("Foo bar"), Is.True);
        exp = new StartsWith(["foo"], "in");
        Assert.That(exp.Test("Foo bar"), Is.False);
        exp = new StartsWith(["foo"], "");
        Assert.That(exp.Test("Foo bar"), Is.False);
        exp = new StartsWith(["foo"], "n");
        Assert.That(exp.Test("Foo bar"), Is.True);
        exp = new StartsWith(["foo"], "n");
        Assert.That(exp.Test("bar foo bar"), Is.True);

        var exp_text = @" starts {
                                s:[""foo""]i #required
                              }";
        exp = new StartsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo bar"), Is.True);
        exp_text = @" startswith {
                                s:[""foo""]in #required
                              }";
        exp = new StartsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo bar"), Is.False);

        exp_text = @" starts {
                                s:[""foo""] #required
                              }";
        exp = new StartsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo bar"), Is.False);

        exp_text = @" startswith {
                                s:[""foo""]n #required
                              }";
        exp = new StartsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo bar"), Is.True);
    }

    [Test]
    public void ExpresssionTest_EndsWith()
    {
        var exp = new EndsWith(["foo"], "i");
        Assert.That(exp.Test("Foo bar Foo"), Is.True);
        exp = new EndsWith(["foo"], "in");
        Assert.That(exp.Test("Foo barFoo"), Is.False);
        exp = new EndsWith(["foo"], "");
        Assert.That(exp.Test("Foo barFoo"), Is.False);
        exp = new EndsWith(["foo"], "n");
        Assert.That(exp.Test("Foo barFoo"), Is.True);
        exp = new EndsWith(["foo"], "n");
        Assert.That(exp.Test("bar foo Foo"), Is.True);

        var exp_text = @" endswith {
                                s:[""foo""]i #required
                              }";
        exp = new EndsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo bar Foo"), Is.True);
        exp_text = @" ends {
                                s:[""foo""]in #required
                              }";
        exp = new EndsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo barFoo"), Is.False);

        exp_text = @" endswith {
                                s:[""foo""] #required
                              }";
        exp = new EndsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo barFoo"), Is.False);

        exp_text = @" ends {
                                s:[""foo""]n #required
                              }";
        exp = new EndsWith(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo barFoo"), Is.True);
    }

    [Test]
    public void ExpresssionTest_Contains()
    {
        var exp = new Contains(["foo"], "i");
        Assert.That(exp.Test("Foo bar Foo"), Is.True);
        exp = new Contains(["foo"], "in");
        Assert.That(exp.Test("Foo barFoo"), Is.False);
        exp = new Contains(["foo"], "");
        Assert.That(exp.Test("Foo barFoo"), Is.False);
        exp = new Contains(["foo"], "n");
        Assert.That(exp.Test("Foo barFoo"), Is.True);

        var exp_text = @" contains {
                                s:[""foo""]i #required
                              }";
        exp = new Contains(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo bar Foo"), Is.True);
        exp_text = @" contains {
                                s:[""foo""]in #required
                              }";
        exp = new Contains(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo barFoo"), Is.False);

        exp_text = @" contains {
                                s:[""foo""] #required
                              }";
        exp = new Contains(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo barFoo"), Is.False);

        exp_text = @" contains {
                                s:[""foo""]n #required
                              }";
        exp = new Contains(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo barFoo"), Is.True);
    }

    [Test]
    public void ExpresssionTest_Count()
    {
        var exp = new Count(["foo"], new IntegerRangeCollection("1-4,6-10"), "i");
        Assert.That(exp.Test("Foo bar Foo"), Is.True);
        exp = new Count(["Foo"], new IntegerRangeCollection("1,4,6-10"), "");
        Assert.That(exp.Test("Foo barFoo"), Is.False);
        exp = new Count(["foo"], new IntegerRangeCollection("1,4,6-10"), "i");
        Assert.That(exp.Test("Foo barFoo foo foo foo"), Is.False);
        exp = new Count(["foo"], new IntegerRangeCollection("1,4,6-10"), "i");
        Assert.That(exp.Test("Foo bar"), Is.True);
        exp = new Count(["foo"], new IntegerRangeCollection("1,4,6-10"), "i");
        Assert.That(exp.Test("bar foo Foo  foo foo  foo foo foo foo"), Is.True);

        exp = new Count(["foo", "bar"], new IntegerRangeCollection("1,4,6-8,10"), "i");
        Assert.That(exp.Test("bar foo Foo  foo foo  foo foo foo foo"), Is.False);

        var exp_text = @"count {
                                s:[""foo""]i #required
                                rng: ""1-4,6-10"" #required
                            }";

        exp = new Count(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo bar Foo"), Is.True);
        exp_text = @"count {
                                s:[""foo""]i #required
                                rng: ""1,4,6-10"" #required
                            }";

        exp = new Count(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("Foo barFoo"), Is.False);
        Assert.That(exp.Test("Foo barFoo foo foo foo"), Is.False);
        Assert.That(exp.Test("Foo bar"), Is.True);

        try
        {
            exp_text = @"count {
                                rng: ""1,4,6-10"" #required
                            }";

            exp = new Count(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ArgumentException)
        {
        }

        try
        {
            exp_text = @"count {
                                s:[""foo""]i #required
                            }";

            exp = new Count(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ArgumentException)
        {
        }
    }

    [Test]
    public void ExpresssionTest_Look()
    {
        var exp = new Look(["bar"], "", ["foo"], "", ["baz"], "");
        Assert.That(exp.Test("foo bar baz"), Is.True);
        exp = new Look(["bar"], "i", ["Foo"], "i", ["Baz"], "i");
        Assert.That(exp.Test("foo Bar baz"), Is.True);

        var exp_text = @"look {
                                    a:[""bar""]i # optional
                                    s:[""foo""]ni #required
                                    b:[""baz""]i # optional
                                  }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("bar foo baz"), Is.True);
        exp_text = @"look {
                                    a:[""bar""]i # optional
                                    s:[""Foo""]ni #required
                                    b:[""Baz""]i # optional
                                  }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("foo Bar baz"), Is.True);

        exp_text = @"look {
                                    s:[""Foo""]ni #required
                                    b:[""Baz""]i # optional
                                  }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("foo Bar baz"), Is.True);

        exp_text = @"look {
                                    a:[""bar""]i # optional
                                    s:[""Foo""]ni #required
                                  }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("foo Bar baz"), Is.True);

        exp_text = @"look {s:[""Foo""]ni }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("foo Bar baz"), Is.False);

        exp_text = @"look {s:[""Woo""] }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("foo Bar baz"), Is.False);

        exp_text = @"look {  a:[""bar""]i # optional
                                 s:[""Foo""]ni #required
                              }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("foo jar baz"), Is.False);

        exp_text = @"look {  b:[""bar""]i # optional
                                 s:[""Foo""]ni #required
                              }";

        exp = new Look(new Parser().Parse(exp_text).First());
        Assert.That(exp.Test("foo jar baz"), Is.False);
        try
        {
            exp_text = @"look {a:[""Foo""]ni }";

            exp = new Look(new Parser().Parse(exp_text).First());
            Assert.That(exp.Test("foo Bar baz"), Is.False);
            Assert.Fail();
        }
        catch (ArgumentException)
        {
        }
    }

    [Test]
    public void ExpresssionTest_And()
    {
        var exp1 = new Contains(["foo"], "i");
        var exp2 = new Contains(["bar"], "i");
        var and1 = new And(new[] { exp1, exp2 });

        Assert.That(and1.Test("blah foo blah bar blah"), Is.True);
        Assert.That(and1.Test("blah foo blah ar blah"), Is.False);

        var exp_text = @" and {
                                    contains {s:[""foo""]i}
                                    contains {s: [""bar""]i}
                                 }";
        var tags = new Parser().Parse(exp_text);
        var and2 = new And(tags.First());
        Assert.That(and2.Test("blah foo blah bar blah"), Is.True);
        Assert.That(and2.Test("blah foo blah ar blah"), Is.False);
    }

    [Test]
    public void ExpresssionTest_Or()
    {
        var exp1 = new Contains(["foo"], "i");
        var exp2 = new Contains(["bar"], "i");
        var exp = new Or(new[] { exp1, exp2 });

        Assert.That(exp.Test("blah foo blah "), Is.True);
        Assert.That(exp.Test("blah bar blah "), Is.True);
        Assert.That(exp.Test("blah blah "), Is.False);

        var exp_text = @" or {
                                    contains {s:[""foo""]i}
                                    contains {s: [""bar""]i}
                                 }";
        var tags = new Parser().Parse(exp_text);
        var or2 = new Or(tags.First());
        Assert.That(or2.Test("blah foo blah "), Is.True);
        Assert.That(or2.Test("blah bar blah "), Is.True);
        Assert.That(or2.Test("blah blah "), Is.False);
    }

    [Test]
    public void ExtractorTest_Base()
    {
        var exp_text = @" split {
                                        s:[""foo""]irt #required
                                        i: ""2""  #required
                                    }
            ";
        Extractors.Base.ToExtractor(new Parser().Parse(exp_text).First());

        exp_text = @"  to {
                                        s:[""foo""]irt #required
                                        q: ""2""  #required
                                    }";
        Extractors.Base.ToExtractor(new Parser().Parse(exp_text).First());

        exp_text = @"after {
                                        s:[""foo""]irt #required
                                        q: ""2""  #required
                                    }";
        Extractors.Base.ToExtractor(new Parser().Parse(exp_text).First());

        try
        {
            exp_text = @"jason {a:[""Foo""]ni }";

            Extractors.Base.ToExtractor(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ArgumentException)
        {
        }

        try
        {
            exp_text = @"after {
                                        s:[""foo""]irt #required
                                        q: ""-1""  #required
                                    }";

            Extractors.Base.ToExtractor(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ParseException)
        {
        }

        try
        {
            exp_text = @"split {
                                        s:[""foo""]irt #required
                                        i: ""-1""  #required
                                    }";

            Extractors.Base.ToExtractor(new Parser().Parse(exp_text).First());
            Assert.Fail();
        }
        catch (ParseException)
        {
        }
    }

    [Test]
    public void Extractor_After()
    {
        var ext = new After(["foo"], "i", -1);
        Assert.That("foo blah ", Is.EqualTo(ext.Parse("blah foo blah ")));

        ext = new After(["foo"], "iw", -1);
        Assert.That(" blah ", Is.EqualTo(ext.Parse("blah Foo blah ")));
        ext = new After(["foo"], "iwt", -1);
        Assert.That("blah", Is.EqualTo(ext.Parse("blah Foo blah ")));
        ext = new After(["foo"], "iwtl", -1);
        Assert.That("what", Is.EqualTo(ext.Parse("blah Foo blah foo what ")));

        ext = new After(["foo"], "iwtl", 2);
        Assert.That("what foo ho", Is.EqualTo(ext.Parse("blah Foo blah foo what foo ho ")));
    }

    [Test]
    public void Extractor_To()
    {
        var ext = new To(["foo"], "i", -1);
        Assert.That("blah ", Is.EqualTo(ext.Parse("blah foo blah ")));

        ext = new To(["foo"], "iw", -1);
        Assert.That("blah Foo", Is.EqualTo(ext.Parse("blah Foo blah ")));
        ext = new To(["foo"], "iwt", -1);
        Assert.That("blah Foo", Is.EqualTo(ext.Parse("blah Foo blah ")));
        ext = new To(["foo"], "iwtl", -1);
        Assert.That("blah Foo blah foo", Is.EqualTo(ext.Parse("blah Foo blah foo what ")));

        ext = new To(["foo"], "iwtl", 2);
        Assert.That("blah Foo blah foo", Is.EqualTo(ext.Parse("blah Foo blah foo what foo ho ")));
    }

    [Test]
    public void Extractor_Split()
    {
        var ext = new Split(["foo"], "i", 0);
        Assert.That("blah ", Is.EqualTo(ext.Parse("blah foo blue ")));
        ext = new Split(["foo"], "it", 1);
        Assert.That("blue", Is.EqualTo(ext.Parse("blah Foo blue ")));
        ext = new Split(["foo"], "ir", 1);
        Assert.That(" blue ", Is.EqualTo(ext.Parse("blah foofoo blue ")));

        try
        {
            ext = new Split(["foo"], "", -1);
            Assert.That(" blue ", Is.EqualTo(ext.Parse("blah foofoo blue ")));
            Assert.Fail();
        }
        catch (ArgumentException)
        {
        }

        try
        {
            ext = new Split(["foo"], "r", 2);
            ext.Parse("blah foofoo blue ");
            Assert.Fail();
        }
        catch (IndexOutOfRangeException)
        {
        }

        try
        {
            var exp_text = @" split {
                                        s:[""foo""]irt #required
                                        i: ""2""  #required
                                    }
            ";
            var exp = new Split(new Parser().Parse(exp_text).First());
            exp.Parse("blah foofoo blue ");
            Assert.Fail();
        }
        catch (IndexOutOfRangeException)
        {
        }
    }

    [Test]
    public void Extractor_Column()
    {
        var ext = new Column(1)
        {
            Positions = []
        };
        ext.Positions.Add(new ColumnPosition
        {
            Name = "Jason",
            Key = "Jason",
            Start = 2,
            Length = 5
        });
        ext.Positions.Add(new ColumnPosition
        {
            Name = "Wall",
            Key = "Wall",
            Start = 8,
            Length = 4
        });
        Assert.That("Wall", Is.EqualTo(ext.Parse("  Jason Wall ")));

        var exp_text = @" col {
                                        i:""1"" #optional
                                        n: ""Jason"" #optional
                                    }";
        ext = new Column(new Parser().Parse(exp_text).First())
        {
            Positions = []
        };
        ext.Positions.Add(new ColumnPosition
        {
            Name = "Jason",
            Key = "Jason",
            Start = 2,
            Length = 5
        });
        ext.Positions.Add(new ColumnPosition
        {
            Name = "Wall",
            Key = "Wall",
            Start = 8,
            Length = 4
        });
        Assert.That("Wall", Is.EqualTo(ext.Parse("  Jason Wall ")));

        try
        {
            exp_text = @" col {
                                        i:""3"" #optional
                                        n: ""Jason"" #optional
                                    }";
            ext = new Column(new Parser().Parse(exp_text).First())
            {
                Positions = []
            };
            ext.Positions.Add(new ColumnPosition
            {
                Name = "Jason",
                Key = "Jason",
                Start = 2,
                Length = 5
            });
            ext.Positions.Add(new ColumnPosition
            {
                Name = "Wall",
                Key = "Wall",
                Start = 8,
                Length = 4
            });
            ext.Parse("  Jason Wall ");
            Assert.Fail();
        }
        catch (ArgumentException)
        { }
    }

    [Test]
    public void ColumnPostionTest1()
    {
        var ext = new ColumnPosition
        {
            Name = "Jason",
            Start = 5,
            Length = 5
        };
        Assert.That("Jason: 5-9", Is.EqualTo(ext.ToString()));
        ext.Start = 5;
        ext.Length = -1;
        Assert.That("Jason: 5", Is.EqualTo(ext.ToString()));
        ext.Start = 5;
        ext.Length = 0;
        Assert.That("Jason: 5-5", Is.EqualTo(ext.ToString()));

        Assert.That("", Is.EqualTo(ext.GetColumnValue("test")));
        ext.Start = 5;
        ext.Length = 2;
        Assert.That("56", Is.EqualTo(ext.GetColumnValue("0123456789")));
        ext.Start = 5;
        ext.Length = 20;
        Assert.That("56789", Is.EqualTo(ext.GetColumnValue("0123456789")));
    }

    // Parsing runs on a worker task so a tokenizer that loops forever fails the test instead of hanging the run.
    private static List<Tag> ParseWithin(Parser parser, string input, CancellationToken cancellationToken)
    {
        var task = Task.Run(() => parser.Parse(input), cancellationToken);
        try
        {
            task.Wait(cancellationToken);
        }
        catch (AggregateException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
        }
        return task.Result;
    }

    [TestCase("foo { }")]
    [TestCase("foo { }\n")]
    [TestCase("foo { }\r\n")]
    [TestCase("foo {\n}\n")]
    [TestCase("foo {\r\n}\r\n")]
    [TestCase("foo { } # trailing comment")]
    [TestCase("foo { } # trailing comment\n")]
    [TestCase("foo { } # trailing comment\r\n")]
    [TestCase("# leading comment\nfoo { }")]
    [TestCase("foo { };")]
    [TestCase("foo { },")]
    [TestCase("foo { bar: \"x\" }")]
    [CancelAfter(10000)]
    public void CompileParse_WellFormed_ReturnsOneObject(string input, CancellationToken cancellationToken)
    {
        var tags = ParseWithin(new Parser(), input, cancellationToken);

        Assert.That(tags, Has.Count.EqualTo(1));
        Assert.That(tags[0].Name, Is.EqualTo("foo"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\n")]
    [TestCase("\r\n")]
    [TestCase(" \t\r\n ")]
    [TestCase(";")]
    [TestCase(",")]
    [TestCase("# only a comment")]
    [TestCase("# only a comment\r\n")]
    [CancelAfter(10000)]
    public void CompileParse_NothingToParse_ReturnsEmpty(string input, CancellationToken cancellationToken)
    {
        Assert.That(ParseWithin(new Parser(), input, cancellationToken), Is.Empty);
    }

    [TestCase("Foo { }")]
    [TestCase("1 { }")]
    [TestCase("! { }")]
    [TestCase("foo { Bar: \"x\" }")]
    [TestCase("foo { bar: \"x\" 9 }")]
    [TestCase("foo { }\n!")]
    [TestCase("foo { }\r\nX")]
    [TestCase("}")]
    [TestCase("]")]
    [TestCase("foo { } }")]
    [TestCase("foo { ] }")]
    [TestCase("bar: \"x\"")]
    [TestCase("s: [\"x\"]")]
    [TestCase("[\"x\"]")]
    [TestCase("bar: foo { }")]
    [TestCase("foo {")]
    [TestCase("foo { bar: \"x")]
    [CancelAfter(10000)]
    public void CompileParse_Malformed_ThrowsParseException(string input, CancellationToken cancellationToken)
    {
        Assert.Throws<ParseException>(() => ParseWithin(new Parser(), input, cancellationToken));
    }

    [Test]
    [CancelAfter(10000)]
    public void CompileParse_AfterFailure_SameParserStartsClean(CancellationToken cancellationToken)
    {
        var parser = new Parser();
        Assert.Throws<ParseException>(() => ParseWithin(parser, "foo {", cancellationToken));

        var tags = ParseWithin(parser, "bar { }", cancellationToken);

        Assert.That(tags, Has.Count.EqualTo(1));
        Assert.That(tags[0].Name, Is.EqualTo("bar"));
    }
}