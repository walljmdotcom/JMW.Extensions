using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using JMW.Networking.Parsers.JunosConfig;

namespace JMW.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(FastConfig))]
public class TokenizerBenchmarks
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

    private const string InstructionBlock = """
        para {
            include: section {
                start: startswith {s:["foo"]}
                stop: startswith {s:["bar"]}
            }
            start: contains {s:["start"]i} #this is a comment
            props:[
                prop {
                    name:"jason"
                    line: contains {s:["is"]i} #this is a comment
                    parsers:[
                        after { s:["is"] }
                        to { s:["line"] }
                    ]
                }
            ]
        }

        """;

    private const string TemplateBlock = """
        Hello <var name='first' value="1"/> and some plain text \# not a comment
        <if test='a == b'>body text <var name="second"/></if>
        # a comment line
        </end>

        """;

    private string junos = string.Empty;
    private string instructions = string.Empty;
    private string template = string.Empty;
    private string plain = string.Empty;

    [GlobalSetup]
    public void GlobalSetup()
    {
        junos = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "junos.txt"));
        instructions = string.Concat(Enumerable.Repeat(InstructionBlock, 200));
        template = string.Concat(Enumerable.Repeat(TemplateBlock, 500));
        plain = new string('x', 100_000);

        // Each tokenizer has to run to the end, or the benchmark measures an early error.
        string parsingEnd = TokenizeInstructions();
        string templateEnd = TokenizeTemplate();
        if (parsingEnd != "EOF" || templateEnd != "EOF")
        {
            throw new InvalidOperationException($"Tokenizer stopped early: parsing '{parsingEnd}', templates '{templateEnd}'.");
        }
    }

    [Benchmark]
    public int JunosParse() => new Parser().Parse(junos).Count;

    [Benchmark]
    public string TokenizeInstructions()
    {
        var tokenizer = new JMW.Parsing.Compile.Tokenizer(instructions);
        while (tokenizer.Next() != JMW.Parsing.Compile.TokenType.Error)
        {
        }

        return tokenizer.Token.Value;
    }

    [Benchmark]
    public string TokenizeTemplate()
    {
        var tokenizer = new JMW.Templates.Tokenizer(template);
        while (tokenizer.Next() != JMW.Templates.TokenType.ERROR)
        {
        }

        return tokenizer.Token.Value;
    }

    // Peek-then-read over 100,000 characters: the access pattern every Scanner peek produces.
    [Benchmark]
    public int ReadAfterPushBack()
    {
        var reader = new LineTrackingReader(new StringReader(plain));
        int count = 0;
        int c;
        while ((c = reader.Read()) != -1)
        {
            reader.PushBack((char)c);
            _ = reader.Read();
            count++;
        }

        return count;
    }
}
