using JMW.Extensions;

namespace JMW.Templates.Tags;

public class TableColumn : ColumnBase
{
    private readonly Table parent;
    private readonly int columnIndex = -1;
    public override string TagName { get; }

    public TableColumn(string name, Table data)
    {
        TagName = name;
        parent = data;
    }

    internal TableColumn(string name, Table data, int column_index) : this(name, data)
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
