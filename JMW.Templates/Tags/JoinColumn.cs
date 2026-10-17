using JMW.Extensions;

namespace JMW.Templates.Tags;

public class JoinColumn : ColumnBase
{
    private readonly Join parent;
    private readonly int columnIndex = -1;
    public override string TagName { get; }
    public const string LEFT_PREFIX = "left_";
    public const string RIGHT_PREFIX = "right_";

    public JoinColumn(string name, Join data)
    {
        TagName = name;
        parent = data;
    }

    internal JoinColumn(string name, Join data, int column_index) : this(name, data)
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
