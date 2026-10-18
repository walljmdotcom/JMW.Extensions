namespace JMW.TextExtraction.Tests.Records;

[TestFixture]
public class RecordInferenceTest
{
    private const string Cdp = """
        show cdp neighbors detail
        -------------------------
        Device ID: sw1.example.com
        Entry address(es):
          IP address: 10.0.0.1
        Platform: cisco WS-C3850,  Capabilities: Switch IGMP
        Interface: GigabitEthernet1/0/1,  Port ID (outgoing port): TenGigabitEthernet1/1
        Holdtime : 131 sec

        Version :
        Cisco IOS Software, Version 16.9.4
        Technical Support: http://www.cisco.com/techsupport

        advertisement version: 2
        Duplex: full
        -------------------------
        Device ID: rtr2.example.com
        Entry address(es):
          IP address: 10.0.0.2
          IP address: 10.0.9.2
        Platform: cisco ISR4331,  Capabilities: Router
        Interface: GigabitEthernet1/0/2,  Port ID (outgoing port): GigabitEthernet0/0/0
        Holdtime : 150 sec

        Version :
        Cisco IOS XE Software, Version 17.6.3

        advertisement version: 2
        Duplex: half

        Total cdp entries displayed : 2
        """;

    [Test]
    public void SplitsRecordsAtRuleLinesAndDropsEchoAndFooter()
    {
        RecordSpec spec = RecordInference.Infer(Cdp) ?? throw new AssertionException("no spec");
        List<ExtractedRecord> records = RecordExtractor.Extract(spec, Cdp);

        Assert.That(spec.Boundary, Is.EqualTo(RecordBoundary.Rule));
        Assert.That(records.Select(r => Field(r, "Device ID")), Is.EqualTo(new[] { "sw1.example.com", "rtr2.example.com" }));
    }

    [Test]
    public void ReadsKeysBesideValuesOnOneLine()
    {
        RecordSpec spec = RecordInference.Infer(Cdp) ?? throw new AssertionException("no spec");
        ExtractedRecord first = RecordExtractor.Extract(spec, Cdp)[0];

        Assert.That(Field(first, "Platform"), Is.EqualTo("cisco WS-C3850"));
        Assert.That(Field(first, "Capabilities"), Is.EqualTo("Switch IGMP"));
        Assert.That(Field(first, "Interface"), Is.EqualTo("GigabitEthernet1/0/1"));
        Assert.That(Field(first, "Port ID (outgoing port)"), Is.EqualTo("TenGigabitEthernet1/1"));
        Assert.That(Field(first, "Duplex"), Is.EqualTo("full"));
    }

    [Test]
    public void ReadsTextBelowAKeyAndNestedGroups()
    {
        RecordSpec spec = RecordInference.Infer(Cdp) ?? throw new AssertionException("no spec");
        ExtractedRecord second = RecordExtractor.Extract(spec, Cdp)[1];

        Assert.That(Field(second, "Version"), Is.EqualTo("Cisco IOS XE Software, Version 17.6.3"));
        Assert.That(
            second.Fields.Where(f => f.Key == "Entry address(es) / IP address").Select(f => f.Value),
            Is.EqualTo(new[] { "10.0.0.2", "10.0.9.2" })
        );
    }

    [Test]
    public void ValueStaysASlotEvenWhenEverySampleAgrees()
    {
        RecordSpec spec = RecordInference.Infer(Cdp) ?? throw new AssertionException("no spec");

        Assert.That(spec.Lines.Single(l => l.Key == "advertisement version:").Fields, Is.EqualTo(new[] { "advertisement version" }));
    }

    private const string Interfaces = """
        show interfaces
        Vlan1 is administratively down, line protocol is down
          Hardware is Ethernet SVI, address is bcf1.f25e.9a47 (bia bcf1.f25e.9a47)
          MTU 1500 bytes, BW 1000000 Kbit/sec, DLY 10 usec,
             reliability 255/255, txload 1/255, rxload 1/255
          5 minute output rate 0 bits/sec, 0 packets/sec
             113815629 packets input, 18151607671 bytes, 0 no buffer
             0 input errors, 0 CRC, 0 frame, 0 overrun, 0 ignored
        Vlan110 is up, line protocol is up
          Hardware is Ethernet SVI, address is bcf1.f25e.9a4d (bia bcf1.f25e.9a4d)
          MTU 9000 bytes, BW 1000000 Kbit/sec, DLY 10 usec,
             reliability 255/255, txload 1/255, rxload 1/255
          5 minute output rate 1000 bits/sec, 2 packets/sec
             5 packets input, 640 bytes, 0 no buffer
             2 input errors, 1 CRC, 0 frame, 0 overrun, 0 ignored
        GigabitEthernet1/0/1 is up, line protocol is up (connected)
          Hardware is Gigabit Ethernet, address is 0011.2233.4455 (bia 0011.2233.4455)
          MTU 1500 bytes, BW 1000000 Kbit/sec, DLY 10 usec,
             reliability 255/255, txload 1/255, rxload 1/255
          5 minute output rate 3000 bits/sec, 4 packets/sec
             77 packets input, 9000 bytes, 0 no buffer
             0 input errors, 0 CRC, 0 frame, 0 overrun, 0 ignored
        """;

