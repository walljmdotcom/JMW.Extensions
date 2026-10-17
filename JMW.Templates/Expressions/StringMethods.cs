using System;
using System.Text;

namespace JMW.Templates.Expressions;

/// <summary>
/// The JavaScript String.prototype methods available to template expressions.
/// </summary>
internal static class StringMethods
{
    public static ScriptValue Call(string s, string name, ScriptValue[] args)
    {
        return name switch
        {
            "substr" => ScriptValue.FromString(Substr(s, args)),
            "substring" => ScriptValue.FromString(Substring(s, args)),
            "slice" => ScriptValue.FromString(Slice(s, args)),
            "indexOf" => ScriptValue.FromNumber(IndexOf(s, args)),
            "lastIndexOf" => ScriptValue.FromNumber(LastIndexOf(s, args)),
            "includes" => ScriptValue.FromBoolean(s.IndexOf(Arg(args, 0), StringComparison.Ordinal) >= 0),
            "startsWith" => ScriptValue.FromBoolean(s.StartsWith(Arg(args, 0), StringComparison.Ordinal)),
            "endsWith" => ScriptValue.FromBoolean(s.EndsWith(Arg(args, 0), StringComparison.Ordinal)),
            "charAt" => ScriptValue.FromString(CharAt(s, args)),
            "toLowerCase" => ScriptValue.FromString(s.ToLowerInvariant()),
            "toUpperCase" => ScriptValue.FromString(s.ToUpperInvariant()),
            "trim" => ScriptValue.FromString(s.Trim()),
            "trimStart" => ScriptValue.FromString(s.TrimStart()),
            "trimEnd" => ScriptValue.FromString(s.TrimEnd()),
            "replace" => ScriptValue.FromString(ReplaceFirst(s, Arg(args, 0), Arg(args, 1))),
            "padStart" => ScriptValue.FromString(Pad(s, args, atStart: true)),
            "padEnd" => ScriptValue.FromString(Pad(s, args, atStart: false)),
            "toString" => ScriptValue.FromString(s),
            _ => throw new ExpressionException($"'{name}' is not a supported string function."),
        };
    }

    private static string Substr(string s, ScriptValue[] args)
    {
        int start = ToInteger(args, 0, 0);
        if (start < 0) start = Math.Max(s.Length + start, 0);
        start = Math.Min(start, s.Length);

        int length = IsMissing(args, 1) ? s.Length - start : ToInteger(args, 1, 0);
        length = Clamp(length, 0, s.Length - start);
        return s.Substring(start, length);
    }

    private static string Substring(string s, ScriptValue[] args)
    {
        int start = Clamp(ToInteger(args, 0, 0), 0, s.Length);
        int end = IsMissing(args, 1) ? s.Length : Clamp(ToInteger(args, 1, 0), 0, s.Length);
        if (start > end) (start, end) = (end, start);
        return s.Substring(start, end - start);
    }

    private static string Slice(string s, ScriptValue[] args)
    {
        int start = RelativeIndex(ToInteger(args, 0, 0), s.Length);
        int end = IsMissing(args, 1) ? s.Length : RelativeIndex(ToInteger(args, 1, 0), s.Length);
        return end > start ? s.Substring(start, end - start) : string.Empty;
    }

    private static int IndexOf(string s, ScriptValue[] args)
    {
        int from = Clamp(ToInteger(args, 1, 0), 0, s.Length);
        return s.IndexOf(Arg(args, 0), from, StringComparison.Ordinal);
    }

    private static int LastIndexOf(string s, ScriptValue[] args)
    {
        string search = Arg(args, 0);
        int from = IsMissing(args, 1) || double.IsNaN(args[1].ToNumber()) ? s.Length : Clamp(ToInteger(args, 1, 0), 0, s.Length);
        int start = Math.Min(from, s.Length - search.Length);
        for (int i = start; i >= 0; i--)
        {
            if (string.CompareOrdinal(s, i, search, 0, search.Length) == 0) return i;
        }

        return -1;
    }

    private static string CharAt(string s, ScriptValue[] args)
    {
        int index = ToInteger(args, 0, 0);
        return index >= 0 && index < s.Length ? s[index].ToString() : string.Empty;
    }

    private static string ReplaceFirst(string s, string search, string replacement)
    {
        int at = s.IndexOf(search, StringComparison.Ordinal);
        return at < 0 ? s : s.Substring(0, at) + replacement + s.Substring(at + search.Length);
    }

    private static string Pad(string s, ScriptValue[] args, bool atStart)
    {
        int target = ToInteger(args, 0, 0);
        string fill = IsMissing(args, 1) ? " " : Arg(args, 1);
        if (target <= s.Length || fill.Length == 0) return s;

        var padding = new StringBuilder(target - s.Length);
        while (padding.Length < target - s.Length) padding.Append(fill);
        padding.Length = target - s.Length;

        return atStart ? padding + s : s + padding;
    }

    private static string Arg(ScriptValue[] args, int index) => index < args.Length ? args[index].ToString() : "undefined";

    private static bool IsMissing(ScriptValue[] args, int index) => index >= args.Length || args[index].Kind == ScriptValueKind.Undefined;

    // JavaScript's ToIntegerOrInfinity, clamped into int range: NaN is 0, fractions truncate toward zero.
    private static int ToInteger(ScriptValue[] args, int index, int missing)
    {
        if (IsMissing(args, index)) return missing;

        double value = args[index].ToNumber();
        if (double.IsNaN(value)) return 0;
        if (value >= int.MaxValue) return int.MaxValue;
        if (value <= int.MinValue) return int.MinValue;
        return (int)Math.Truncate(value);
    }

    private static int RelativeIndex(int index, int length) => index < 0 ? Math.Max(length + index, 0) : Math.Min(index, length);

    private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
}
