using JMW.Extensions;

namespace JMW.Templates.Tags;

public class JoinColumn : ColumnBase
{
    private readonly Join parent;
    public override string TagName { get; }
    public const string LEFT_PREFIX = "left_";
    public const string RIGHT_PREFIX = "right_";

    public JoinColumn(string name, Join data)
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