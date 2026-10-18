using JMW.Parsing.Compile;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Parsing.Expressions;

public class Or : IExpression
{
    public const string NAME = "or";

    public Stack<IExpression> Expressions = new();

    public Or(Tag t)
    {
        if (t.Value is Stack<Tag> tags)
        {
            foreach (var tag in tags)
            {
                this.Expressions.Push(Base.ToExpression(tag));
            }
        }
    }

    public Or(IEnumerable<IExpression> expressions)
    {
        foreach (var exp in expressions)
        {
            this.Expressions.Push(exp);
        }
    }

    public bool Test(string s)
    {
        foreach (var expression in this.Expressions)
        {
            if (expression.Test(s))
            {
                return true;
            }
        }

        return false;
    }
}