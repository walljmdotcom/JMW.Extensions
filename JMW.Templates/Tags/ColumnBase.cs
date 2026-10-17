using System.Collections.Generic;

namespace JMW.Templates.Tags
{
    /// <summary>
    /// This is the base class for column handlers.  This is not intended to be used by itself,
    /// as it has now way to get data from anything.
    /// </summary>
    public abstract class ColumnBase : TagHandlerBase
    {
        public static readonly HashSet<string> ALLOWED = [ATTR_EXP, ATTR_OCT];

        public override HashSet<string> ALLOWEDPROPS { get; } = ALLOWED;

        public const string ATTR_EXP = "exp";
        public const string ATTR_OCT = "oct";

        protected static void HandleColumn(Tag token, Interpreter interp, string data)
        {
            if (token.Properties.TryGetValue(ATTR_EXP, out var attrExp))
            {
                var arguments = new List<string>();
                if (token.Properties.TryGetValue(ATTR_OCT, out var attrOct))
                {
                    arguments.Add(TagHelpers.RetrieveOctet(data, attrOct));
                    var octet_result = TagHelpers.EvaluateArithmeticExpression(attrExp, arguments);
                    interp.OutputStream.Write(TagHelpers.ReplaceOctet(data, attrOct, octet_result));
                }
                else
                {
                    arguments.Add(data);
                    interp.OutputStream.Write(TagHelpers.EvaluateExpression(attrExp, arguments));
                }
            }
            else
            {
                interp.OutputStream.Write(data);
            }
        }
    }
}