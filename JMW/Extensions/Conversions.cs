/*
The MIT License (MIT)
Copyright (c) 2016 Jason Wall

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using JMW.Functional;
using System;

namespace JMW.Extensions.Conversions;

public static class Extensions
{
    /// <summary>
    /// Converts a string into an <typeref name="short"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<short> ToInt16(this string s)
    {
        short v;
        return short.TryParse(s, out v)
            ? new Maybe<short>(v)
            : new Maybe<short>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="ushort"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<ushort> ToUInt16(this string s)
    {
        ushort v;
        return ushort.TryParse(s, out v)
            ? new Maybe<ushort>(v)
            : new Maybe<ushort>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="int"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<int> ToInt32(this string s)
    {
        int v;
        return int.TryParse(s, out v) ? new Maybe<int>(v) : new Maybe<int>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="uint"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<uint> ToUInt32(this string s)
    {
        uint v;
        return uint.TryParse(s, out v) ? new Maybe<uint>(v) : new Maybe<uint>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="long"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<long> ToInt64(this string s)
    {
        long v;
        return long.TryParse(s, out v) ? new Maybe<long>(v) : new Maybe<long>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="ulong"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<ulong> ToUInt64(this string s)
    {
        ulong v;
        return ulong.TryParse(s, out v)
            ? new Maybe<ulong>(v)
            : new Maybe<ulong>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="decimal"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<decimal> ToDecimal(this string s)
    {
        decimal v;
        return decimal.TryParse(s, out v)
            ? new Maybe<decimal>(v)
            : new Maybe<decimal>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="float"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<float> ToFloat(this string s)
    {
        float v;
        return float.TryParse(s, out v)
            ? new Maybe<float>(v)
            : new Maybe<float>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into an <typeref name="double"/> safely.
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<double> ToDouble(this string s)
    {
        double v;
        return double.TryParse(s, out v)
            ? new Maybe<double>(v)
            : new Maybe<double>(new ArgumentException("Argument " + s + " is not a number."));
    }

    /// <summary>
    /// Converts a string into a <typeref name="bool"/> safely, and intuitively.
    ///
    ///  true/false
    ///  0/1
    ///  yes/no
    ///  on/off
    ///  y/n
    ///  t/f
    /// </summary>
    /// <param name="s">String to convert.</param>
    public static Maybe<bool> ToBoolean(this string s)
    {
        bool v;
        if (bool.TryParse(s, out v))
        {
            return new Maybe<bool>(v);
        }
        var s1 = s.ToLowerInvariant().Trim();
        if (s1 == "yes" || s1 == "1" || s1 == "on" || s1 == "t" || s1 == "y") return new Maybe<bool>(true);
        return s1 == "no" || s1 == "0" || s1 == "off" || s1 == "f" || s1 == "n"
            ? new Maybe<bool>(false)
            : new Maybe<bool>(new ArgumentException("Argument " + s + " is not a true/false, yes/no, 0/1, or on/off."));
    }
}