    [Test]
    public void SplitsIndentedRecordsAndKeysTheHeadLine()
    {
        RecordSpec spec = RecordInference.Infer(Interfaces) ?? throw new AssertionException("no spec");
        List<ExtractedRecord> records = RecordExtractor.Extract(spec, Interfaces);

        Assert.That(spec.Boundary, Is.EqualTo(RecordBoundary.Indent));
        Assert.That(records.Select(r => Field(r, "Name")), Is.EqualTo(new[] { "Vlan1", "Vlan110", "GigabitEthernet1/0/1" }));
        Assert.That(Field(records[0], "line protocol"), Is.EqualTo("down"));
    }

    [Test]
    public void NamesValuesWrittenAheadOfTheirKey()
    {
        RecordSpec spec = RecordInference.Infer(Interfaces) ?? throw new AssertionException("no spec");
        ExtractedRecord second = RecordExtractor.Extract(spec, Interfaces)[1];

        Assert.That(Field(second, "packets input"), Is.EqualTo("5"));
        Assert.That(Field(second, "bytes"), Is.EqualTo("640"));
        Assert.That(Field(second, "input errors"), Is.EqualTo("2"));
        Assert.That(Field(second, "CRC"), Is.EqualTo("1"));
        Assert.That(Field(second, "MTU"), Is.EqualTo("9000"));
    }

    [TestCase("Device ID:sw1", "Device ID: sw1")]
    [TestCase("Device ID: sw1", "Device ID: sw1")]
    [TestCase("Device ID :sw1", "Device ID : sw1")]
    [TestCase("mtu=1500,", "mtu = 1500 ,")]
    [TestCase("time 12:32:55", "time 12:32:55")]
    [TestCase("mac 00:11:22:33:44:55", "mac 00:11:22:33:44:55")]
    [TestCase("Support: http://www.cisco.com", "Support: http://www.cisco.com")]
    [TestCase("v4:routing", "v4:routing")]
    [TestCase("fe80::1", "fe80::1")]
    [TestCase("inet6 ::1  prefixlen 128", "inet6 ::1 prefixlen 128")]
    [TestCase("brd ff:ff:ff:ff:ff:ff", "brd ff:ff:ff:ff:ff:ff")]
    [TestCase("ether aa:bb:cc:00:11:22", "ether aa:bb:cc:00:11:22")]
    [TestCase("ID:ab", "ID: ab")]
    [TestCase("inet6 fdab:cd00::1/64", "inet6 fdab:cd00::1/64")]
    [TestCase("Host Name.......: foo", "Host Name: foo")]
    public void TokenizesGluedSeparators(string line, string tokens)
    {
        Assert.That(string.Join(" ", Tokenizer.Tokenize(line).Select(t => t.Text)), Is.EqualTo(tokens));
    }

    [TestCase("Host Name . . . . . . . . . . . . : WS-0042", "Host Name : WS-0042")]
    [TestCase("Physical Address. . . . . . . . . : 02-00-5E-00-00-42", "Physical Address : 02-00-5E-00-00-42")]
    [TestCase("Connection-specific DNS Suffix  . :", "Connection-specific DNS Suffix :")]
    [TestCase("Host Name.......... : WS-0042", "Host Name : WS-0042")]
    [TestCase("Serial No. : 1234", "Serial No. : 1234")]
    [TestCase("Domain . : .", "Domain : .")]
    [TestCase("Version 1.2.3 . Build 4", "Version 1.2.3 . Build 4")]
    [TestCase(": value", ": value")]
    [TestCase("System Name...................... c5508-wlc", "System Name : c5508-wlc")]
    [TestCase("Manufacturer's Name...... Cisco Systems Inc.", "Manufacturer's Name : Cisco Systems Inc.")]
    [TestCase("Loading... done", "Loading... done")]
    [TestCase("1.2.3... 4", "1.2.3... 4")]
    public void DropsDotLeadersBeforeAColon(string line, string tokens)
    {
        Assert.That(string.Join(" ", Tokenizer.Tokenize(line).Select(t => t.Text)), Is.EqualTo(tokens));
    }

