using System.Text.Json;

namespace JMW.TextExtraction.Tests;

[TestFixture]
public class TextExtractorTest
{
    private const string Mixed = """
        Interface              IP-Address      OK? Method Status                Protocol
        GigabitEthernet0/0     10.0.0.1        YES NVRAM  up                    up
        GigabitEthernet0/1     unassigned      YES NVRAM  administratively down down
        Loopback0              192.168.255.1   YES NVRAM  up                    up

        Neighbor: 10.1.1.1
          State: Full
          Uptime: 3d

        Neighbor: 10.1.1.2
          State: Init
          Uptime: 4m
        """;

    [Test]
    public void ReadsTablesThenRecordsFromWhatIsLeft()
    {
        DocumentSpec spec = TextExtractor.Infer(Mixed);
        ExtractedDocument document = TextExtractor.Extract(spec, Mixed);

        ExtractedTable table = document.Tables.Single();
        Assert.That(table.Rows, Has.Count.EqualTo(3));
        Assert.That(table.Rows[1]["Status"], Is.EqualTo("administratively down"));
        Assert.That(document.Records.Select(r => r["Neighbor"]), Is.EqualTo(new[] { "10.1.1.1", "10.1.1.2" }));
        Assert.That(document.Records[1]["State"], Is.EqualTo("Init"));
        Assert.That(document.Unclaimed, Is.Empty);
    }

    [Test]
    public void TableColumnsTakeTheSpecsNamesRegardlessOfCase()
    {
        TableSpec spec = new(TableLayout.Aligned, true, ["INTERFACE", "IP-Address", "OK?", "Method", "Status", "Protocol"]);

        ExtractedTable table = TableExtractor.Extract([spec], Mixed).Single();

        Assert.That(table.Rows[0]["INTERFACE"], Is.EqualTo("GigabitEthernet0/0"));
        Assert.That(table.Rows[0].Fields[0].Name, Is.EqualTo("INTERFACE"));
    }

    [Test]
    public void ATableThatFitsNoSpecIsSkippedUnlessUnknownTablesAreWanted()
    {
        TableSpec other = new(TableLayout.Aligned, true, ["Vlan", "Mac Address", "Type", "Ports"]);

        Assert.That(TableExtractor.Extract([other], Mixed), Is.Empty);
        Assert.That(TableExtractor.Extract([other], Mixed, includeUnknown: true).Single().Spec.Columns[0], Is.EqualTo("Interface"));
    }

    [Test]
    public void ReportsLinesNothingReads()
    {
        string text = Mixed + "\n\n% Invalid input detected at '^' marker.\n";

        ExtractedDocument document = TextExtractor.Extract(TextExtractor.Infer(Mixed), text);

        Assert.That(document.Unclaimed, Has.Count.EqualTo(1));
        Assert.That(text.Split('\n')[document.Unclaimed[0]], Does.StartWith("% Invalid"));
    }

    [Test]
    public void TableRowsKnowTheirLines()
    {
        ExtractedTable table = TableExtractor.Extract([], Mixed, includeUnknown: true).Single();

        Assert.That(table.Rows.Select(r => r.FirstLine), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test, CancelAfter(10_000)]
    public void AVeryLongLineEndsATableAndDoesNotStallDetection()
    {
        string text = new string('x', 500_000) + "\n" + Mixed;

        ExtractedDocument document = TextExtractor.Extract(TextExtractor.Infer(text), text);

        Assert.That(document.Tables.Single().Rows, Has.Count.EqualTo(3));
    }

    [Test]
    public void ARecordBecomesADictionaryWithRepeatsAsArrays()
    {
        ExtractedRecord record = new(0, 3, [
            new ExtractedField("Address", "10.0.0.1"),
            new ExtractedField("MTU", "1500", Unit: "bytes"),
            new ExtractedField("Address", "10.0.0.2", Qualifier: "secondary"),
        ], []);

        IReadOnlyDictionary<string, object?> fields = record.ToDictionary();

        Assert.That(fields.Keys, Is.EqualTo(new[] { "Address", "Address qualifier", "MTU", "MTU unit" }));
        Assert.That(fields["Address"], Is.EqualTo(new[] { "10.0.0.1", "10.0.0.2" }));
        Assert.That(fields["Address qualifier"], Is.EqualTo(new[] { null, "secondary" }));
        Assert.That(fields["MTU"], Is.EqualTo("1500"));
        Assert.That(fields["MTU unit"], Is.EqualTo("bytes"));
    }

    [Test]
    public void ADocumentBecomesJson()
    {
        ExtractedDocument document = TextExtractor.Extract(TextExtractor.Infer(Mixed), Mixed);

        using JsonDocument json = JsonDocument.Parse(document.ToJson());
        JsonElement root = json.RootElement;

        Assert.That(root.GetProperty("tables")[0].GetArrayLength(), Is.EqualTo(3));
        Assert.That(root.GetProperty("tables")[0][1].GetProperty("Status").GetString(), Is.EqualTo("administratively down"));
        Assert.That(root.GetProperty("records")[1].GetProperty("State").GetString(), Is.EqualTo("Init"));
        Assert.That(root.GetProperty("single").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(root.GetProperty("unclaimed").GetArrayLength(), Is.Zero);
    }
}
