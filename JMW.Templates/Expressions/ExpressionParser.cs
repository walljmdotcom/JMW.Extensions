using System.Collections.Generic;

namespace JMW.Templates.Expressions;

/// <summary>
/// Parses the JavaScript subset template expressions use: literals, the <c>x1..xN</c> variables, arithmetic, comparison,
/// logical and conditional operators, assignment, <c>++</c>/<c>--</c>, string methods, and <c>;</c>-separated
/// statements whose last value is the result. There are no loops or function definitions, so evaluation always ends.
/// </summary>
internal sealed class ExpressionParser
{
    private const int maxDepth = 200;

    private readonly List<ExpressionToken> tokens;
    private int position;
    private int depth;
    private int highestArgument;

    private ExpressionParser(string source)
    {
        tokens = ExpressionLexer.Tokenize(source);
    }

    /// <summary>
    /// Parses <paramref name="source"/>. <paramref name="highestArgument"/> is the highest <c>xN</c> referenced,
    /// so callers can check it against the arguments they will supply.
    /// </summary>
    public static ExpressionNode Parse(string source, out int highestArgument)
    {
        var parser = new ExpressionParser(source);
        ExpressionNode program = parser.ParseProgram();
        highestArgument = parser.highestArgument;
        return program;
    }

    private ExpressionToken Current => tokens[position];

    private ExpressionNode ParseProgram()
    {
        var statements = new List<ExpressionNode>();
        while (Current.Kind != ExpressionTokenKind.End)
        {
            if (TryConsume(";")) continue;

            statements.Add(ParseAssignment());

            if (Current.Kind != ExpressionTokenKind.End) Expect(";");
        }

        return statements.Count == 1 ? statements[0] : new SequenceNode(statements);
    }

    private ExpressionNode ParseAssignment()
    {
        Enter();
        ExpressionNode left = ParseConditional();

        if (Current.Kind == ExpressionTokenKind.Punctuator && Current.Text is "=" or "+=" or "-=" or "*=" or "/=" or "%=")
        {
            ExpressionToken op = Current;
            if (left is not IdentifierNode target)
            {
                throw new ExpressionException($"Invalid assignment target at position {op.Position}.");
            }

            position++;
            left = new AssignNode(target.Name, op.Text, ParseAssignment());
        }

        Leave();
        return left;
    }

    private ExpressionNode ParseConditional()
    {
        ExpressionNode test = ParseBinary(1);
        if (!TryConsume("?")) return test;

        ExpressionNode whenTrue = ParseAssignment();
        Expect(":");
        ExpressionNode whenFalse = ParseAssignment();
        return new ConditionalNode(test, whenTrue, whenFalse);
    }

    private static int Precedence(string op)
    {
        return op switch
        {
            "||" => 1,
            "&&" => 2,
            "==" or "!=" or "===" or "!==" => 3,
            "<" or ">" or "<=" or ">=" => 4,
            "+" or "-" => 5,
            "*" or "/" or "%" => 6,
            _ => -1,
        };
    }

    // Precedence climbing: every binary operator here is left-associative.
    private ExpressionNode ParseBinary(int minPrecedence)
    {
        ExpressionNode left = ParseUnary();

        while (Current.Kind == ExpressionTokenKind.Punctuator)
        {
            string op = Current.Text;
            int precedence = Precedence(op);
            if (precedence < minPrecedence) break;

            position++;
            Enter();
            ExpressionNode right = ParseBinary(precedence + 1);
            Leave();

            left = op switch
            {
                "&&" => new LogicalNode(isAnd: true, left, right),
                "||" => new LogicalNode(isAnd: false, left, right),
                _ => new BinaryNode(op, left, right),
            };
        }

        return left;
    }

    private ExpressionNode ParseUnary()
    {
        if (Current.Kind == ExpressionTokenKind.Punctuator)
        {
            string op = Current.Text;
            if (op is "!" or "-" or "+")
            {
                position++;
                Enter();
                var node = new UnaryNode(op, ParseUnary());
                Leave();
                return node;
            }

            if (op is "++" or "--")
            {
                position++;
                Enter();
                ExpressionNode operand = ParseUnary();
                Leave();
                if (operand is not IdentifierNode target)
                {
                    throw new ExpressionException($"Invalid {op} target at position {Current.Position}.");
                }

                return new UpdateNode(target.Name, increment: op == "++", prefix: true);
            }
        }

        return ParsePostfix();
    }

    private ExpressionNode ParsePostfix()
    {
        ExpressionNode node = ParsePrimary();

        while (TryConsume("."))
        {
            if (Current.Kind != ExpressionTokenKind.Identifier)
            {
                throw new ExpressionException($"Expected a property name at position {Current.Position}.");
            }

            string name = Current.Text;
            position++;

            node = Current.Kind == ExpressionTokenKind.Punctuator && Current.Text == "("
                ? new CallNode(node, name, ParseArguments())
                : new MemberNode(node, name);
        }

        if (node is IdentifierNode target && Current.Kind == ExpressionTokenKind.Punctuator && Current.Text is "++" or "--")
        {
            bool increment = Current.Text == "++";
            position++;
            return new UpdateNode(target.Name, increment, prefix: false);
        }

        return node;
    }

    private List<ExpressionNode> ParseArguments()
    {
        Expect("(");
        var arguments = new List<ExpressionNode>();
        if (TryConsume(")")) return arguments;

        do
        {
            arguments.Add(ParseAssignment());
        }
        while (TryConsume(","));

        Expect(")");
        return arguments;
    }

    private ExpressionNode ParsePrimary()
    {
        ExpressionToken token = Current;
        switch (token.Kind)
        {
            case ExpressionTokenKind.Number:
                position++;
                return new LiteralNode(ScriptValue.FromNumber(token.Number));

            case ExpressionTokenKind.String:
                position++;
                return new LiteralNode(ScriptValue.FromString(token.Text));

            case ExpressionTokenKind.Identifier:
                position++;
                return token.Text switch
                {
                    "true" => new LiteralNode(ScriptValue.True),
                    "false" => new LiteralNode(ScriptValue.False),
                    "null" => new LiteralNode(ScriptValue.Null),
                    "undefined" => new LiteralNode(ScriptValue.Undefined),
                    _ => Identifier(token.Text),
                };

            case ExpressionTokenKind.Punctuator when token.Text == "(":
                position++;
                Enter();
                ExpressionNode inner = ParseAssignment();
                Leave();
                Expect(")");
                return inner;

            case ExpressionTokenKind.End:
                throw new ExpressionException("Unexpected end of expression.");

            default:
                throw new ExpressionException($"Unexpected '{token.Text}' at position {token.Position}.");
        }
    }

    private IdentifierNode Identifier(string name)
    {
        if (ExpressionScope.TryGetArgumentIndex(name, out int index) && index + 1 > highestArgument)
        {
            highestArgument = index + 1;
        }

        return new IdentifierNode(name);
    }

    private bool TryConsume(string punctuator)
    {
        if (Current.Kind != ExpressionTokenKind.Punctuator || Current.Text != punctuator) return false;

        position++;
        return true;
    }

    private void Expect(string punctuator)
    {
        if (!TryConsume(punctuator))
        {
            string found = Current.Kind == ExpressionTokenKind.End ? "end of expression" : $"'{Current.Text}'";
            throw new ExpressionException($"Expected '{punctuator}' but found {found} at position {Current.Position}.");
        }
    }

    private void Enter()
    {
        if (++depth > maxDepth)
        {
            throw new ExpressionException($"Expression is nested more than {maxDepth} levels deep.");
        }
    }

    private void Leave() => depth--;
}