    [Test]
    public void OneSampleKeepsLinesThatSayWhatTheyAre()
    {
        const string text = """
            Configuration register on node 0/RSP0/CPU0 is 0x102
            Boot device on node 0/RSP0/CPU0 is disk0:
            219k bytes of non-volatile configuration memory.
            36 Number of requested variables
            0 Number of altered variables
            Table ID: 0x0   RD version: 0
            BGP is operating in STANDALONE mode.
            BGP scan interval 60 secs
            Configured hello time 2, max age 20, forward delay 15
            """;
        SingleSpec spec = SingleInference.Infer(text) ?? throw new AssertionException("no spec");
        ExtractedRecord record = SingleExtractor.Extract(spec, text) ?? throw new AssertionException("no record");

        Assert.That(record["Configuration register on node 0/RSP0/CPU0"], Is.EqualTo("0x102"));
        Assert.That(record["Boot device on node 0/RSP0/CPU0"], Is.EqualTo("disk0"));
        Assert.That(record["Number of requested variables"], Is.EqualTo("36"));
        Assert.That(record["Number of altered variables"], Is.EqualTo("0"));
        Assert.That(record["RD version"], Is.EqualTo("0"));
        Assert.That(record["max age"], Is.EqualTo("20"));
        Assert.That(record.Unmatched.Select(i => i + 1), Is.EqualTo(new[] { 8 }), "a line sharing only its first word with a template is not read by it");
    }

    [TestCase("cisco id is --", "cisco id")]
    [TestCase("cisco extended id number is 4", "cisco extended id number")]
    [TestCase("Hardware is DellEth, address is 02:00:5e:60:6a:01", "Hardware")]
    [TestCase("Ethernet1/1 is up", "Ethernet1/1")]
    [TestCase("Link length supported for 9/125um fiber is 10 km", "Link")]
    [TestCase("BGP is operating in STANDALONE mode.", "BGP")]
    public void KeysAStatementByItsSubject(string line, string key)
    {
        Assert.That(TextDocument.Parse(line)[0].Key, Is.EqualTo(key));
    }

    [Test]
    public void FlatRecordsUnderACommandEchoStartAtTheirFirstKey()
    {
        const string text = """
            show fdp neighbors detail
            Device ID: access-1
            Platform: cisco WS-C3560X,  Capabilities: Router, Switch, IGMP
            Holdtime : 141 seconds
            Device ID: core-1
            Platform: N5K-C5548UP,  Capabilities: Switch, IGMP
            Holdtime : 124 seconds
            Device ID: stack-2
            Platform: FCX624S,  Capabilities: Switch
            Holdtime : 173 seconds
            """;
        RecordSpec spec = RecordInference.Infer(text) ?? throw new AssertionException("no spec");
        List<ExtractedRecord> records = RecordExtractor.Extract(spec, text);

        Assert.That(spec.Anchor, Is.EqualTo("Device ID:"));
        Assert.That(records.Select(r => r["Capabilities"]), Is.EqualTo(new[] { "Router, Switch, IGMP", "Switch, IGMP", "Switch" }));
    }

    [Test]
    public void LeadingRecordsUnlikeTheRestAreKeptWhenTheyShareAKey()
    {
        const string text = """
            ip addr show
            1: lo: <LOOPBACK,UP> mtu 65536 qdisc noqueue state UNKNOWN
                link/loopback 00:00:00:00:00:00 brd 00:00:00:00:00:00
                inet 127.0.0.1/8 scope host lo
                inet6 ::1/128 scope host
            2: eth0: <BROADCAST,UP> mtu 1500 qdisc mq state UP
                link/ether 02:00:5e:00:00:10 brd ff:ff:ff:ff:ff:ff
                inet 192.0.2.10/24 brd 192.0.2.255 scope global eth0
            3: eth1: <BROADCAST,UP> mtu 1500 qdisc mq state UP
                link/ether 02:00:5e:00:00:11 brd ff:ff:ff:ff:ff:ff
                inet 192.0.2.11/24 brd 192.0.2.255 scope global eth1
            """;
        RecordSpec spec = RecordInference.Infer(text) ?? throw new AssertionException("no spec");

        Assert.That(RecordExtractor.Extract(spec, text).Select(r => r["inet"]), Is.EqualTo(new[] { "127.0.0.1/8", "192.0.2.10/24", "192.0.2.11/24" }));
    }

