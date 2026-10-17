using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JMW.Collections;
using JMW.Extensions;
using JMW.Networking.Parsers;
using JMW.Parsing.Compile;
using JMW.Parsing.Handlers;
using JMW.Templates;
using JMW.Types;
using TemplateTable = JMW.Templates.Tags.Table;

namespace JMW.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class ParsingBenchmarks
{
    private sealed class FastConfig : ManualConfig
    {
        public FastConfig()
        {
            AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(3)
                .WithIterationCount(10));
        }
    }

    private const int Rows = 10_000;

    private const string TableInstructions = """
        table {
            row: contains {s:[" "]}
            props:[
                prop { name: "C0" parsers:[ col { i:"0" } ] }
                prop { name: "C1" parsers:[ col { i:"1" } ] }
                prop { name: "C2" parsers:[ col { i:"2" } ] }
                prop { name: "C3" parsers:[ col { i:"3" } ] }
                prop { name: "C4" parsers:[ col { i:"4" } ] }
                prop { name: "C5" parsers:[ col { i:"5" } ] }
            ]
        }
        """;

    private const string WhereTemplate = """
        <tab name="t" where="x2 == 'up'"><num exp="x1 + 1"/>\n</tab>
        """;

    private const int BodyRows = 5_000;

    private const string BodyTemplate = """
        <tab name="w"><c0/>,<c1/>,<c2/>,<c3/>,<c4/>,<c5/>,<c6/>,<c7/>,<c8/>,<c9/><if column="c1" exp="x1 == 'up'">,U</if><if column="c2, c3" exp="x1 == x2">,S</if>\n</tab>
        """;

    private List<string> templateRows = [];
    private List<string> bodyRows = [];
    private string tableText = string.Empty;
    private string netstatText = string.Empty;
    private int[] fragmented = [];
    private int[] contiguous = [];
    private LongRange[] disjoint = [];
    private readonly IParser tableParser = Compiler.Compile(TableInstructions);

    [GlobalSetup]
    public void GlobalSetup()
    {
        templateRows = ["num,state", .. Enumerable.Range(0, Rows).Select(static i => i + "," + (i % 2 == 0 ? "up" : "down"))];
        bodyRows =
        [
            "c0,c1,c2,c3,c4,c5,c6,c7,c8,c9",
            .. Enumerable.Range(0, BodyRows).Select(static i =>
                i + "," + (i % 2 == 0 ? "up" : "down") + "," + i % 3 + "," + i % 5 + ",Gi1/0/" + i % 48 + ",full,1000," + i % 4094 + ",copper,desc" + i)
        ];

        var table = new StringBuilder("Interface  Status  Speed  Duplex  Vlan  Type\n");
        for (int i = 0; i < Rows; i++)
        {
            table.Append("Gi1/0/").Append(i % 48).Append("  connected  1000  full  ").Append(i % 4094).Append("  copper\n");
        }

        tableText = table.ToString();

        var netstat = new StringBuilder("Routing tables\n\nInternet:\nDestination        Gateway            Flags               Netif Expire\n");
        for (int i = 0; i < Rows; i++)
        {
            netstat.Append("10.").Append(i / 256 % 256).Append('.').Append(i % 256).Append(".0/24       link#15            UCS                   en0      !\n");
        }

        netstatText = netstat.ToString();
        fragmented = Enumerable.Range(0, 4094).Select(static i => i * 2).ToArray();
        contiguous = Enumerable.Range(1, 4094).ToArray();
        disjoint = Enumerable.Range(0, 5_000).Select(static i => new LongRange(i * 2L, i * 2L)).ToArray();

        // Each benchmark has to do the work it names, or it measures an early exit.
        int filtered = TemplateWhere().Split('\n').Length - 1;
        string body = TemplateRowBody();
        int bodyLines = body.Split('\n').Length - 1;
        int parsed = ParseTable();
        int routes = NetstatParse();
        Console.WriteLine($"// setup counts: where {filtered}, body {bodyLines}, table {parsed}, netstat {routes}");
        if (filtered < Rows / 2 || bodyLines != BodyRows || !body.Contains(",U", StringComparison.Ordinal) || !body.Contains(",S", StringComparison.Ordinal) || parsed < Rows || routes < Rows)
        {
            throw new InvalidOperationException($"Unexpected counts: where {filtered}, body {bodyLines}, table {parsed}, netstat {routes}.");
        }
    }

    [Benchmark]
    public string TemplateWhere()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        var interpreter = new Interpreter(writer);
        interpreter.AddHandler(new TemplateTable(new TableData("t", templateRows), interpreter));
        interpreter.Eval(WhereTemplate);
        return sb.ToString();
    }

    [Benchmark]
    public string TemplateRowBody()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        var interpreter = new Interpreter(writer);
        interpreter.AddHandler(new TemplateTable(new TableData("w", bodyRows), interpreter));
        interpreter.Eval(BodyTemplate);
        return sb.ToString();
    }

    [Benchmark]
    public int ParseTable() => tableParser.Parse(tableText).Count();

    [Benchmark]
    public int NetstatParse() => NetStat.Parse(new StringReader(netstatText)).Count();

    [Benchmark]
    public int CollapseFragmented() => fragmented.CollapseIntsToRanges().Length;

    [Benchmark]
    public int CollapseContiguous() => contiguous.CollapseIntsToRanges().Length;

    [Benchmark]
    public int RangeCollectionAdd()
    {
        var collection = new LongRangeCollection();
        foreach (LongRange range in disjoint)
        {
            collection.Add(range);
        }

        return collection.Ranges.Count;
    }
}
