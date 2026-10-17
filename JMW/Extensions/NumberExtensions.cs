using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JMW.Types;
using Lambda.Generic.Arithmetic;

namespace JMW.Extensions;

public static class NumberExtensions
{
    /// <summary>
    /// Indicates if the string is an <see cref="int"/>
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsInt(this string s)
    {
        return int.TryParse(s, out _);
    }

    /// <summary>
    /// Indicates if the string is an <see cref="long"/>
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsLong(this string s)
    {
        return long.TryParse(s, out _);
    }

    /// <summary>
    /// Indicates if the string is a <see cref="double"/>
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static bool IsDouble(this string s)
    {
        return double.TryParse(s, out _);
    }

    /// <summary>
    /// Converts a Decimal to an Int32
    /// </summary>
    public static int ToInt(this decimal d)
    {
        return Convert.ToInt32(d);
    }

    /// <summary>
    /// Converts a Double to an Int32
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public static int ToInt(this double d)
    {
        return Convert.ToInt32(d);
    }

    /// <summary>
    /// A highly performant integer checking function.  Less robust than the default C# implementation.
    /// </summary>
    /// <param name="str">String to check</param>
    /// <returns>If the string is a 32bit Integer, true, else false.</returns>
    public static bool IsIntFast(this string str)
    {
        foreach (int c in str)
        {
            // ignore spaces
            if (c < 33)
                continue;

            // make sure the characters are the digits 0-9
            if (c < '0' || c > '9')
                return false;
        }

        return true;
    }

    /// <summary>
    /// A highly performant integer conversion function.  Less robust than the default C# implementation. Use at your own risk.
    /// </summary>
    /// <param name="s">String to check</param>
    /// <returns>If the string is a 32bit Integer, true, else false.</returns>
    public static int ToIntFast(this string s)
    {
        if (s.Length == 0)
            throw new ArgumentOutOfRangeException(nameof(s), "String: " + s + " is not a valid number.");

        var result = 0;
        var c = s[0];
        var start = 0;
        var end = s.Length;

        for (; end > 0; end--)
        {
            if (!IgnoreChar(s[end - 1]))
                break;
        }

        int sign;
        if (c == '-')
        {
            sign = -1;
            start = 0 + 1;
        }
        else if (c > 57 || c < 48)
        {
            if (IgnoreChar(c))
            {
                do
                {
                    ++start;
                }
                while (start < end && IgnoreChar(c = s[start]));

                if (start >= end)
                {
                    throw new ArgumentOutOfRangeException(nameof(s), "String: " + s + " is not a valid number.");
                }

                if (c == '-')
                {
                    sign = -1;
                    ++start;
                }
                else
                {
                    sign = 1;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(s), "String: " + s + " is not a valid number.");
            }
        }
        else
        {
            start = 0 + 1;
            result = 10 * result + (c - 48);
            sign = 1;
        }

        var i = start;

        for (; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                throw new ArgumentOutOfRangeException(nameof(s), "String: " + s + " is not a valid number.");
            }

            result = 10 * result + (c - 48);
        }

