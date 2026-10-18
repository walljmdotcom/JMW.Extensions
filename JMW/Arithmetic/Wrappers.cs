// Copyright (c) 2004, Rüdiger Klaehn
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of lambda computing nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

using System;

namespace Lambda.Generic.Arithmetic
{
    #region Signed Wrapper

    public readonly struct Signed<T, O>
        : IComparable<Signed<T, O>>, IComparable<T>, IComparable, IFormattable, IConvertible, IEquatable<Signed<T, O>>, IEquatable<T>
        where O : ISignedMath<T>, new()
        where T : IComparable<T>, IComparable, IFormattable, IConvertible, IEquatable<T>
    {
        private static readonly O o = new();
        internal readonly T value;

        public Signed(T a)
        {
            value = a;
        }

        public static explicit operator Signed<T, O>(ulong a)
        {
            return o.ConvertFrom(a);
        }

        public static explicit operator Signed<T, O>(long a)
        {
            return o.ConvertFrom(a);
        }

        public static implicit operator Signed<T, O>(T a)
        {
            return new Signed<T, O>(a);
        }

        public static implicit operator T(Signed<T, O> a)
        {
            return a.value;
        }

        public static Signed<T, O> operator +(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Add(a, b);
        }

        public static Signed<T, O> operator -(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Subtract(a, b);
        }

        public static Signed<T, O> operator -(Signed<T, O> a)
        {
            return o.Negate(a);
        }

        public static Signed<T, O> operator *(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Multiply(a, b);
        }

        public static Signed<T, O> operator /(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Divide(a, b);
        }

        public static Signed<T, O> Zero
        {
            get { return o.Zero; }
        }

        public static Signed<T, O> One
        {
            get { return o.One; }
        }

        public static Signed<T, O> Parse(string s)
        {
            return o.ConvertFrom(s);
        }

        public static bool operator ==(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Equals(a, b);
        }

        public static bool operator !=(Signed<T, O> a, Signed<T, O> b)
        {
            return !o.Equals(a, b);
        }

        public static bool operator <=(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Compare(a, b) <= 0;
        }

        public static bool operator >=(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Compare(a, b) >= 0;
        }

        public static bool operator <(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Compare(a, b) < 0;
        }

        public static bool operator >(Signed<T, O> a, Signed<T, O> b)
        {
            return o.Compare(a, b) > 0;
        }

        // The Signed overloads let the default comparers, as in SortedList and Dictionary keys, compare without boxing.
        public readonly int CompareTo(Signed<T, O> other)
        {
            return value.CompareTo(other.value);
        }

        public readonly int CompareTo(T other)
        {
            return value.CompareTo(other);
        }

        public readonly bool Equals(Signed<T, O> other)
        {
            return value.Equals(other.value);
        }

        public readonly bool Equals(T other)
        {
            return value.Equals(other);
        }

        public override readonly bool Equals(object? a)
        {
            return a switch
            {
                Signed<T, O> _ => value.Equals(((Signed<T, O>)a).value),
                T _ => value.Equals((T)a),
                _ => false,
            };
        }

        public override readonly int GetHashCode()
        {
            return o.GetHashCode(value);
        }

        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            return value.ToString(format, formatProvider);
        }

        public override readonly string ToString()
        {
            return value.ToString() ?? string.Empty;
        }

        public readonly int CompareTo(object? obj)
        {
            return obj switch
            {
                Signed<T, O> _ => value.CompareTo(((Signed<T, O>)obj).value),
                T _ => value.CompareTo((T)obj),
                _ => -1,
            };
        }

        #region Implementation of IConvertible

        public readonly TypeCode GetTypeCode()
        {
            return value.GetTypeCode();
        }

        public readonly bool ToBoolean(IFormatProvider? provider)
        {
            return value.ToBoolean(provider);
        }

        public readonly char ToChar(IFormatProvider? provider)
        {
            return value.ToChar(provider);
        }

        public readonly sbyte ToSByte(IFormatProvider? provider)
        {
            return value.ToSByte(provider);
        }

        public readonly byte ToByte(IFormatProvider? provider)
        {
            return value.ToByte(provider);
        }

        public readonly short ToInt16(IFormatProvider? provider)
        {
            return value.ToInt16(provider);
        }

        public readonly ushort ToUInt16(IFormatProvider? provider)
        {
            return value.ToUInt16(provider);
        }

        public readonly int ToInt32(IFormatProvider? provider)
        {
            return value.ToInt32(provider);
        }

        public readonly uint ToUInt32(IFormatProvider? provider)
        {
            return value.ToUInt32(provider);
        }

        public readonly long ToInt64(IFormatProvider? provider)
        {
            return value.ToInt64(provider);
        }

        public readonly ulong ToUInt64(IFormatProvider? provider)
        {
            return value.ToUInt64(provider);
        }

        public readonly float ToSingle(IFormatProvider? provider)
        {
            return value.ToSingle(provider);
        }

        public readonly double ToDouble(IFormatProvider? provider)
        {
            return value.ToDouble(provider);
        }

        public readonly decimal ToDecimal(IFormatProvider? provider)
        {
            return value.ToDecimal(provider);
        }

        public readonly DateTime ToDateTime(IFormatProvider? provider)
        {
            return value.ToDateTime(provider);
        }

        public readonly string ToString(IFormatProvider? provider)
        {
            return value.ToString(provider);
        }

        public readonly object ToType(Type conversionType, IFormatProvider? provider)
        {
            return value.ToType(conversionType, provider);
        }

        #endregion Implementation of IConvertible
    }

    #endregion Signed Wrapper
}