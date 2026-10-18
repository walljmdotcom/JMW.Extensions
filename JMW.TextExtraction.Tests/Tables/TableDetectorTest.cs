namespace JMW.TextExtraction.Tests.Tables;

[TestFixture]
public class TableDetectorTest
{
    private string Sample(string name) => SampleFiles.Read(name);

    [TestCase("cisco-ipbrief.txt", TableLayout.Aligned, "Interface|IP-Address|OK?|Method|Status|Protocol", 4)]
    [TestCase("cisco-cdp.txt", TableLayout.Aligned, "Device ID|Local Intrfce|Holdtme|Capability|Platform|Port ID", 4)]
    [TestCase("cisco-mac.txt", TableLayout.Ruled, "Vlan|Mac Address|Type|Ports", 3)]
    [TestCase("tasklist.txt", TableLayout.Ruled, "Image Name|PID|Session Name|Session#|Mem Usage", 3)]
    [TestCase("mysql.txt", TableLayout.Ruled, "id|name|state", 3)]
    [TestCase("kubectl.txt", TableLayout.Aligned, "NAME|READY|STATUS|RESTARTS|AGE", 3)]
    public void FindsOneTable(string sample, TableLayout layout, string columns, int rows)
    {
        DetectedTable table = TableDetector.Detect(this.Sample(sample)).Single();

        Assert.That(table.Layout, Is.EqualTo(layout));
        Assert.That(string.Join("|", table.Columns.Select(c => c.Name)), Is.EqualTo(columns));
        Assert.That(table.Rows, Has.Count.EqualTo(rows));
    }

    [TestCase("cisco-ipbrief.txt", 1, "GigabitEthernet0/1|unassigned|YES|NVRAM|administratively down|down")]
    [TestCase("cisco-cdp.txt", 0, "core-sw01.example.com|Gig 1/0/1|150|S I|WS-C3850|Gig 1/0/24")]
    [TestCase("cisco-cdp.txt", 3, "phone-1001|Fas 0/5|128|H P M|IP Phone|Port 1")]
    [TestCase("cisco-mac.txt", 2, "20|00aa.bbcc.ddee|STATIC|Gi1/0/3")]
    [TestCase("tasklist.txt", 0, "System Idle Process|0|Services|0|8 K")]
    [TestCase("mysql.txt", 0, "1|alpha one|active")]
    [TestCase("kubectl.txt", 2, "worker-5b7f9c-qq7x2|0/1|CrashLoopBackOff|41 (2m3s ago)|3h12m")]
    public void SlicesRow(string sample, int row, string cells)
    {
        DetectedTable table = TableDetector.Detect(this.Sample(sample)).Single();

        Assert.That(string.Join("|", table.Rows[row]), Is.EqualTo(cells));
    }

    [Test]
    public void EmptyNameCellsStayInTheirColumn()
    {
        DetectedTable table = TableDetector.Detect(this.Sample("cisco-status.txt")).Single();

        Assert.That(table.Rows[1][0], Is.EqualTo("Gi1/0/2"));
        Assert.That(table.Rows[1][1], Is.Empty);
        Assert.That(table.Rows[1][2], Is.EqualTo("notconnect"));
    }

    [Test]
    public void FooterEndsTable()
    {
        DetectedTable table = TableDetector.Detect(this.Sample("cisco-mac.txt")).Single();

        Assert.That(table.LastLine, Is.EqualTo(7));
    }

    [TestCase("cisco-version.txt")]
    [TestCase("")]
    [TestCase("one line")]
    [TestCase("Key: value\nOther key: other value\nThird: 3\n")]
    public void FindsNoTableInProse(string sampleOrText)
    {
        string text = sampleOrText.EndsWith(".txt", System.StringComparison.Ordinal) ? this.Sample(sampleOrText) : sampleOrText;

        Assert.That(TableDetector.Detect(text), Is.Empty);
    }

