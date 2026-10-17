using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace JMW.Templates.Expressions;

internal static class ScriptEvaluator
{
    // Table tags evaluate the same expression once per row. Parsed trees hold no state, so they are shared. The
    // limit keeps generated expression text from growing the cache without end.
    private const int CacheLimit = 1024;

    private static readonly ConcurrentDictionary<string, ParsedExpression> cache = new(StringComparer.Ordinal);

    private readonly record struct ParsedExpression(ExpressionNode Program, int HighestArgument);

    /// <summary>
    /// Evaluates a template expression with <paramref name="arguments"/> bound to <c>x1</c>, <c>x2</c>, and so on.
    /// Argument text that reads as a number is bound as a number.
    /// </summary>
    /// <exception cref="ExpressionException">The expression is malformed or fails at run time.</exception>
    /// <exception cref="ArgumentException">The expression uses an <c>xN</c> with no matching argument.</exception>
    public static ScriptValue Evaluate(string expression, IReadOnlyList<string> arguments)
    {
        if (!cache.TryGetValue(expression, out ParsedExpression parsed))
        {
            ExpressionNode node = ExpressionParser.Parse(expression, out int highest);
            parsed = new ParsedExpression(node, highest);
            if (cache.Count >= CacheLimit)
            {
                cache.Clear();
            }

            cache[expression] = parsed;
        }

        (ExpressionNode program, int highestArgument) = parsed;
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
