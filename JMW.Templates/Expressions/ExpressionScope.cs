using System.Collections.Generic;

namespace JMW.Templates.Expressions;

/// <summary>
/// Variables visible to one expression evaluation. <c>x1</c> (or <c>X1</c>) is the first argument,
/// <c>x2</c> the second, and so on. Assigning to any other name creates a variable for the rest of the evaluation.
/// </summary>
internal sealed class ExpressionScope(IReadOnlyList<string> arguments)
{
    // Most expressions only read their arguments, so the variables are created by the first assignment.
    private Dictionary<string, ScriptValue>? variables;

    public ScriptValue Get(string name)
    {
        return Get(name, TryGetArgumentIndex(name, out int index) ? index : -1);
    }

    /// <param name="argument_index">The argument <paramref name="name"/> stands for, or -1 when it is not one.</param>
    public ScriptValue Get(string name, int argument_index)
    {
        if (variables is not null && variables.TryGetValue(name, out ScriptValue value)) return value;

        if (argument_index >= 0 && argument_index < arguments.Count)
        {
            return ScriptValue.FromArgument(arguments[argument_index]);
        }

        return name switch
        {
            "NaN" => ScriptValue.FromNumber(double.NaN),
            "Infinity" => ScriptValue.FromNumber(double.PositiveInfinity),
            _ => throw new ExpressionException($"'{name}' is not defined."),
        };
    }

    public void Set(string name, ScriptValue value)
    {
        variables ??= new(System.StringComparer.Ordinal);
        variables[name] = value;
    }

    /// <summary>
    /// Maps <c>x1</c>/<c>X1</c> to argument index 0, <c>x2</c> to 1, and so on.
    /// </summary>
    public static bool TryGetArgumentIndex(string name, out int index)
    {
        index = -1;
        if (name.Length < 2 || name[0] is not ('x' or 'X') || name[1] == '0') return false;

        int number = 0;
        for (int i = 1; i < name.Length; i++)
        {
            char c = name[i];
            if (c is < '0' or > '9') return false;
            if (number > (int.MaxValue - 9) / 10) return false;
            number = (number * 10) + (c - '0');
        }

        index = number - 1;
        return true;
    }
}
