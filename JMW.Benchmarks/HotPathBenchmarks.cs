using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JMW.Networking.Parsers;
using JMW.Networking.Rules;
using JMW.Parsing.Expressions;
using JMW.Parsing.Extractors;
using JMW.Parsing.Handlers;
using JMW.Templates;
using JMW.Types;
using CompileParser = JMW.Parsing.Compile.Parser;
using TemplateTable = JMW.Templates.Tags.Table;

namespace JMW.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class HotPathBenchmarks
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

    private const string SplitProperty = """
        prop {
            name: "p"
            line: contains {s:["target"]}
            split: ";;"
            parsers:[ after { s:["target"] } ]
        }
        """;

    private const string DistinctTemplate = """
        <tab name="t" distinct="num, state"><state/></tab>
        """;

    private static readonly string[] IfconfigBlock =
    [
        "en{0}: flags=8863<UP,BROADCAST,SMART,RUNNING,SIMPLEX,MULTICAST> mtu 1500",
        "\toptions=6460<TSO4,TSO6,CHANNEL_IO,PARTIAL_CSUM,ZEROINVERT_CSUM>",
        "\tether a4:83:e7:12:34:56",
        "\tinet6 fe80::1c2b:3d4e:5f60:7a8b%en{0} prefixlen 64 secured scopeid 0xe",
        "\tinet 192.168.1.23 netmask 0xffffff00 broadcast 192.168.1.255",
        "\tnd6 options=201<PERFORMNUD,DAD>",
        "\tmedia: autoselect",
        "\tstatus: active",
    ];

    private string splitText = string.Empty;
    private readonly Property splitProperty = new(new CompileParser().Parse(SplitProperty).First());
    private List<string> tableRows = [];
    private RulesEngine rulesEngine = new([]);
    private Location[] packetSources = [];
    private readonly Location anywhere = new();
    private readonly IntegerRange http = new(80, 80);
    private string ifconfigText = string.Empty;
    private string[] lines = [];
    private readonly To to = new([" "], string.Empty, -1);
    private readonly After after = new([" "], string.Empty, -1);
    private readonly Split split = new([" "], string.Empty, 1);
    private RegularExpression[] regexes = [];

    [GlobalSetup]
    public void GlobalSetup()
    {
        splitText = string.Concat(Enumerable.Range(0, Rows).Select(static i => "segment " + i + " value;;")) + "target found";

        tableRows = ["num,state", .. Enumerable.Range(0, Rows).Select(static i => i + "," + (i % 2 == 0 ? "up" : "down"))];

        // A deny-all at the top, then one allow per source, so the last match decides.
        const int ruleCount = 1_000;
        var rules = new List<FirewallRule> { new() { Source = new(), Dest = new(), Port = new IntegerRange(0, 65535) } };
        packetSources = new Location[ruleCount];
        for (var i = 0; i < ruleCount; i++)
        {
            packetSources[i] = new Location(new IPAddress(new byte[] { 10, 0, (byte)(i / 256), (byte)(i % 256) }));
            rules.Add(new FirewallRule { Source = packetSources[i], Dest = new(), Port = new IntegerRange(80, 80), Policy = Policy.Permit });
        }

        rulesEngine = new RulesEngine(rules);

        var ifconfig = new StringBuilder();
        for (var i = 0; i < 500; i++)
        {
            foreach (var line in IfconfigBlock)
                ifconfig.AppendFormat(CultureInfo.InvariantCulture, line, i).Append('\n');
        }

        ifconfigText = ifconfig.ToString();

        lines = Enumerable.Range(0, Rows).Select(static i => "Gi1/0/" + i % 48 + " connected 1000 full " + i % 4094 + " copper").ToArray();
        regexes = Enumerable.Range(0, 20).Select(static i => new RegularExpression(["^Gi1/0/" + i + " "], string.Empty)).ToArray();

        // Each benchmark has to do the work it names, or it measures an early exit.
        var property = PropertySplit();
        var distinct = TableDistinct();
        var allowed = RulesAllowed();
        var extracted = ExtractorsParse();
        var matched = RegexTest();
        Console.WriteLine($"// setup: property '{property}', distinct {distinct}, allowed {allowed}, extracted {extracted}, matched {matched}");
        if (property != "target found" || distinct != Rows / 2 * ("up".Length + "down".Length) || allowed != packetSources.Length || extracted == 0 || matched == 0)
            throw new InvalidOperationException("A benchmark did not do its work.");
    }

    [Benchmark]
    public string PropertySplit() => (string)splitProperty.Parse(new StringReader(splitText));

    [Benchmark]
    public int TableDistinct()
    {
        var sb = new StringBuilder();
        using var writer = new StringWriter(sb);
        var interpreter = new Interpreter(writer);
        interpreter.AddHandler(new TemplateTable(new TableData("t", tableRows), interpreter));
        interpreter.Eval(DistinctTemplate);
        return sb.Length;
    }

    [Benchmark]
    public int RulesAllowed()
    {
        var allowed = 0;
        foreach (var source in packetSources)
        {
            if (rulesEngine.Allowed(source, anywhere, http))
                allowed++;
        }

        return allowed;
    }

    [Benchmark]
    public int IfconfigJson()
    {
        // OutputJson writes to the console, which BenchmarkDotNet also logs to in process.
        var original = Console.Out;
        Console.SetOut(TextWriter.Null);
        try
        {
            Ifconfig.OutputJson(new StringReader(ifconfigText));
        }
        finally
        {
            Console.SetOut(original);
        }

        return ifconfigText.Length;
    }

    [Benchmark]
    public int ExtractorsParse()
    {
        var length = 0;
        foreach (var line in lines)
            length += to.Parse(line).Length + after.Parse(line).Length + split.Parse(line).Length;

        return length;
    }

    [Benchmark]
    public int RegexTest()
    {
        var matches = 0;
        foreach (var line in lines)
        {
            foreach (var regex in regexes)
            {
                if (regex.Test(line))
                    matches++;
            }
        }

        return matches;
    }
}
