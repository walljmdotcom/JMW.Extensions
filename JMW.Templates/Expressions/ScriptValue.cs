using System;
using System.Globalization;

namespace JMW.Template.Expressions;

internal enum ScriptValueKind
{
    Undefined,
    Null,
    Boolean,
    Number,
    String,
}

/// <summary>
/// A JavaScript value restricted to the primitive types template expressions use, with JavaScript's conversion rules.
/// </summary>
internal readonly struct ScriptValue : IEquatable<ScriptValue>
{
    private readonly double number;
    private readonly string? text;

    private ScriptValue(ScriptValueKind kind, double number, string? text)
    {
        Kind = kind;
        this.number = number;
        this.text = text;
    }

    public static ScriptValue Undefined { get; } = new(ScriptValueKind.Undefined, 0, null);

    public static ScriptValue Null { get; } = new(ScriptValueKind.Null, 0, null);

    public static ScriptValue True { get; } = new(ScriptValueKind.Boolean, 1, null);

    public static ScriptValue False { get; } = new(ScriptValueKind.Boolean, 0, null);

    public ScriptValueKind Kind { get; }

    public static ScriptValue FromNumber(double value) => new(ScriptValueKind.Number, value, null);

    public static ScriptValue FromString(string value) => new(ScriptValueKind.String, 0, value);

    public static ScriptValue FromBoolean(bool value) => value ? True : False;

    /// <summary>
    /// Binds a template argument: text that reads as a number becomes a number, anything else stays a string.
    /// </summary>
    public static ScriptValue FromArgument(string argument)
    {
        return double.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? FromNumber(value)
            : FromString(argument);
    }

    public bool ToBoolean()
    {
        return Kind switch
        {
            ScriptValueKind.Boolean => number != 0,
            ScriptValueKind.Number => number != 0 && !double.IsNaN(number),
            ScriptValueKind.String => text?.Length > 0,
            _ => false,
        };
    }

    public double ToNumber()
    {
        return Kind switch
        {
            ScriptValueKind.Boolean or ScriptValueKind.Number => number,
            ScriptValueKind.String => StringToNumber(text ?? string.Empty),
            ScriptValueKind.Null => 0,
            _ => double.NaN,
        };
    }

    public override string ToString()
    {
        return Kind switch
        {
            ScriptValueKind.Undefined => "undefined",
            ScriptValueKind.Null => "null",
            ScriptValueKind.Boolean => number != 0 ? "true" : "false",
            ScriptValueKind.Number => NumberToString(number),
            _ => text ?? string.Empty,
        };
    }

    /// <summary>
    /// JavaScript strict equality (<c>===</c>).
    /// </summary>
    public bool StrictEquals(ScriptValue other)
    {
        if (Kind != other.Kind) return false;

        return Kind switch
        {
            ScriptValueKind.Boolean or ScriptValueKind.Number => number == other.number,
            ScriptValueKind.String => string.Equals(text, other.text, StringComparison.Ordinal),
            _ => true,
        };
    }

    /// <summary>
    /// JavaScript loose equality (<c>==</c>) for primitive values.
    /// </summary>
    public bool LooseEquals(ScriptValue other)
    {
        if (Kind == other.Kind) return StrictEquals(other);

        bool thisIsNullish = Kind is ScriptValueKind.Null or ScriptValueKind.Undefined;
        bool otherIsNullish = other.Kind is ScriptValueKind.Null or ScriptValueKind.Undefined;
        if (thisIsNullish || otherIsNullish) return thisIsNullish && otherIsNullish;

        // Any remaining mix of boolean, number, and string compares numerically.
        return ToNumber() == other.ToNumber();
    }

    public bool Equals(ScriptValue other) => StrictEquals(other);

    public override bool Equals(object? obj) => obj is ScriptValue other && StrictEquals(other);

    public override int GetHashCode()
    {
        return Kind switch
        {
            ScriptValueKind.Boolean or ScriptValueKind.Number => number.GetHashCode(),
            ScriptValueKind.String => StringComparer.Ordinal.GetHashCode(text ?? string.Empty),
            _ => (int)Kind,
        };
    }

    public static bool operator ==(ScriptValue left, ScriptValue right) => left.Equals(right);

    public static bool operator !=(ScriptValue left, ScriptValue right) => !left.Equals(right);

    internal static double StringToNumber(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length == 0) return 0;

        switch (trimmed)
        {
            case "Infinity":
            case "+Infinity":
                return double.PositiveInfinity;
            case "-Infinity":
                return double.NegativeInfinity;
        }

        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
        return double.TryParse(trimmed, style, CultureInfo.InvariantCulture, out double result) ? result : double.NaN;
    }

    internal static string NumberToString(double value)
    {
        if (double.IsNaN(value)) return "NaN";
        if (double.IsPositiveInfinity(value)) return "Infinity";
        if (double.IsNegativeInfinity(value)) return "-Infinity";
        if (value == 0) return "0";

        // Shortest round-trip digits, then laid out the way JavaScript's Number.prototype.toString does:
        // plain notation for exponents -7 < e < 21, exponent notation otherwise.
        string roundTrip = value.ToString("R", CultureInfo.InvariantCulture);
        bool negative = roundTrip[0] == '-';
        if (negative) roundTrip = roundTrip.Substring(1);

        int exponentAt = roundTrip.IndexOf('E');
        string mantissa = exponentAt < 0 ? roundTrip : roundTrip.Substring(0, exponentAt);
        int exponent = exponentAt < 0 ? 0 : int.Parse(roundTrip.Substring(exponentAt + 1), CultureInfo.InvariantCulture);

        int pointAt = mantissa.IndexOf('.');
        string digits = pointAt < 0 ? mantissa : mantissa.Remove(pointAt, 1);
        int integerDigits = (pointAt < 0 ? mantissa.Length : pointAt) + exponent;

        int leadingZeros = 0;
        while (leadingZeros < digits.Length - 1 && digits[leadingZeros] == '0') leadingZeros++;
        digits = digits.Substring(leadingZeros).TrimEnd('0');
        integerDigits -= leadingZeros;
        if (digits.Length == 0) return "0";

        string result;
        if (integerDigits > 21 || integerDigits <= -6)
        {
            int e = integerDigits - 1;
            string fraction = digits.Length > 1 ? "." + digits.Substring(1) : string.Empty;
            result = digits[0] + fraction + "e" + (e < 0 ? "-" : "+") + Math.Abs(e).ToString(CultureInfo.InvariantCulture);
        }
        else if (integerDigits <= 0)
        {
            result = "0." + new string('0', -integerDigits) + digits;
        }
        else if (integerDigits >= digits.Length)
        {
            result = digits + new string('0', integerDigits - digits.Length);
        }
        else
        {
            result = digits.Substring(0, integerDigits) + "." + digits.Substring(integerDigits);
        }

        return negative ? "-" + result : result;
    }
}
