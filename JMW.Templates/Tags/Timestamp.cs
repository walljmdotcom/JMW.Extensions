using System;
using System.Collections.Generic;
using System.Globalization;

namespace JMW.Templates.Tags
{
    public class Timestamp : TagHandlerBase
    {
        public const string TAG = "timestamp";
        public override string TagName { get; } = TAG;
        public override HashSet<string> ALLOWEDPROPS { get; } = [ATTR_FORMAT, ATTR_EXP];
        public const string ATTR_EXP = "exp";
        private const string ATTR_FORMAT = "format";

        public override void Handler(Tag tag, Interpreter interp)
        {
            var format = @"M/d/yyyy hh:mm:ss";

            try
            {
                if (tag.Properties.ContainsKey(ATTR_FORMAT))
                    format = tag.Properties[ATTR_FORMAT];

                var value = DateTime.Now.ToString(format, CultureInfo.InvariantCulture);

                if (tag.Properties.ContainsKey(ATTR_EXP))
                {
                    value = TagHelpers.EvaluateExpression(tag.Properties[ATTR_EXP], [value]).ToString(CultureInfo.InvariantCulture);
                    interp.OutputStream.Write(value);
                }
                else
                    interp.OutputStream.Write(value);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to interpret tag '" + tag.Name + "' with format: '" + format + "'. Error Message: " + ex.Message);
            }
        }
    }
}