    [Test]
    public void AQuotedNameInAKeyVaries()
    {
        Assert.That(
            TextDocument.Parse("Traffic Statistics for \"dmz-a\":")[0].Key,
            Is.EqualTo(TextDocument.Parse("Traffic Statistics for \"dmz-b\":")[0].Key)
        );
    }

    [Test]
    public void AWordSetOffByWideSpaceStartsTheNextPair()
    {
        LineTemplate head = LineTemplate.Infer(
            RecordLines.Head,
            [Tokenizer.Tokenize("eth0: flags=4163<UP,RUNNING>  mtu 1500"), Tokenizer.Tokenize("lo: flags=73<UP,LOOPBACK>  mtu 65536")]
        );
        LineTemplate enabled = LineTemplate.Infer("Enabled:", [Tokenizer.Tokenize("Enabled:     no"), Tokenizer.Tokenize("Enabled:     no")]);

        List<ExtractedField> fields = [];
        Assert.That(head.TryMatch("eth1: flags=4099<UP>  mtu 9000", fields), Is.True);
        Assert.That(enabled.TryMatch("Enabled:     yes", fields), Is.True);
        Assert.That(fields.Select(f => f.Name + "=" + f.Value), Is.EqualTo(new[] { "Name=eth1", "flags=4099<UP>", "mtu=9000", "Enabled=yes" }));
    }

    [Test]
    public void HeadsEndingInAColonAreANameAfterTheWordsTheyShare()
    {
        LineTemplate adapters = LineTemplate.InferVariants(
            RecordLines.Head,
            [Tokenizer.Tokenize("Ethernet adapter Ethernet:"), Tokenizer.Tokenize("Ethernet adapter Ethernet 2:"), Tokenizer.Tokenize("Wireless LAN adapter Wi-Fi:")]
        ).Single();
        LineTemplate dtp = LineTemplate.InferVariants(
            RecordLines.Head,
            [Tokenizer.Tokenize("DTP information for Gi1/0/1:"), Tokenizer.Tokenize("DTP information for Te1/1/1:")]
        ).Single();

        List<ExtractedField> fields = [];
        Assert.That(adapters.TryMatch("Ethernet adapter Ethernet 2:", fields), Is.True);
        Assert.That(dtp.TryMatch("DTP information for Gi1/0/2:", fields), Is.True);
        Assert.That(fields.Select(f => f.Value), Is.EqualTo(new[] { "Ethernet adapter Ethernet 2", "Gi1/0/2" }));
    }

    [Test]
    public void MatchToleratesAMissingLiteral()
    {
        LineTemplate template = LineTemplate.Infer(
            "Platform: Capabilities:",
            [Tokenizer.Tokenize("Platform: cisco WS-C3850 , Capabilities: Switch IGMP"), Tokenizer.Tokenize("Platform: cisco ISR4331 , Capabilities: Router")]
        );

        // A later release spells the second key differently, so one literal of three is missing.
        string line = "Platform: cisco C9300 , Capability: Switch";
        List<ExtractedField> fields = [];

        Assert.That(template.TryMatch(line, fields), Is.True);
        Assert.That(fields.Select(f => f.Value), Is.EqualTo(new[] { "cisco C9300", "Capability: Switch" }));
    }

    private const string FlatInterfaces = """
        TenGigabitEthernet 0/1 is up, line protocol is up
        Hardware is DellEth, address is f8:b1:56:49:6a:52
        Internet address is not set
        MTU 12000 bytes, IP MTU 11982 bytes
        TenGigabitEthernet 0/2 is down, line protocol is down
        Hardware is DellEth, address is f8:b1:56:49:6a:53
        Internet address is not set
        MTU 2500 bytes, IP MTU 2482 bytes
        Port-channel 1 is up, line protocol is up
        Hardware is DellEth, address is f8:b1:56:49:6a:54
        Internet address is 10.1.1.1/24
        MTU 9000 bytes, IP MTU 8982 bytes
        """;

    [Test]
    public void FindsRecordsByARepeatedPhraseWhenNothingElseSetsThemApart()
    {
        RecordSpec spec = RecordInference.Infer(FlatInterfaces) ?? throw new AssertionException("no spec");
        List<ExtractedRecord> records = RecordExtractor.Extract(spec, FlatInterfaces);

        Assert.That(spec.Boundary, Is.EqualTo(RecordBoundary.Phrase));
        Assert.That(spec.Anchor, Is.EqualTo("line protocol is"));
        Assert.That(records.Select(r => Field(r, "Name")), Is.EqualTo(new[] { "TenGigabitEthernet 0/1", "TenGigabitEthernet 0/2", "Port-channel 1" }));
        Assert.That(records.Select(r => Field(r, "IP MTU")), Is.EqualTo(new[] { "11982", "2482", "8982" }));
    }

