using System.Collections.Generic;

namespace JMW.Networking.Parsers.JunosConfig;

public class Tag
{
    public TagTypes TagType { get; set; } = TagTypes.Object;

    private string _Name = string.Empty;
    public string Name { get { return _Name; } set { _Name = value.Trim(); } }

    // Most tags are words and properties with neither, so both are created on first use.
    public Dictionary<string, Tag> Properties { get => field ??= []; set; }

    public string Value { get; set; } = string.Empty;

    public Stack<Tag> Children { get => field ??= new Stack<Tag>(); set; }

    public override string ToString()
    {
        return Name + ":" + TagType;
    }
}
