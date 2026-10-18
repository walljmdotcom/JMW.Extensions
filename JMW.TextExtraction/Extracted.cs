using System.Text.Json.Nodes;

namespace JMW.TextExtraction;

/// <summary>
/// One value read from text. <see cref="Name"/> is the field as the spec knows it, the same in every record.
/// <see cref="Qualifier"/> is the parenthetical the text put on the key for this value, if any, so
/// <c>Chassis ID (MAC address): 0024.387a.c200</c> reads as name <c>Chassis ID</c>, qualifier <c>MAC address</c>.
/// </summary>
/// <param name="Unit">The unit written after a number, as <c>bytes</c> in <c>MTU 1500 bytes</c>, if any.</param>
public sealed record ExtractedField(string Name, string Value, string? Qualifier = null, string? Unit = null)
{
    /// <summary>The name with its qualifier, as the text wrote it: <c>Chassis ID (MAC address)</c>.</summary>
    public string Key => this.Qualifier is null ? this.Name : this.Name + " (" + this.Qualifier + ")";

    public override string ToString() => this.Key + " = " + this.Value + (this.Unit is null ? "" : " [" + this.Unit + "]");
}

/// <summary>A record or a table row, as the list of values read from it.</summary>
/// <param name="FirstLine">Zero-based index of the record's first line.</param>
/// <param name="LastLine">Zero-based index of the record's last line.</param>
/// <param name="Unmatched">Lines inside the record that no template in the spec accounts for.</param>
public sealed record ExtractedRecord(int FirstLine, int LastLine, IReadOnlyList<ExtractedField> Fields, IReadOnlyList<int> Unmatched)
{
    /// <summary>The first value of the named field, or null when the record has none.</summary>
    public string? this[string name] =>
        this.Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;

    /// <summary>
    /// The fields by name, in the order they first appear. A name read once maps to its value as a string; a name
    /// read more than once, as several addresses on one interface, maps to a string array in the order read. A
    /// qualifier or unit goes under <c>"&lt;name&gt; qualifier"</c> or <c>"&lt;name&gt; unit"</c> in the same
    /// shape, with null where a repeat had none.
    /// </summary>
    public IReadOnlyDictionary<string, object?> ToDictionary()
    {
        Dictionary<string, List<ExtractedField>> byName = new(StringComparer.OrdinalIgnoreCase);
        foreach (ExtractedField field in this.Fields)
        {
            (byName.TryGetValue(field.Name, out List<ExtractedField>? list) ? list : byName[field.Name] = []).Add(field);
        }

        Dictionary<string, object?> result = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string name, List<ExtractedField> fields) in byName)
        {
            result[name] = Shape(fields, static f => f.Value);
            if (fields.Any(static f => f.Qualifier is not null))
            {
                result.TryAdd(name + " qualifier", Shape(fields, static f => f.Qualifier));
            }

            if (fields.Any(static f => f.Unit is not null))
            {
                result.TryAdd(name + " unit", Shape(fields, static f => f.Unit));
            }
        }

        return result;
    }

    /// <summary>The fields as a JSON object, shaped as <see cref="ToDictionary"/> shapes them.</summary>
    public JsonObject ToJson()
    {
        JsonObject json = [];
        foreach ((string name, object? value) in this.ToDictionary())
        {
            json[name] = value switch
            {
                string s => JsonValue.Create(s),
                string?[] list => new JsonArray([.. list.Select(static s => (JsonNode?)JsonValue.Create(s))]),
                _ => null,
            };
        }

        return json;
    }

    private static object? Shape(List<ExtractedField> fields, Func<ExtractedField, string?> part) =>
        fields.Count == 1 ? part(fields[0]) : fields.Select(part).ToArray();
}
