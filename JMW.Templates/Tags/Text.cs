using System;
using System.Collections.Generic;

namespace JMW.Templates.Tags
{
    public class Text : TagHandlerBase
    {
        public override string TagName { get; } = "text";
        public override HashSet<string> ALLOWEDPROPS { get; } = [];

        public override void Handler(Tag token, Interpreter interp)
        {
            interp.OutputStream.Write(token.TokenText);
        }
    }
}