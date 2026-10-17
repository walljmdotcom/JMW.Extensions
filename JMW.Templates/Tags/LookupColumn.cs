using JMW.Extensions;

namespace JMW.Templates.Tags;

public class LookupColumn : ColumnBase
{
    private readonly Lookup parent;
    public override string TagName { get; }

    public LookupColumn(string name, Lookup data)
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