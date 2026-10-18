namespace JMW.TextExtraction.Tests;

[TestFixture]
public class SpecTextTest
{
    private const string Cdp = """
        -------------------------
        Device ID: sw1.example.com
        Entry address(es):
          IP address: 10.0.0.1
        Platform: cisco WS-C3850,  Capabilities: Switch IGMP
        Interface: GigabitEthernet1/0/1,  Port ID (outgoing port): TenGigabitEthernet1/1
        Holdtime : 131 sec

        Version :
        Cisco IOS Software, Version 16.9.4

        -------------------------
        Device ID: rtr2.example.com
        Entry address(es):
          IP address: 10.0.0.2
        Platform: cisco ISR4331,  Capabilities: Router
        Interface: GigabitEthernet1/0/2,  Port ID (outgoing port): GigabitEthernet0/0/0
        Holdtime : 150 sec

        Version :
        Cisco IOS XE Software, Version 17.6.3
        """;

    private const string Status = """
        Port      Name               Status       Vlan
        Gi1/0/1   uplink to core     connected    trunk
        Gi1/0/2                      notconnect   10
        Gi1/0/3   printer            connected    20
        """;

    [Test]
    public void RoundTripsAnInferredSpec()
    {
        DocumentSpec spec = TextExtractor.Infer(Cdp, Status);

        DocumentSpec read = SpecText.Read(SpecText.Write(spec));

        Assert.That(SpecText.Write(read), Is.EqualTo(SpecText.Write(spec)));
        Assert.That(Fields(TextExtractor.Extract(read, Cdp, inferUnknown: false)), Is.EqualTo(Fields(TextExtractor.Extract(spec, Cdp, inferUnknown: false))));
    }

    [Test]
    public void ReadsAHandWrittenSpec()
    {
        string text = """
            # CDP neighbors, written by hand
            records split by rule
              line "Device ID:" => Device ID: {Device ID}
              line "Platform: Capabilities:" => Platform: {Platform} , Capabilities: {Capabilities}
              line "Interface: Port ID :" => Interface: {Local Port} , Port ID {?Port ID qualifier} : {Remote Port}
              text "Version :" => Version : {Software}
            """;

        DocumentSpec spec = SpecText.Read(text);
        ExtractedDocument document = TextExtractor.Extract(spec, Cdp, inferUnknown: false);

        Assert.That(document.Records, Has.Count.EqualTo(2));
        ExtractedRecord first = document.Records[0];
        Assert.That(first["Device ID"], Is.EqualTo("sw1.example.com"));
        Assert.That(first["Local Port"], Is.EqualTo("GigabitEthernet1/0/1"));
        Assert.That(first.Fields.Single(f => f.Name == "Remote Port").Key, Is.EqualTo("Remote Port (outgoing port)"));
        Assert.That(first["Software"], Is.EqualTo("Cisco IOS Software, Version 16.9.4"));
    }

    [Test]
    public void ToleratesCaseSpacingAndComments()
    {
        string text = """

            TABLE   Aligned   HEADER:  Port |Name|  Status | Vlan   # not a comment inside a name list

            # a comment
               Records   Split By   Rule ,  Head
                 LINE   "@head"   =>   {Name}
            """;

        DocumentSpec spec = SpecText.Read(text);

        Assert.That(spec.Tables.Single().Columns, Is.EqualTo(new[] { "Port", "Name", "Status", "Vlan   # not a comment inside a name list" }));
        Assert.That(spec.Records.Single().KeyHead, Is.True);
        Assert.That(spec.Records.Single().Lines.Single().Key, Is.EqualTo("@head"));
    }

    [TestCase("table aligned", 1)]
    [TestCase("records split by sideways", 1)]
    [TestCase("records start at key Device ID:", 1)]
    [TestCase("\n\n  line \"x\" => {x}", 3)]
    [TestCase("records split by rule\n  line \"x\" {x}", 2)]
    [TestCase("records split by rule\n  text \"x\" => x", 2)]
    [TestCase("records split by rule\n  line \"x\" => {x", 2)]
    [TestCase("hello", 1)]
    public void NamesTheLineOfAnError(string text, int line)
    {
        FormatException error = Assert.Throws<FormatException>(() => SpecText.Read(text));

        Assert.That(error.Message, Does.StartWith("line " + line + ":"));
    }

    [Test]
    public void EscapesQuotesBracesAndBars()
    {
        DocumentSpec spec = new(
            [new TableSpec(TableLayout.Aligned, true, ["a|b", "c"])],
            [new RecordSpec(RecordBoundary.Anchor, "say \"hi\"", false, [new LineTemplate("k\\\"", [new Literal("{x}"), new Literal("\\y"), new Slot("v")], false)])]
        );

        DocumentSpec read = SpecText.Read(SpecText.Write(spec));

        Assert.That(read.Tables[0].Columns, Is.EqualTo(new[] { "a|b", "c" }));
        Assert.That(read.Records[0].Anchor, Is.EqualTo("say \"hi\""));
        Assert.That(read.Records[0].Lines[0].Key, Is.EqualTo("k\\\""));
        Assert.That(read.Records[0].Lines[0].Parts, Is.EqualTo(spec.Records[0].Lines[0].Parts));
    }

    private static List<string> Fields(ExtractedDocument document) =>
        document.Records.SelectMany(r => r.Fields).Select(f => f.ToString()).ToList();
}
