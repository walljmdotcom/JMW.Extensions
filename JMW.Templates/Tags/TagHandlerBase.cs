using System.Collections.Generic;

namespace JMW.Template.Tags;

public abstract class TagHandlerBase : ITagHandler
{
    public abstract string TagName { get; }
    public abstract HashSet<string> ALLOWEDPROPS { get; }

    public abstract void Handler(Tag token, Interpreter interp);

    public virtual void Validate(Tag tag, Token token)
    {
        TagHelpers.CheckAllowedAttributes(tag, ALLOWEDPROPS, token);
    }
}