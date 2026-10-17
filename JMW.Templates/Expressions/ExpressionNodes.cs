using System;
using System.Collections.Generic;
using System.Globalization;

namespace JMW.Templates.Expressions;

internal abstract class ExpressionNode
{
    public abstract ScriptValue Evaluate(ExpressionScope scope);
}

internal sealed class LiteralNode(ScriptValue value) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope) => value;
}

internal sealed class IdentifierNode(string name) : ExpressionNode
{
    public string Name { get; } = name;

    public override ScriptValue Evaluate(ExpressionScope scope) => scope.Get(Name);
}

internal sealed class SequenceNode(IReadOnlyList<ExpressionNode> statements) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        ScriptValue result = ScriptValue.Undefined;
        foreach (ExpressionNode statement in statements)
        {
            result = statement.Evaluate(scope);
        }

        return result;
    }
}

internal sealed class AssignNode(string name, string op, ExpressionNode value) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        ScriptValue right = value.Evaluate(scope);
        ScriptValue result = op == "=" ? right : BinaryNode.Apply(op.Substring(0, 1), scope.Get(name), right);
        scope.Set(name, result);
        return result;
    }
}

internal sealed class UpdateNode(string name, bool increment, bool prefix) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        double before = scope.Get(name).ToNumber();
        double after = increment ? before + 1 : before - 1;
        scope.Set(name, ScriptValue.FromNumber(after));
        return ScriptValue.FromNumber(prefix ? after : before);
    }
}

internal sealed class UnaryNode(string op, ExpressionNode operand) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        ScriptValue value = operand.Evaluate(scope);
        return op switch
        {
            "!" => ScriptValue.FromBoolean(!value.ToBoolean()),
            "-" => ScriptValue.FromNumber(-value.ToNumber()),
            "+" => ScriptValue.FromNumber(value.ToNumber()),
            _ => throw new ExpressionException($"Unknown unary operator '{op}'."),
        };
    }
}

internal sealed class LogicalNode(bool isAnd, ExpressionNode left, ExpressionNode right) : ExpressionNode
{
    // Like JavaScript, && and || return one of their operands, not a boolean.
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        ScriptValue first = left.Evaluate(scope);
        return first.ToBoolean() == isAnd ? right.Evaluate(scope) : first;
    }
}

internal sealed class ConditionalNode(ExpressionNode test, ExpressionNode whenTrue, ExpressionNode whenFalse) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        return test.Evaluate(scope).ToBoolean() ? whenTrue.Evaluate(scope) : whenFalse.Evaluate(scope);
    }
}

internal sealed class BinaryNode(string op, ExpressionNode left, ExpressionNode right) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope) => Apply(op, left.Evaluate(scope), right.Evaluate(scope));

    public static ScriptValue Apply(string op, ScriptValue a, ScriptValue b)
    {
        switch (op)
        {
            case "+":
                return a.Kind == ScriptValueKind.String || b.Kind == ScriptValueKind.String
                    ? ScriptValue.FromString(a.ToString() + b.ToString())
                    : ScriptValue.FromNumber(a.ToNumber() + b.ToNumber());
            case "-": return ScriptValue.FromNumber(a.ToNumber() - b.ToNumber());
            case "*": return ScriptValue.FromNumber(a.ToNumber() * b.ToNumber());
            case "/": return ScriptValue.FromNumber(a.ToNumber() / b.ToNumber());
            // JavaScript's % keeps the sign of the dividend, the same as C#'s % on doubles.
            case "%": return ScriptValue.FromNumber(a.ToNumber() % b.ToNumber());
            case "==": return ScriptValue.FromBoolean(a.LooseEquals(b));
            case "!=": return ScriptValue.FromBoolean(!a.LooseEquals(b));
            case "===": return ScriptValue.FromBoolean(a.StrictEquals(b));
            case "!==": return ScriptValue.FromBoolean(!a.StrictEquals(b));
            case "<": return Compare(a, b, static c => c < 0);
            case ">": return Compare(a, b, static c => c > 0);
            case "<=": return Compare(a, b, static c => c <= 0);
            case ">=": return Compare(a, b, static c => c >= 0);
            default: throw new ExpressionException($"Unknown operator '{op}'.");
        }
    }

    private static ScriptValue Compare(ScriptValue a, ScriptValue b, Func<int, bool> test)
    {
        if (a.Kind == ScriptValueKind.String && b.Kind == ScriptValueKind.String)
        {
            return ScriptValue.FromBoolean(test(string.CompareOrdinal(a.ToString(), b.ToString())));
        }

        double x = a.ToNumber();
        double y = b.ToNumber();
        if (double.IsNaN(x) || double.IsNaN(y)) return ScriptValue.False;

        return ScriptValue.FromBoolean(test(x.CompareTo(y)));
    }
}

internal sealed class MemberNode(ExpressionNode target, string name) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        ScriptValue value = target.Evaluate(scope);
        if (name == "length" && value.Kind == ScriptValueKind.String)
        {
            return ScriptValue.FromNumber(value.ToString().Length);
        }

        if (value.Kind is ScriptValueKind.Null or ScriptValueKind.Undefined)
        {
            throw new ExpressionException($"Cannot read property '{name}' of {value}.");
        }

        return ScriptValue.Undefined;
    }
}

internal sealed class CallNode(ExpressionNode target, string name, IReadOnlyList<ExpressionNode> arguments) : ExpressionNode
{
    public override ScriptValue Evaluate(ExpressionScope scope)
    {
        ScriptValue value = target.Evaluate(scope);
        var args = new ScriptValue[arguments.Count];
        for (int i = 0; i < args.Length; i++)
        {
            args[i] = arguments[i].Evaluate(scope);
        }

        return value.Kind switch
        {
            ScriptValueKind.String => StringMethods.Call(value.ToString(), name, args),
            ScriptValueKind.Number => NumberMethod(value.ToNumber(), name, args),
            ScriptValueKind.Boolean when name == "toString" => ScriptValue.FromString(value.ToString()),
            _ => throw new ExpressionException($"'{name}' is not a function on {value}."),
        };
    }

    private static ScriptValue NumberMethod(double value, string name, ScriptValue[] args)
    {
        switch (name)
        {
            case "toString":
                return ScriptValue.FromString(ScriptValue.NumberToString(value));
            case "toFixed":
                int digits = args.Length > 0 ? (int)args[0].ToNumber() : 0;
                if (digits is < 0 or > 100)
                {
                    throw new ExpressionException("toFixed() digits argument must be between 0 and 100.");
                }

                if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) >= 1e21)
                {
                    return ScriptValue.FromString(ScriptValue.NumberToString(value));
                }

                string format = "F" + digits.ToString(CultureInfo.InvariantCulture);
                return ScriptValue.FromString(value.ToString(format, CultureInfo.InvariantCulture));
            default:
                throw new ExpressionException($"'{name}' is not a function on a number.");
        }
    }
}
