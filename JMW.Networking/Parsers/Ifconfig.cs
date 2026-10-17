using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;

namespace JMW.Networking.Parsers;

public enum OutputType
{
    Json,
    KeyValue,
}

public record ParsingOptions(OutputType OutputType);

public static class Ifconfig
{
    #region Public Parse/Output

    public static void Parse(TextReader output, ParsingOptions options)
    {
        if (options.OutputType == OutputType.KeyValue)
        {
            OutputKeyValues(output);
        }
        else if (options.OutputType == OutputType.Json)
        {
            OutputJson(output);
        }
    }

    public static void OutputKeyValues(TextReader output)
    {
        var blocks = GetBlocks(output);

        foreach (var block in blocks)
        {
            Console.WriteLine(); // empty line for a spacer

            using var blockReader = new StringReader(block);
            var pairs = GetPairs(Tokenize(blockReader));

            foreach (var pair in pairs)
            {
                pair.WriteKeyValues(Console.Out);
            }
        }
    }

    public static void OutputJson(TextReader output)
    {
        var blocks = GetBlocks(output);

        using var json = new StringWriter();
        var writer = new JsonWriter(json);

        writer.WriteStartArray();

        foreach (var block in blocks)
        {
            using var blockReader = new StringReader(block);
            var pairs = GetPairs(Tokenize(blockReader));

            writer.WriteStartObject();
            WritePairsJson(writer, pairs);

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        Console.WriteLine(json.ToString());
    }

    private static void WritePairsJson(JsonWriter writer, IEnumerable<Pair> pairs)
    {
        Pair? last = null;
        bool arrayStarted = false;
        foreach (var pair in pairs)
        {
            if (arrayPairs.Contains(pair.Key))
            {
                if (last is not null && last.Key != pair.Key && arrayStarted)
                {
                    writer.WriteEndArray();
                    arrayStarted = false;
                }

                if (!arrayStarted)
                {
                    writer.WriteStartArray(CleanKey(pair.Key));
                    arrayStarted = true;
                }

                pair.WriteJson(writer, arrayStarted);
                last = pair;
                continue;
            }

            if (arrayStarted)
            {
                arrayStarted = false;
                writer.WriteEndArray();
            }

            pair.WriteJson(writer);
        }

        if (arrayStarted)
        {
            writer.WriteEndArray();
        }
    }

    private static string CleanKey(string key)
    {
        // remove non alphabet chars
        // convert to PascalCase
        var sb = new StringBuilder();
        char? last = null;
        foreach (var current in key)
        {
            if (char.IsDigit(current) || char.IsLetter(current))
            {
                sb.Append(last is null || char.IsWhiteSpace(last.Value)
                    ? char.ToUpperInvariant(current)
                    : current);
            }
            last = current;
        }
        return sb.ToString();
    }

    #endregion Public Parse/Output

    private record Pair(string Key, string Value, Pair[] Children, string ChildType = "object")
    {
        public void WriteJson(JsonWriter writer, bool arrayStarted = false)
        {
            var key = CleanKey(Key);

            if (Children?.Length > 0)
            {
                if (arrayStarted)
                {
                    writer.WriteStartObject();
                }
                else
                {
                    writer.WriteStartObject(key);
                }

                if (Value.Length > 0)
                {
                    writer.WriteString(key, Value);
                }
                if (ChildType == "array")
                {
                    writer.WriteStartArray("Values");
                    foreach (var child in Children)
                    {
                        writer.WriteStringValue(child.Value);
                    }
                    writer.WriteEndArray();
                }
                else
                {
                    foreach (var child in Children)
                    {
                        child.WriteJson(writer);
                    }
                }
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteString(CleanKey(Key), Value);
            }
        }

        public void WriteKeyValues(TextWriter writer, int indent = 0)
        {
            writer.WriteLine($"{string.Empty.PadLeft(indent)}{CleanKey(Key)}: {Value}");
            if (Children?.Length > 0)
            {
                foreach (var child in Children)
                {
                    child.WriteKeyValues(writer, indent + 4);
                }
            }
        }
    }

    private static readonly HashSet<string> arrayPairs = [
        "inet",
        "inet6",
        "agent",
        "member",
    ];

    private record GroupDefinition(
        string Kind,
        Dictionary<string, string> Keywords,
        Dictionary<string, MultipleDefinition> MultipleKeywords
        );
    private record MultipleDefinition(
        string Kind,
        Dictionary<string, string> Keywords
    );

    private const string newLine = "<newline>";
    private const string next = "<next>";
    private const string options = "<options>";
    private const string group = "<group>";
    private const string groupNext = "<groupnext>";
    private const string drop = "<drop>";

    private static readonly Dictionary<string, string> singleKeywords = new(){
        {"flags", options},
        {"eflags", options},
        {"xflags", options},
        {"hwassist", options},
        {"mtu", next},
        {"ether", next},
        {"media", newLine},
        {"status", next},
        {"priority", next},
        {"type", newLine},
        {"desc", newLine},
        {"scheduler", next},
        {"routermode4", next},
        {"routermode6", next},
        {"netif", next},
        {"flowswitch", next},
        {"options", options},
        {"index", next},
    };

    private static readonly Dictionary<string, GroupDefinition> groupKeywords = new()
    {
        {"Configuration:", new(newLine, new(){
            {"id", next},
            {"priority", next},
            {"hellotime", next},
            {"fwddelay", next},
            {"maxage", next},
            {"holdcnt", next},
            {"proto", next},
            {"maxaddr", next},
            {"timeout", next},
            {"ifcost", next},
            {"port", next},
            {"ipfilter", next},
            {"flags", next},
            {"root", group},
        }, [])},

        {"member", new(groupNext, new () {
            {"flags", options},
            {"ifmaxaddr", next},
            {"port", next},
            {"priority", next},
            {"hostfilter", next},
            {"hw", next},
            {"ip", next}
        }, new(){
            {"path", new(next, new(){{"cost", next}})}})
        },

        {"agent", new(group, new() {
            {"domain", next},
            {"type", next},
            {"flags", next},
            {"desc", newLine}
        }, [])},

        {"root", new(group, new(){
            {"id", next},
            {"priority", next},
            {"ifcost", next},
            {"port", next},
            {"ipfilter", next},
            {"flags", options},
            {"member", group},
        }, [])},

        {"inet6", new(groupNext, new() {
            {"prefixlen", next},
            {"scopeid", next},
        }, [])},
        {"inet", new(groupNext, new() {
            {"netmask", next},
            {"broadcast", next},
        }, [])},
    };

    private static readonly Dictionary<string, MultipleDefinition> multipleKeywords = new()
    {
        {"nd6",new(options, new(){{"options", options}})},
        {"root",new(next, new(){{"id", next}})},
        {"path",new(next, new(){{"cost", next}})},
        {"state",new(next, new(){{"availability", next}})},
        {"qosmarking",new(next, new(){{"enabled", next}})},
        {"generation",new(next, new(){{"id", next}})},
        {"uplink",new(next, new(){{"rate", next}})},
        {"downlink",new(next, new(){{"rate", next}})},
        {"low",new(next, new(){{"power", next}, {"mode", next}})},
        {"link",new(next, new(){{ "rate", next}, {"quality", next}})},
        {"multi",new(next, new(){{"layer", next}, {"packet", next }, { "logging", next }, { "(mpklog)", drop} })},
    };

    private static bool TryGetValue(
        IEnumerator<string> enumerator,
        Queue<string> queue,
        [NotNullWhen(true)] out string? value
    )
    {
        // first try and get it from the queue.
        if (queue.TryDequeue(out value))
        {
            return true;
        }
        // not in the queue? get it from the enumerator.
        if (enumerator.MoveNext())
        {
            value = enumerator.Current;
            return true;
        }

        value = null;
        return false;
    }

    private static IEnumerable<Pair> GetPairs(IEnumerable<string> tokens)
    {
        var queue = new Queue<string>();
        var enumerator = tokens.GetEnumerator();
        // first token should always be the interface name.
        enumerator.MoveNext();
        yield return new Pair("InterfaceName", enumerator.Current, []);

        while (TryGetValue(enumerator, queue, out var token))
        {
            if (groupKeywords.TryGetValue(token, out var group))
            {
                foreach (var item in HandleGroups(queue, enumerator, token, group))
                {
                    yield return item;
                }
            }
            else if (multipleKeywords.ContainsKey(token))
            {
                foreach (var item in HandleMultipleKeywords(queue, enumerator, multipleKeywords, token, token))
                {
                    yield return item;
                }
            }
            else if (singleKeywords.TryGetValue(token, out var kind))
            {
                if (token is null)
                {
                    throw new InvalidOperationException("Token can't be null");
                }

                foreach (var item in HandleSingleKeyword(queue, enumerator, kind, token))
                {
                    yield return item;
                }
            }
        }
    }

    private static IEnumerable<Pair> HandleGroups(
        Queue<string> queue,
        IEnumerator<string> enumerator,
        string token,
        GroupDefinition group
    )
    {
        var key = token;
        var pairChildren = new List<Pair>();
        var value = string.Empty;

        if (group.Kind == groupNext)
        {
            pairChildren.AddRange(
                HandleSingleKeyword(queue, enumerator, next, token)
            );
        }

        while (TryGetValue(enumerator, queue, out var nextToken))
        {
            if (group.Keywords.TryGetValue(nextToken, out var kind))
            {
                if (groupKeywords.TryGetValue(nextToken, out var childGroup))
                {
                    pairChildren.AddRange(HandleGroups(queue, enumerator, nextToken, childGroup));
                }
                else
                {
                    pairChildren.AddRange(
                        HandleSingleKeyword(queue, enumerator, kind, nextToken)
                    );
                }
            }
            else if (group.MultipleKeywords.ContainsKey(nextToken))
            {
                pairChildren.AddRange(
                    HandleMultipleKeywords(queue, enumerator, group.MultipleKeywords, nextToken, nextToken)
                );
            }
            else if (nextToken == newLine)
            {
                continue;
            }
            else
            {
                queue.Enqueue(nextToken); // put it back on the stack.
                break;
            }
        }

        yield return new Pair(key, value ?? string.Empty, [.. pairChildren]);
    }

    private static IEnumerable<Pair> HandleMultipleKeywords(
        Queue<string> queue,
        IEnumerator<string> enumerator,
        Dictionary<string, MultipleDefinition> definition,
        string token,
        string key)
    {
        var escape = false;
        foreach (var item in definition[token].Keywords)
        {
            if (!TryGetValue(enumerator, queue, out var next))
            {
                yield break;
            }

            if (!definition[token].Keywords.ContainsKey(next))
            {
                queue.Enqueue(next);
                escape = true;
                break;
            }
            if (item.Value != drop)
            {
                key += $" {next}";
            }
        }

        if (escape)
        {
            yield break;
        }

        foreach (var pair in HandleSingleKeyword(queue, enumerator, definition[token].Kind, key))
        {
            yield return pair;
        }
    }

    private static IEnumerable<Pair> HandleSingleKeyword(
        Queue<string> queue,
        IEnumerator<string> enumerator,
        string kind,
        string token)
    {
        if (kind == newLine)
        {
            // grab tokens until newline.
            var value = string.Empty;
            while (TryGetValue(enumerator, queue, out var next) && next != newLine)
            {
                value += $" {next}";
            }
            // last item is a newline, which we can ignore,
            yield return new Pair(token, value.Trim(), []);
        }
        else if (kind == next)
        {
            if (TryGetValue(enumerator, queue, out var value))
            {
                if (token == "inet6")
                {
                    var items = value.Split('%');
                    yield return new Pair(token, items[0], []);
                    if (items.Length > 1)
                    {
                        yield return new Pair("interface", items[1], []);
                    }
                }
                else
                {
                    yield return new Pair(token, value, []);
                }
            }
        }
        else if (kind == options)
        {
            if (TryGetValue(enumerator, queue, out var value))
            {
                var items = value.Split('<');
                var optionList = Array.Empty<Pair>();

                if (items.Length > 1)
                {
                    optionList = items[1].Trim('>').Split(',').Select(o => new Pair($"{token} item", o, [])).ToArray();
                }

                yield return new Pair(token, items[0], optionList, "array");
            }
        }
        else
        {
            throw new InvalidOperationException("Unknown Keyword Type");
        }
    }

    private static IEnumerable<string> Tokenize(TextReader output)
    {
        int? c;
        var sb = new StringBuilder();
        while ((c = output.Read()) != -1)
        {
            if (c == '=')
            {
                if (sb.Length > 0 && sb[sb.Length - 1] == ':')
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                yield return sb.ToString();
                sb.Clear();
                continue;
            }
            else if (!char.IsWhiteSpace((char)c))
            {
                sb.Append((char)c);
                continue;
            }
            else if (c == '\n')
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                yield return newLine;
                continue;
            }

            if (sb.Length == 0)
            {
                continue;
            }

            if (sb[sb.Length - 1] == ':')
            {
                sb.Remove(sb.Length - 1, 1);
            }
            // sometimes the key:value pairs are separated
            //  by a single colon.  split these.
            if (CountChar(sb, ':') == 1)
            {
                var tokens = sb.ToString().Split(':');
                yield return tokens[0];
                yield return tokens[1];
            }
            else
            {
                yield return sb.ToString();
            }

            sb.Clear();
        }

        // The last token of input that does not end in a newline.
        if (sb.Length > 0)
        {
            if (sb[sb.Length - 1] == ':')
            {
                sb.Remove(sb.Length - 1, 1);
            }
            yield return sb.ToString();
        }
    }

    private static IEnumerable<string> GetBlocks(TextReader output)
    {
        var sb = new StringBuilder();
        var last = '\0';
        int? i;
        while ((i = output.Read()) != -1)
        {
            char current = (char)i;
            if (!char.IsWhiteSpace((char)current) && last == '\n')
            {
                // new block
                if (!IsBlank(sb))
                {
                    yield return sb.ToString();
                }
                sb.Clear();
                sb.Append(current);
            }
            else
            {
                sb.Append(current);
            }

            last = current;
        }

        if (!IsBlank(sb))
        {
            yield return sb.ToString();
        }
    }

    private static bool IsBlank(StringBuilder sb)
    {
        for (var i = 0; i < sb.Length; i++)
        {
            if (!char.IsWhiteSpace(sb[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static int CountChar(StringBuilder sb, char target)
    {
        var count = 0;
        for (int i = 0; i < sb.Length; i++)
        {
            if (sb[i] == target)
            {
                count++;
            }
        }
        return count;
    }
}