using JMW.Networking.Parsers;
using JMW.Tests;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace JMW.Parsing.Tests;

[TestFixture]
public class NetstatParsingTest : TestBase
{
    [Test]
    public void ParseTest()
    {
        // var dir = Path.Combine(ExecutionPath, "Networking", "macnetstat.txt");
        // using var st = new FileStream(dir, FileMode.Open, FileAccess.Read);
        // using var reader = new StreamReader(st);

        // var table = NetStat.Parse(reader).ToArray();
        // foreach (var row in table)
        // {
        //     foreach (var kvp in row)
        //     {
        //         Console.Write(kvp.Value.PadRight(60));
        //     }
        //     Console.WriteLine();
        // }
    }

    [Test]
    public void Parse_MacRoutingTable_ReadsBothTables()
    {
        var path = Path.Combine(ExecutionPath, "Networking", "macnetstat.txt");
        using var reader = new StreamReader(path);

        var rows = NetStat.Parse(reader).ToArray();

        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows[0]["Destination"], Is.EqualTo("default"));
        Assert.That(rows[0]["Gateway"], Is.EqualTo("10.181.10.1"));
        Assert.That(rows[0]["Netif"], Is.EqualTo("en0"));
        Assert.That(rows.Any(r => r["Gateway"] == "fe80::%utun0"), Is.True);
    }

    private const string Header = "Destination        Gateway            Flags               Netif Expire";

    [TestCase("default            10.181.10.1        UGScg                 en0", "default", "10.181.10.1", "UGScg", "en0", "")]
    [TestCase("10.181.10.1        0:0:c:7:ac:b5      UHLWIir               en0   1179", "10.181.10.1", "0:0:c:7:ac:b5", "UHLWIir", "en0", "1179")]
    [TestCase("default            10.181.10.1        UGScg", "default", "10.181.10.1", "UGScg", "", "")]
    [TestCase("default            10.181.10.1", "default", "10.181.10.1", "", "", "")]
    [TestCase("default", "default", "", "", "", "")]
    [TestCase("default            10.181.10.1        UGScg       ", "default", "10.181.10.1", "UGScg", "", "")]
    public void Parse_RaggedRow_ClampsToRowLength(
        string row,
        string destination,
        string gateway,
        string flags,
        string netif,
        string expire
    )
    {
        foreach (var newline in new[] { "\n", "\r\n" })
        {
            var input = "Internet:" + newline + Header + newline + row + newline;

            var rows = NetStat.Parse(new StringReader(input)).ToArray();

            Assert.That(rows, Has.Length.EqualTo(1));
            Assert.That(rows[0]["Destination"], Is.EqualTo(destination));
            Assert.That(rows[0]["Gateway"], Is.EqualTo(gateway));
            Assert.That(rows[0]["Flags"], Is.EqualTo(flags));
            Assert.That(rows[0]["Netif"], Is.EqualTo(netif));
            Assert.That(rows[0]["Expire"], Is.EqualTo(expire));
        }
    }

    [TestCase("")]
    [TestCase("\n")]
    [TestCase("netstat -rn\nRouting tables\n")]
    [TestCase("Internet:\n" + Header + "\n")]
    public void Parse_NoRows_ReturnsEmpty(string input)
    {
        Assert.That(NetStat.Parse(new StringReader(input)), Is.Empty);
    }

    [TestCase("Name     Value    Value\nfoo      1        2\n")]
    [TestCase("Name     Name     Value\nfoo      bar      2\n")]
    [TestCase("Value    Value    Value\n1        2        3\n")]
    public void Parse_DuplicateColumnNames_KeepsEveryColumn(string input)
    {
        var rows = NetStat.Parse(new StringReader("Internet:\n" + input)).ToArray();

        Assert.That(rows, Has.Length.EqualTo(1));
        Assert.That(rows[0], Has.Count.EqualTo(3));
    }
}