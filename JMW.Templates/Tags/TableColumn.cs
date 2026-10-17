using JMW.Extensions;

namespace JMW.Templates.Tags;

public class TableColumn : ColumnBase
{
    private readonly Table parent;
    public override string TagName { get; }

    public TableColumn(string name, Table data)
    {
        TagName = name;
        parent = data;
    }

    public override void Handler(Tag token, Interpreter interp)
    {
        var column_name = TagName.ParseAfterLastIndexOf_PlusLength(":");
        var data = parent.RetrieveColumnValue(column_name);

        HandleColumn(token, interp, data);
    }
}