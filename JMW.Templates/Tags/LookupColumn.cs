using JMW.Extensions;

namespace JMW.Templates.Tags;

public class LookupColumn : ColumnBase
{
    private readonly Lookup parent;
    private readonly int columnIndex = -1;
    public override string TagName { get; }

    public LookupColumn(string name, Lookup data)
    {
        TagName = name;
        parent = data;
    }

    internal LookupColumn(string name, Lookup data, int column_index) : this(name, data)
    {
        columnIndex = column_index;
    }

    public override void Handler(Tag token, Interpreter interp)
    {
        var data = columnIndex >= 0
            ? parent.RetrieveColumnValue(columnIndex)
            : parent.RetrieveColumnValue(TagName.ParseAfterLastIndexOf_PlusLength(":"));

        HandleColumn(token, interp, data);
    }
}