    [Test]
    public void ExtractOrInferReadsAFormatNoSpecHasSeen()
    {
        RecordSpec cdp = RecordInference.Infer(Cdp) ?? throw new AssertionException("no spec");

        (RecordSpec Spec, List<ExtractedRecord> Records)? read = RecordExtractor.ExtractOrInfer([cdp], FlatInterfaces);

        Assert.That(read, Is.Not.Null);
        Assert.That(read?.Spec, Is.Not.SameAs(cdp));
        Assert.That(read?.Records.Select(r => Field(r, "Name")), Is.EqualTo(new[] { "TenGigabitEthernet 0/1", "TenGigabitEthernet 0/2", "Port-channel 1" }));
    }

    [Test]
    public void ExtractOrInferKeepsAKnownSpecThatFits()
    {
        RecordSpec cdp = RecordInference.Infer(Cdp) ?? throw new AssertionException("no spec");

        (RecordSpec Spec, List<ExtractedRecord> Records)? read = RecordExtractor.ExtractOrInfer([cdp], Cdp);

        Assert.That(read?.Records, Has.Count.EqualTo(2));
    }

    [Test]
    public void GroupsSamplesByFormat()
    {
        string ios = "Device ID: a\nPlatform: x\nHoldtime : 1 sec\n";
        string ios2 = "Device ID: b\nPlatform: y\nHoldtime : 2 sec\n";
        string nxos = "Chassis id: c\nPort id: d\nSystem Name: e\n";
        string nxos2 = "Chassis id: f\nPort id: g\nSystem Name: h\n";

        List<List<int>> groups = SampleFormats.Group([ios, nxos, ios2, nxos2, "% Invalid input"]);

        Assert.That(groups, Has.Count.EqualTo(3));
        Assert.That(groups, Has.One.EqualTo(new[] { 0, 2 }));
        Assert.That(groups, Has.One.EqualTo(new[] { 1, 3 }));
    }

    [TestCase("1500", true)]
    [TestCase("0/1", true)]
    [TestCase("10.0.0.1", true)]
    [TestCase("04:00:00", true)]
    [TestCase("64-byte", false)]
    [TestCase("1y34w", false)]
    [TestCase("Gi1/0/1", false)]
    [TestCase("-", false)]
    public void IsNumberMeansDigitsAndNumberPunctuationOnly(string value, bool expected)
    {
        Assert.That(Cells.IsNumber(value), Is.EqualTo(expected));
    }

    private static string Field(ExtractedRecord record, string name) =>
        record.Fields.FirstOrDefault(f => f.Key == name)?.Value ?? "<missing>";

    [Test]
    public void PairsAKeysParentheticalWithItsValue()
    {
        string text = """
            Local port: 1/1
              Neighbor: 0024.387a.c230, TTL 114 seconds
                + Chassis ID (MAC address): 0024.387a.c200
                + Port ID (MAC address): 0024.387a.c230
                + System name         : "core-1"

            Local port: 1/2
              Neighbor: cc4e.240c.e831, TTL 108 seconds
                + Chassis ID (MAC address): cc4e.240c.e800
                + Port ID (locally assigned): 27
                + System name         : "core-2"
            """;

        RecordSpec spec = RecordInference.Infer(text) ?? throw new AssertionException("no spec");
        List<ExtractedRecord> records = RecordExtractor.Extract(spec, text);
        ExtractedField port = records[1].Fields.Single(f => f.Name.EndsWith("Port ID", System.StringComparison.Ordinal));

        Assert.That(port.Qualifier, Is.EqualTo("locally assigned"));
        Assert.That(port.Value, Is.EqualTo("27"));
        Assert.That(port.Key, Does.EndWith("Port ID (locally assigned)"));
        Assert.That(records[0].Fields.Single(f => f.Name.EndsWith("System name", System.StringComparison.Ordinal)).Value, Is.EqualTo("core-1"));
    }

    [TestCase("\"core-1\"", "core-1")]
    [TestCase("(MAC address)", "MAC address")]
    [TestCase("[ethernet]", "ethernet")]
    [TestCase("(a) (b)", "(a) (b)")]
    [TestCase("plain", "plain")]
    [TestCase("\"", "\"")]
    public void UnwrapStripsOneEnclosingPair(string value, string expected)
    {
        Assert.That(Cells.Unwrap(value), Is.EqualTo(expected));
    }
}
