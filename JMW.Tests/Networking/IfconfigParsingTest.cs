using JMW.Networking.Parsers;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JMW.Parsing.Tests;

[TestFixture]
[NonParallelizable]
public class IfconfigParsingTest
{
    // macOS ifconfig output. utun0 ends on an inet6 line, so its block finishes with an array still open.
    private static readonly string[] SampleLines =
    [
        "lo0: flags=8049<UP,LOOPBACK,RUNNING,MULTICAST> mtu 16384",
        "\toptions=1203<RXCSUM,TXCSUM,TXSTATUS,SW_TIMESTAMP>",
        "\tinet 127.0.0.1 netmask 0xff000000",
        "\tinet6 ::1 prefixlen 128",
        "\tinet6 fe80::1%lo0 prefixlen 64 scopeid 0x1",
        "\tnd6 options=201<PERFORMNUD,DAD>",
        "en0: flags=8863<UP,BROADCAST,SMART,RUNNING,SIMPLEX,MULTICAST> mtu 1500",
        "\toptions=6460<TSO4,TSO6,CHANNEL_IO,PARTIAL_CSUM,ZEROINVERT_CSUM>",
        "\tether a4:83:e7:12:34:56",
        "\tinet6 fe80::1c2b:3d4e:5f60:7a8b%en0 prefixlen 64 secured scopeid 0xe",
        "\tinet 192.168.1.23 netmask 0xffffff00 broadcast 192.168.1.255",
        "\tnd6 options=201<PERFORMNUD,DAD>",
        "\tmedia: autoselect",
        "\tstatus: active",
        "utun0: flags=8051<UP,POINTOPOINT,RUNNING,MULTICAST> mtu 1380",
        "\tinet6 fe80::2a1b:3c4d:5e6f:7081%utun0 prefixlen 64 scopeid 0x10",
    ];

    private static string Sample(string newline, bool trailingNewline)
    {
        return string.Join(newline, SampleLines) + (trailingNewline ? newline : string.Empty);
    }

    private static string Capture(Action<TextReader> output, string input)
    {
        var original = Console.Out;
        using var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            output(new StringReader(input));
        }
        finally
        {
            Console.SetOut(original);
        }
        return captured.ToString();
    }

    [TestCase("\n", true)]
    [TestCase("\n", false)]
    [TestCase("\r\n", true)]
    [TestCase("\r\n", false)]
    public void OutputJson_MacSample_WritesEveryInterface(string newline, bool trailingNewline)
    {
        var json = Capture(Ifconfig.OutputJson, Sample(newline, trailingNewline));

        using var doc = JsonDocument.Parse(json);
        var interfaces = doc.RootElement.EnumerateArray().ToArray();
        Assert.That(interfaces.Select(i => i.GetProperty("InterfaceName").GetString()), Is.EqualTo(new[] { "lo0", "en0", "utun0" }));

        var lo0 = interfaces[0];
        Assert.That(lo0.GetProperty("Flags").GetProperty("Flags").GetString(), Is.EqualTo("8049"));
        Assert.That(
            lo0.GetProperty("Flags").GetProperty("Values").EnumerateArray().Select(v => v.GetString()),
            Is.EqualTo(new[] { "UP", "LOOPBACK", "RUNNING", "MULTICAST" })
        );
        Assert.That(lo0.GetProperty("Mtu").GetString(), Is.EqualTo("16384"));
        Assert.That(lo0.GetProperty("Inet")[0].GetProperty("Inet").GetString(), Is.EqualTo("127.0.0.1"));
        Assert.That(lo0.GetProperty("Inet")[0].GetProperty("Netmask").GetString(), Is.EqualTo("0xff000000"));
        Assert.That(lo0.GetProperty("Inet6").GetArrayLength(), Is.EqualTo(2));
        Assert.That(lo0.GetProperty("Inet6")[0].GetProperty("Inet6").GetString(), Is.EqualTo("::1"));
        Assert.That(lo0.GetProperty("Inet6")[1].GetProperty("Inet6").GetString(), Is.EqualTo("fe80::1"));
        Assert.That(lo0.GetProperty("Inet6")[1].GetProperty("Interface").GetString(), Is.EqualTo("lo0"));
        Assert.That(lo0.GetProperty("Nd6Options").GetProperty("Nd6Options").GetString(), Is.EqualTo("201"));
        Assert.That(
            lo0.GetProperty("Nd6Options").GetProperty("Values").EnumerateArray().Select(v => v.GetString()),
            Is.EqualTo(new[] { "PERFORMNUD", "DAD" })
        );

        var en0 = interfaces[1];
        Assert.That(en0.GetProperty("Ether").GetString(), Is.EqualTo("a4:83:e7:12:34:56"));
        Assert.That(en0.GetProperty("Inet6")[0].GetProperty("Inet6").GetString(), Is.EqualTo("fe80::1c2b:3d4e:5f60:7a8b"));
        Assert.That(en0.GetProperty("Inet")[0].GetProperty("Inet").GetString(), Is.EqualTo("192.168.1.23"));
        Assert.That(en0.GetProperty("Inet")[0].GetProperty("Broadcast").GetString(), Is.EqualTo("192.168.1.255"));
        Assert.That(en0.GetProperty("Nd6Options").GetProperty("Nd6Options").GetString(), Is.EqualTo("201"));
        Assert.That(en0.GetProperty("Media").GetString(), Is.EqualTo("autoselect"));
        Assert.That(en0.GetProperty("Status").GetString(), Is.EqualTo("active"));

        var utun0 = interfaces[2];
        Assert.That(utun0.GetProperty("Inet6")[0].GetProperty("Inet6").GetString(), Is.EqualTo("fe80::2a1b:3c4d:5e6f:7081"));
        Assert.That(utun0.GetProperty("Inet6")[0].GetProperty("Scopeid").GetString(), Is.EqualTo("0x10"));
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    public void OutputKeyValues_MacSample_KeepsMultiKeywordPairs(string newline)
    {
        var text = Capture(Ifconfig.OutputKeyValues, Sample(newline, true));

        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        Assert.That(lines.Count(l => l == "Nd6Options: 201"), Is.EqualTo(2));
        Assert.That(lines, Has.Member("InterfaceName: utun0"));
        Assert.That(lines, Has.Member("Status: active"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\n")]
    [TestCase("\r\n\r\n")]
    public void OutputJson_NoInterfaces_WritesEmptyArray(string input)
    {
        var json = Capture(Ifconfig.OutputJson, input);

        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(0));
    }

    [TestCase("lo0: flags=8049<UP,LOOPBACK,RUNNING,MULTICAST> mtu 16384\n\tinet6 ::1 prefixlen 128\n")]
    [TestCase("lo0: flags=8049<UP,LOOPBACK,RUNNING,MULTICAST> mtu 16384\n\tinet 127.0.0.1 netmask 0xff000000")]
    [TestCase("lo0: flags=8049<UP,LOOPBACK,RUNNING,MULTICAST> mtu 16384\r\n\tinet6 ::1 prefixlen 128\r\n")]
    public void OutputJson_BlockEndingInArray_ClosesTheArray(string input)
    {
        var json = Capture(Ifconfig.OutputJson, input);

        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(1));
        Assert.That(doc.RootElement[0].GetProperty("InterfaceName").GetString(), Is.EqualTo("lo0"));
    }
}
