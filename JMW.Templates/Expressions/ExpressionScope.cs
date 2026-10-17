using System.Collections.Generic;

namespace JMW.Template.Expressions;

/// <summary>
/// Variables visible to one expression evaluation. <c>x1</c> (or <c>X1</c>) is the first argument,
/// <c>x2</c> the second, and so on. Assigning to any other name creates a variable for the rest of the evaluation.
/// </summary>
internal sealed class ExpressionScope(IReadOnlyList<string> arguments)
{
    private readonly Dictionary<string, ScriptValue> variables = new(System.StringComparer.Ordinal);

    public ScriptValue Get(string name)
    {
        if (variables.TryGetValue(name, out ScriptValue value)) return value;

        if (TryGetArgumentIndex(name, out int index) && index < arguments.Count)
        {
            value = ScriptValue.FromArgument(arguments[index]);
            variables[name] = value;
            return value;
        }

        return name switch
        {
            "NaN" => ScriptValue.FromNumber(double.NaN),
            "Infinity" => ScriptValue.FromNumber(double.PositiveInfinity),
            _ => throw new ExpressionException($"'{name}' is not defined."),
        };
    }

    public void Set(string name, ScriptValue value) => variables[name] = value;

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
