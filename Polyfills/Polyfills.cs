// Compiled into each netstandard2.0 library as internal types, so the source can use APIs and
// attributes that netstandard2.0 lacks without taking a package dependency.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit;
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
    internal sealed class NotNullAttribute : Attribute;
}

namespace System
{
    internal static class PolyfillExtensions
    {
        extension(ArgumentNullException)
        {
            public static void ThrowIfNull(
                [NotNull] object? argument,
                [CallerArgumentExpression(nameof(argument))] string? paramName = null
            )
            {
                if (argument is null)
                {
                    throw new ArgumentNullException(paramName);
                }
            }
        }

        extension(string s)
        {
            public bool Contains(char value) => s.IndexOf(value) >= 0;

            public bool Contains(string value, StringComparison comparisonType) => s.IndexOf(value, comparisonType) >= 0;
        }

        extension<T>(Queue<T> queue)
        {
            public bool TryDequeue([MaybeNullWhen(false)] out T result)
            {
                if (queue.Count == 0)
                {
                    result = default;
                    return false;
                }

                result = queue.Dequeue();
                return true;
            }
        }
    }
}

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
    {
        public string ParameterName { get; } = parameterName;
    }
}