    [Test]
    public void WordsLayoutFollowsRowsThatDriftOutOfAlignment()
    {
        string text = """
            USER               PID  %CPU %MEM      VSZ    RSS   TT  STAT STARTED      TIME COMMAND
            alice            29582  21.0  0.5 416745136 500208 s002  S+    9:21AM   0:10.70 claude
            alice            46941   5.0  1.1 1928302288 1094576   ??  S    12:14PM  24:19.24 /Applications/Visual Studio Code.app/Code
            root               573  14.8  0.0 427007984  19664   ??  Ss    7Oct26  11:08.04 /usr/libexec/opendirectoryd
            alice            81537   4.1  0.3 1924879696 303344   ??  S    Thu12PM   8:08.88 /bin/zsh -c true
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Layout, Is.EqualTo(TableLayout.Words));
        Assert.That(table.Rows, Has.Count.EqualTo(4));
        Assert.That(table.Rows[1][4], Is.EqualTo("1928302288"));
        Assert.That(table.Rows[1][10], Is.EqualTo("/Applications/Visual Studio Code.app/Code"));
    }

    [Test]
    public void TitleLineAboveHeaderIsNotTheHeader()
    {
        string text = """
            Internet:
            Destination        Gateway            Flags               Netif Expire
            default            10.0.0.1           UGScg                 en0
            10.0.0.1           0:0:c:7:ac:b5      UHLWIir               en0   1179
            10.0.0.255         ff:ff:ff:ff:ff:ff  UHLWbI                en0      !
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.HeaderLine, Is.EqualTo(1));
        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Destination", "Gateway", "Flags", "Netif", "Expire" }));
        Assert.That(table.Rows[1][4], Is.EqualTo("1179"));
    }

    [Test]
    public void PaddedHeaderNamesSplitValuesTruncatedIntoTheNextColumn()
    {
        string text = """
            Device ID           Local Intf     Hold-time  Capability      Port ID
            SEP5067AE258397.corpGi1/0/5        180        B,T             5067AE258397:P1
            sw2                 Gi1/0/8        120        R               Gi0/1
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Device ID", "Local Intf", "Hold-time", "Capability", "Port ID" }));
        Assert.That(table.Rows[0][0], Is.EqualTo("SEP5067AE258397.corp"));
        Assert.That(table.Rows[0][1], Is.EqualTo("Gi1/0/5"));
    }

    [Test]
    public void HeaderWordOverNoDataJoinsTheWordItIsSingleSpacedTo()
    {
        string text = """
            Protocol  Address          Age (min)  Hardware Addr   Type   Interface
            Internet  10.1.2.1                -   0015.c612.36cc  ARPA   Vlan666
            Internet  10.1.2.7               12   0015.c612.4411  ARPA   Vlan666
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Protocol", "Address", "Age (min)", "Hardware Addr", "Type", "Interface" }));
        Assert.That(table.Rows[1][2], Is.EqualTo("12"));
    }

    [Test]
    public void PaddedHeaderNameOverNoDataIsAnEmptyColumn()
    {
        string text = """
            Port      Reason       Status
            Gi1/0/1                up
            Gi1/0/2                down
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Port", "Reason", "Status" }));
        Assert.That(table.Rows[1], Is.EqualTo(new[] { "Gi1/0/2", "", "down" }));
    }

    [Test]
    public void OneRowUnderAPaddedHeaderIsATable()
    {
        string text = """
            Port        Vlans allowed on trunk
            Te1/1/1     2-999,1005-4094

            Port        Vlans allowed and active in management domain
            Te1/1/1     80,110,120
            """;

        IReadOnlyList<DetectedTable> tables = TableDetector.Detect(text);

        Assert.That(tables, Has.Count.EqualTo(2));
        Assert.That(tables[1].Columns[1].Name, Is.EqualTo("Vlans allowed and active in management domain"));
        Assert.That(tables[1].Rows.Single(), Is.EqualTo(new[] { "Te1/1/1", "80,110,120" }));
    }

    [Test]
    public void SecondHeaderLineJoinsTheNames()
    {
        string text = """
            Address          Interface                Ver/   Nbr    Query  DR     DR
                                                      Mode   Count  Intvl  Prior
            10.1.1.1         Vlan10                   v2/S   1      30     1      10.1.1.2
            10.1.2.1         Vlan20                   v2/S   0      30     1      10.1.2.1
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Address", "Interface", "Ver/ Mode", "Nbr Count", "Query Intvl", "DR Prior", "DR" }));
        Assert.That(table.Rows, Has.Count.EqualTo(2));
    }

    [Test]
    public void LegendBelowRowsEndsTable()
    {
        string text = """
              address         ref clock       st   when   poll reach  delay  offset   disp
             ~10.60.0.9       .TIME.          16      -     64     0  0.000   0.000 15937.
             * sys.peer, # selected, + candidate, - outlyer, x falseticker, ~ configured
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns[0].Name, Is.EqualTo("Flag"));
        Assert.That(table.Rows.Single()[0], Is.EqualTo("~"));
        Assert.That(table.Rows.Single()[1], Is.EqualTo("10.60.0.9"));
        Assert.That(table.Rows.Single()[9], Is.EqualTo("15937."));
    }

    [Test]
    public void TimestampLineIsNotAHeader()
    {
        string text = """
            Wed Dec  7 12:32:55.933 UTC
            BGP router identifier 172.23.255.22, local AS number 65000
            """;

        Assert.That(TableDetector.Detect(text), Is.Empty);
    }

    [Test]
    public void RepeatedPhrasingReadsAsHeaderlessWords()
    {
        string text = """
            ? (192.168.1.1) at 74:24:9f:b1:8d:e0 on en0 ifscope [ethernet]
            ? (192.168.1.23) at 9c:14:63:d9:c6:83 on en0 ifscope [ethernet]
            ? (192.168.1.24) at 9c:14:63:d9:ca:c0 on en0 ifscope [ethernet]
            ? (192.168.1.25) at 9c:14:63:d9:ca:c1 on en0 ifscope [ethernet]
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Layout, Is.EqualTo(TableLayout.Words));
        Assert.That(table.HeaderLine, Is.Null);
        Assert.That(table.Rows[1][1], Is.EqualTo("192.168.1.23"));
        Assert.That(table.Rows[1][3], Is.EqualTo("9c:14:63:d9:c6:83"));
    }

    [Test]
    public void HeaderlessRowsWithValuesInFixedPositionsAreATable()
    {
        string text = """
            show arp
                mgmt 192.168.42.2 10f3.1160.435d 0
                mgmt 192.168.42.192 54e0.32f7.5b3c 0
                inside 10.1.1.1 0000.0c07.ac0a 12
                inside 10.1.1.5 3c61.045d.0e81 47
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Rows, Has.Count.EqualTo(4));
        Assert.That(table.Rows[2], Is.EqualTo(new[] { "inside", "10.1.1.1", "0000.0c07.ac0a", "12" }));
    }

    [TestCase("""
        System returned to ROM by power-on
        System image file is on flash memory
        System restarted at noon on Monday
        """)]
    [TestCase("""
        alpha beta gamma
        delta epsilon zeta
        eta theta iota
        """)]
    public void ProseWithEqualWordCountsIsNotAHeaderlessTable(string text)
    {
        Assert.That(TableDetector.Detect(text), Is.Empty);
    }

    [Test]
    public void IndentedRowsCarryTheirOuterRowAcrossBlankLines()
    {
        const string text = """
            Instance             Type
                     Primary RIB                                     Active/holddown/hidden
            master               forwarding
                     inet.0                                          38/0/0
                     inet.3                                          7/0/0

            __master.anon__      forwarding

            red                  vrf
                     red.inet.0                                      9/0/2
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Instance", "Type", "Primary RIB", "Active/holddown/hidden" }));
        Assert.That(table.Rows.Select(r => string.Join("|", r)), Is.EqualTo(new[]
        {
            "master|forwarding|inet.0|38/0/0",
            "master|forwarding|inet.3|7/0/0",
            "__master.anon__|forwarding||",
            "red|vrf|red.inet.0|9/0/2",
        }));
    }

    [Test]
    public void ATwoLineHeaderOverOneKindOfRowIsNotNested()
    {
        const string text = """
            Port          Native  Status        Port
                          Vlan                  Channel
            Eth1/1        1       trunking      Po100
            Eth1/2        1       trunking      Po100
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Port", "Native Vlan", "Status", "Port Channel" }));
    }

    [Test]
    public void AnEchoAboveALongTableDoesNotHideIt()
    {
        string rows = string.Join("\n", Enumerable.Range(1, 60).Select(i => $"Gi1/0/{i,-5}  connected    {i,-4}  a-full  a-1000  10/100/1000BaseTX"));
        string text = "sw1#show interfaces status\nPort       Status       Vlan  Duplex  Speed   Type\n" + rows;

        Assert.That(TableDetector.Detect(text).Single().Rows, Has.Count.EqualTo(60));
    }

    [Test, CancelAfter(10_000)]
    public void AVeryLongLineOfWordsDoesNotStallInference()
    {
        string text = "Description: " + string.Join(" ", Enumerable.Repeat("word", 100_000)) + "\nName: a\nName: b\n";

        Assert.That(() => TextExtractor.Infer(text), Throws.Nothing);
    }

    [Test]
    public void AHeaderWinsOverWordsThatAFewRowsLineUp()
    {
        const string text = """
              Index  Lo Limit(C)  Temp(degC)  Hi Limit(C)  Location
              1      0            42          74           Main board HSBE transistor temperature
              2      0            34          57           Main board HSBE IC temperature
              3      0            24          41           Main board inlet transistor temperature
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Columns.Select(c => c.Name), Is.EqualTo(new[] { "Index", "Lo Limit(C)", "Temp(degC)", "Hi Limit(C)", "Location" }));
        Assert.That(table.Rows[1][4], Is.EqualTo("Main board HSBE IC temperature"));
    }

    [Test]
    public void RowLabelsUnderNoNameAreAColumn()
    {
        const string text = """
                           total        used        free
            Mem:           15876        4211        6024
            Swap:           2047           0        2047
            """;

        DetectedTable table = TableDetector.Detect(text).Single();

        Assert.That(table.Rows.Select(r => r[0]), Is.EqualTo(new[] { "Mem:", "Swap:" }));
        Assert.That(table.Rows[0][1], Is.EqualTo("15876"));
    }
}