        result *= sign;
        return result;
    }

    /// <summary>
    /// A highly performant integer conversion function.  Less robust than the default C# implementation. Use at your own risk.
    /// </summary>
    /// <param name="s">String to check</param>
    /// <param name="def">The value to return if the number isn't an integer.</param>
    public static int ToIntOrDefaultFast(this string s, int def = 0)
    {
        if (s.Length == 0)
            return def;

        var result = 0;
        var c = s[0];
        var start = 0;
        var end = s.Length;

        // handle trailing whitespace
        for (; end > 0; end--)
        {
            if (!IgnoreChar(s[end - 1]))
                break;
        }

        int sign;
        if (c == '-')
        {
            sign = -1;
            start = 1;
        }
        else if (c > 57 || c < 48)
        {
            if (IgnoreChar(c))
            {
                do
                {
                    ++start;
                }
                while (start < end && IgnoreChar(c = s[start]));

                if (start >= end)
                {
                    return def;
                }

                if (c == '-')
                {
                    sign = -1;
                    ++start;
                }
                else
                {
                    sign = 1;
                }
            }
            else
            {
                return def;
            }
        }
        else
        {
            start = 0 + 1;
            result = 10 * result + (c - 48);
            sign = 1;
        }

        var i = start;

        for (; i < end; ++i)
        {
            c = s[i];
            if (c > 57 || c < 48)
            {
                return def;
            }

            result = 10 * result + (c - 48);
        }

        result *= sign;
        return result;
    }

    /// <summary>
    /// Converts a Decimal to an Int32
    /// </summary>
    public static long ToLong(this decimal d)
    {
        return Convert.ToInt64(d);
    }

    /// <summary>
    /// Converts a Double to an Int32
    /// </summary>
    /// <param name="d"></param>
    /// <returns></returns>
    public static long ToLong(this double d)
    {
        return Convert.ToInt64(d);
    }

    /// <summary>
    /// Converts a string to an integer using int.Parse
    /// </summary>
    public static long ToLong(this string i)
    {
        return long.Parse(i, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a string to an integer using int.Parse
    /// </summary>
    public static double ToDouble(this string i)
    {
        return double.Parse(i, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a string to an integer using int.Parse
    /// </summary>
    public static double ToDouble(this int i)
    {
        return Convert.ToDouble(i);
    }

    /// <summary>
    /// Converts a string to an integer using int.Parse
    /// </summary>
    public static double ToDouble(this long i)
    {
        return Convert.ToDouble(i);
    }

    private static long abbreviationMultiplier(char c) => char.ToLowerInvariant(c) switch
    {
        'k' => 1000,
        'm' => 1000000,
        'g' => 1000000000,
        'b' => 1000000000,
        't' => 1000000000000,
        'p' => 1000000000000000,
        _ => 0
    };

    /// <summary>
    /// Parses an abbreviated number such as "8.2m" exactly, using decimal arithmetic so that the product is not
    /// subject to binary floating point rounding.
    /// </summary>
    /// <returns>False if the text is not a number followed by a known abbreviation.</returns>
    private static bool tryParseAbbreviated(string s, out decimal value)
    {
        value = 0;
        if (s.Length < 2)
            return false;

        var multiplier = abbreviationMultiplier(s[s.Length - 1]);
        if (multiplier == 0)
            return false;

        if (!decimal.TryParse(
                s.Substring(0, s.Length - 1),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out var prefix
            ))
            return false;

        // saturate rather than overflow decimal; any saturated value is outside the range of long anyway
        if (Math.Abs(prefix) > decimal.MaxValue / multiplier)
            value = prefix < 0 ? decimal.MinValue : decimal.MaxValue;
        else
            value = prefix * multiplier;

        return true;
    }

    private static bool tryToInt64(decimal d, out long value)
    {
        value = 0;
        if (d != decimal.Truncate(d) || d < long.MinValue || d > long.MaxValue)
            return false;

        value = (long)d;
        return true;
    }

    private static bool tryToInt32(decimal d, out int value)
    {
        value = 0;
        if (d != decimal.Truncate(d) || d < int.MinValue || d > int.MaxValue)
            return false;

        value = (int)d;
        return true;
    }

    private static decimal requireWhole(decimal d, string s, decimal min, decimal max)
    {
        if (d != decimal.Truncate(d))
            throw new FormatException("The value '" + s + "' is not a whole number.");

        if (d < min || d > max)
            throw new OverflowException("The value '" + s + "' is outside the range of the target type.");

        return d;
    }

    /// <summary>
    /// Handles converting text to an <see cref="long"/>. Will properly deal with the following common abbreviations
    ///
    /// <para>
    /// k1 == 1000 // 1 thousand (kila)
    /// m1 == 1,000,000 // 1 million (mega)
    /// g1 == 1,000,000,000 // 1 billion (giga)
    /// b1 == 1,000,000,000 // 1 billion (giga)
    /// t1 == 1,000,000,000,000 // 1 trillion (tera)
    /// p1 == 1,000,000,000,000,000 // 1 quadrillion (peda)
    /// </para>
    /// </summary>
    /// <param name="i">The text to be converted</param>
    /// <returns>Value as a <see cref="long"/></returns>
    public static long ToInt64(this string i)
    {
        if (tryParseAbbreviated(i, out var n))
            return (long)requireWhole(n, i, long.MinValue, long.MaxValue);

        return long.Parse(i, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Handles converting text to an <see cref="long"/>. Will properly deal with the following common abbreviations
    ///
    /// <para>
    /// k1 == 1000 // 1 thousand (kila)
    /// m1 == 1,000,000 // 1 million (mega)
    /// g1 == 1,000,000,000 // 1 billion (giga)
    /// b1 == 1,000,000,000 // 1 billion (giga)
    /// t1 == 1,000,000,000,000 // 1 trillion (tera)
    /// p1 == 1,000,000,000,000,000 // 1 quadrillion (peda)
    /// </para>
    /// </summary>
    /// <param name="i">The text to be converted</param>
    /// <returns>Value as a <see cref="long"/></returns>
    public static long ToInt64OrNeg1(this string i)
    {
        return i.ToInt64orDefault(-1);
    }

    ///  <summary>
    ///  Handles converting text to an <see cref="long"/>. Will properly deal with the following common abbreviations
    ///
    ///  <para>
    ///  k1 == 1000 // 1 thousand (kila)
    ///  m1 == 1,000,000 // 1 million (mega)
    ///  g1 == 1,000,000,000 // 1 billion (giga)
    ///  b1 == 1,000,000,000 // 1 billion (giga)
    ///  t1 == 1,000,000,000,000 // 1 trillion (tera)
    ///  p1 == 1,000,000,000,000,000 // 1 quadrillion (peda)
    ///  </para>
    ///  </summary>
    ///  <param name="i">The text to be converted</param>
    /// <param name="def">The number to return if its not a valid value</param>
    /// <returns>Value as a <see cref="long"/></returns>
    public static long ToInt64orDefault(this string i, long def = -1)
    {
        long val;
        if (tryParseAbbreviated(i, out var n))
            return tryToInt64(n, out val) ? val : def;

        return long.TryParse(i, NumberStyles.Integer, CultureInfo.InvariantCulture, out val) ? val : def;
    }

    /// <summary>
    /// Handles converting text to an <see cref="int"/>. Will properly deal with the following common abbreviations
    ///
    /// <para>
    /// k1 == 1000 // 1 thousand (kila)
    /// m1 == 1,000,000 // 1 million (mega)
    /// </para>
    /// </summary>
    /// <param name="i">The text to be converted</param>
    /// <returns>Value as a <see cref="int"/></returns>
    public static int ToInt(this string i)
    {
        if (tryParseAbbreviated(i, out var n))
            return (int)requireWhole(n, i, int.MinValue, int.MaxValue);

        return int.Parse(i, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Handles converting text to an <see cref="int"/>. Will properly deal with the following common abbreviations
    ///
    /// <para>
    /// k1 == 1000 // 1 thousand (kila)
    /// m1 == 1,000,000 // 1 million (mega)
    /// </para>
    /// </summary>
    /// <param name="i">The text to be converted</param>
    /// <returns>Value as a <see cref="int"/></returns>
    public static int ToIntOrNeg1(this string i)
    {
        return i.ToIntOrDefault(-1);
    }

    ///  <summary>
    ///  Handles converting text to an <see cref="int"/>. Will properly deal with the following common abbreviations
    ///
    ///  <para>
    ///  k1 == 1000 // 1 thousand (kila)
    ///  m1 == 1,000,000 // 1 million (mega)
    ///  </para>
    ///  </summary>
    ///  <param name="i">The text to be converted</param>
    /// <param name="def">Value to return if not a valid Integer.</param>
    /// <returns>Value as a <see cref="int"/></returns>
    public static int ToIntOrDefault(this string i, int def = -1)
    {
        int val;
        if (tryParseAbbreviated(i, out var n))
            return tryToInt32(n, out val) ? val : def;

        return int.TryParse(i, NumberStyles.Integer, CultureInfo.InvariantCulture, out val) ? val : def;
    }

    public static float? ToFloat(this string s)
    {
        var r = float.TryParse(s, out var i);

        return r ? i : null;
    }

    ///  <summary>
    ///  Handles converting text to an <see cref="float"/>
    ///  </summary>
    ///  <param name="s">The text to be converted</param>
    /// <returns>Value as a <see cref="float"/> or -1 of invalid</returns>
    public static float ToFloatOrNeg1(this string s)
    {
        return s.ToFloatOrDefault(-1);
    }

    ///  <summary>
    ///  Handles converting text to an <see cref="float"/>
    ///  </summary>
    ///  <param name="s">The text to be converted</param>
    /// <param name="def">Value to return if not a valid Integer.</param>
    /// <returns>Value as a <see cref="float"/></returns>
    public static float ToFloatOrDefault(this string s, float def = -1)
    {
        var r = float.TryParse(s, out var i);

        return r ? i : def;
    }

    ///  <summary>
    ///  Handles converting text to an <see cref="double"/>
    ///  </summary>
    ///  <param name="s">The text to be converted</param>
    /// <returns>Value as a <see cref="double"/> or -1 of invalid</returns>
    public static double ToDoubleOrNeg1(this string s)
    {
        return s.ToDoubleOrDefault(-1);
    }

    ///  <summary>
    ///  Handles converting text to an <see cref="double"/>
    ///  </summary>
    ///  <param name="s">The text to be converted</param>
    /// <param name="def">Value to return if not a valid Integer.</param>
    /// <returns>Value as a <see cref="double"/></returns>
    public static double ToDoubleOrDefault(this string s, double def = -1)
    {
        var r = double.TryParse(s, out var i);

        return r ? i : def;
    }

    public static string CollapseIntsToRanges(this IEnumerable<string> ints)
    {
        return ints.Where(s => s.IsInt()).Select(s => s.ToInt()).CollapseIntsToRanges();
    }

    public static string CollapseIntsToRanges(this IEnumerable<int> ints)
    {
        var r = string.Empty;
        var prev = -1;
        var current = -1;

        var sorted = ints.Distinct().OrderBy(i => i).ToList();

        if (sorted.Count > 1)
        {
            for (var i = 1; i < sorted.Count; i++)
            {
                prev = sorted[i - 1];
                current = sorted[i];

                if (prev + 1 == current) // you're at the start of a range.
                {
                    r += prev + "-";

                    while (prev + 1 == current)
                    {
                        prev = current;
                        if (i < sorted.Count - 1)
                            current = sorted[++i];
                        else
                            break;
                    }
                    r += prev + ", ";
                }
                else // its a single.
                {
                    r += prev + ", ";
                }
            }
        }
        else if (sorted.Count == 1)
        {
            r = sorted[0].ToString(CultureInfo.InvariantCulture);
        }

        if (prev != current)
            r += sorted[sorted.Count - 1];

        r = r.Trim().Trim(',');

        return r;
    }

    public static string CollapseLongsToRanges(this IEnumerable<string> ints)
    {
        return ints.Where(s => s.IsLong()).Select(s => s.ToLong()).CollapseLongsToRanges();
    }

    public static string CollapseLongsToRanges(this IEnumerable<long> ints)
    {
        var r = string.Empty;
        long prev = -1;
        long current = -1;

        var sorted = ints.Distinct().OrderBy(i => i).ToList();

        if (sorted.Count > 1)
        {
            for (var i = 1; i < sorted.Count; i++)
            {
                prev = sorted[i - 1];
                current = sorted[i];

                if (prev + 1 == current) // you're at the start of a range.
                {
                    r += prev + "-";

                    while (prev + 1 == current)
                    {
                        prev = current;
                        if (i < sorted.Count - 1)
                            current = sorted[++i];
                        else
                            break;
                    }
                    r += prev + ", ";
                }
                else // its a single.
                {
                    r += prev + ", ";
                }
            }
        }
        else if (sorted.Count == 1)
        {
            r = sorted[0].ToString(CultureInfo.InvariantCulture);
        }

        if (prev != current)
            r += sorted[sorted.Count - 1];

        r = r.Trim().Trim(',');

        return r;
    }

    public static IEnumerable<IntegerRange> CollapseIntsToIntegerRanges(this IEnumerable<int> ints)
    {
        var r = ints.Select(i => new Signed<int, IntMath>(i))
            .CollapseNumbersToRanges()
            .Select(i => new IntegerRange(i));
        return r;
    }

    public static IEnumerable<LongRange> CollapseLongsToLongRanges(this IEnumerable<long> ints)
    {
        return ints.Select(i => new Signed<long, LongMath>(i))
            .CollapseNumbersToRanges()
            .Select(i => new LongRange(i));
    }

    public static List<Range<T, C>> CollapseNumbersToRanges<T, C>(this IEnumerable<Signed<T, C>> ints)
        where T : IComparable<T>, IComparable, IFormattable, IConvertible, IEquatable<T>, new()
        where C : ISignedMath<T>, new()
    {
        var calc = new C();
        var r = new List<Range<T, C>>();
        Signed<T, C> prev = calc.Negate(calc.One);
        Signed<T, C> current = calc.Negate(calc.One);

        var sorted = ints.Distinct().OrderBy(i => i).ToList();

        if (sorted.Count > 1)
        {
            for (var i = 1; i < sorted.Count; i++)
            {
                prev = sorted[i - 1];
                current = sorted[i];

                if (prev + Signed<T, C>.One == current) // you're at the start of a range.
                {
                    var first = prev;

                    while (prev + Signed<T, C>.One == current)
                    {
                        prev = current;
                        if (i < sorted.Count - 1)
                        {
                            current = sorted[++i];
                        }
                        else { break; }
                    }
                    r.Add(new Range<T, C>(first, prev));
                }
                else // its a single.
                {
                    r.Add(new Range<T, C>(prev, prev));
                }
            }
        }
        else if (sorted.Count == 1)
        {
            r.Add(new Range<T, C>(sorted[0], sorted[0]));
        }

        if (prev != current)
        {
            r.Add(new Range<T, C>(sorted[sorted.Count - 1], sorted[sorted.Count - 1]));
        }

        return r;
    }

    public static bool NearlyEqual(this double a, double b, double epsilon)
    {
        var absA = Math.Abs(a);
        var absB = Math.Abs(b);
        var diff = Math.Abs(a - b);

        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (a == b)
        { // shortcut, handles infinities
            return true;
        }
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (a == 0 || b == 0 || diff < double.Epsilon)
        {
            // a or b is zero or both are extremely close to it
            // relative error is less meaningful here
            return diff < epsilon;
        }

        // use relative error
        return diff / (absA + absB) < epsilon;
    }

    private static bool IgnoreChar(char c)
    {
        return c < 33;
    }
}