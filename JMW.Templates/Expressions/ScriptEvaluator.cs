using System;
using System.Collections.Generic;

namespace JMW.Templates.Expressions;

internal static class ScriptEvaluator
{
    /// <summary>
    /// Evaluates a template expression with <paramref name="arguments"/> bound to <c>x1</c>, <c>x2</c>, and so on.
    /// Argument text that reads as a number is bound as a number.
    /// </summary>
    /// <exception cref="ExpressionException">The expression is malformed or fails at run time.</exception>
    /// <exception cref="ArgumentException">The expression uses an <c>xN</c> with no matching argument.</exception>
    public static ScriptValue Evaluate(string expression, IReadOnlyList<string> arguments)
    {
        ExpressionNode program = ExpressionParser.Parse(expression, out int highestArgument);
        if (highestArgument > arguments.Count)
        {
            throw new ArgumentException(
                "The list of arguments provided in:\n" + string.Join(",", arguments) +
                "\n is greater that the number of variables listed in the expression: " + expression + "\n" +
                "Variables must be specified in the format: x1, x2, x3 etc. and must be equal or less than the argumenst provided."
            );
        }

        return program.Evaluate(new ExpressionScope(arguments));
    }
}
