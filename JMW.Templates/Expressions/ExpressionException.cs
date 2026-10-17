using System;

namespace JMW.Template.Expressions;

/// <summary>
/// Thrown when a template expression cannot be parsed or evaluated.
/// </summary>
[Serializable]
public sealed class ExpressionException : Exception
{
    public ExpressionException(string message)
        : base(message)
    {
    }
}
