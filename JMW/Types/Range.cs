using System;
using System.Collections.Generic;
using System.Globalization;
using Lambda.Generic.Arithmetic;

namespace JMW.Types
{
    public class LongRange : Range<long, LongMath>
    {
        public LongRange()
        {
        }

        public LongRange(Range<long, LongMath> rng) : base(rng)
        {
        }

        public LongRange(LongRange rng) : base(rng)
        {
        }

        public LongRange(long start, long stop) : base(start, stop)
        {
        }

        public LongRange(string rng) : base(rng)
        {
        }
    }

    public class IntegerRange : Range<int, IntMath>
    {
        public IntegerRange()
        {
        }

        public IntegerRange(Range<int, IntMath> rng) : base(rng)
        {
        }

        public IntegerRange(IntegerRange rng) : base(rng)
        {
        }

        public IntegerRange(int start, int stop) : base(start, stop)
        {
        }

        public IntegerRange(string rng) : base(rng)
        {
        }
    }

    public class Range<T, C>
        where T : IComparable<T>, IComparable, IFormattable, IConvertible, IEquatable<T>, new()
        where C : ISignedMath<T>, new()
    {
        /// <summary>
        /// An instance of the calculator. We will use this to perform the calculations.
        /// </summary>
        private static C calc = new();

        #region Constructors

        public Range()
        {
        }

        public Range(Range<T, C> rng)
        {
            Start = rng.Start;
            Stop = rng.Stop;
        }

        public Range(T start, T stop)
        {
            Start = start;
            Stop = stop;
        }

        /// <summary>
        /// Creates a range from a string.
        /// </summary>
        /// <example>1-3</example>
        /// <example>1,3</example>
        /// <example>1:3</example>
        /// <example>-5--3</example>
        /// <remarks>
        /// A '-' at the start of the text, or straight after the '-' separator, is a sign, so "-5--3" is -5 to -3
        /// and "1--3" is 1 to -3, which is rejected because Start is greater than Stop.
        /// More than two parts, such as "1,3,5" or "1-3-5", is rejected; a list of ranges is a RangeCollection.
        /// </remarks>
        /// <param name="rng"></param>
        public Range(string rng)
        {
            const string tooManyParts =
                "A range has at most a start and a stop. Use RangeCollection for a list such as \"1-3,5\".";
            var text = rng.Trim();
            var separatorChar = text.Contains(',') ? ',' : text.Contains(':') ? ':' : '-';
            int separator;
            int stopAt;
            if (separatorChar == '-')
            {
                // skip index 0 so a leading sign is not taken for the separator, and allow a sign after it
                separator = text.Length > 1 ? text.IndexOf('-', 1) : -1;
                stopAt = separator + 2;
            }
            else
            {
                separator = text.IndexOf(separatorChar);
                stopAt = separator + 1;
            }

            if (separator < 0)
            {
                Start = Signed<T, C>.Parse(text);
                Stop = Start;
            }
            else if (stopAt < text.Length && text.IndexOf(separatorChar, stopAt) >= 0)
            {
                throw new ArgumentException(tooManyParts, nameof(rng));
            }
            else
            {
                Start = Signed<T, C>.Parse(text.Substring(0, separator));
                Stop = Signed<T, C>.Parse(text.Substring(separator + 1));
            }

            if (Start > Stop)
                throw new ArgumentException("Stop cannot be less than Start", nameof(rng));
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The start of the range.
        /// </summary>
        public Signed<T, C> Start { get; set; } = calc.NegativeOne;

        /// <summary>
        /// The end of the range
        /// </summary>
        public Signed<T, C> Stop { get; set; } = calc.NegativeOne;

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Indictes if the provided number is within the range.
        /// </summary>
        /// <param name="num">Number to evaluate.</param>
        /// <returns><see cref="bool"/></returns>
        public bool Contains(Signed<T, C> num)
        {
            return IsInRange(num);
        }

        /// <summary>
        /// Indictes if the provided number or range is within the range.
        /// </summary>
        /// <param name="num">Number or Range (1 || 1-4) to evaluate.</param>
        /// <returns><see cref="bool"/></returns>
        public bool Contains(string num)
        {
            return Contains(new Range<T, C>(num));
        }

        /// <summary>
        /// Indictes if the provided number is within the range.
        /// </summary>
        /// <param name="rng">Number to evaluate.</param>
        /// <returns><see cref="bool"/></returns>
        public bool Contains(Range<T, C> rng)
        {
            return rng.Start >= Start && rng.Stop <= Stop;
        }

        /// <summary>
        /// Indictes if the provided number is within the range.
        /// </summary>
        /// <param name="num">Number to evaluate.</param>
        /// <returns><see cref="bool"/></returns>
        public bool IsInRange(Signed<T, C> num)
        {
            return num >= Start && num <= Stop;
        }

        /// <summary>
        /// Indictes if the provided number is within the range.
        /// </summary>
        /// <param name="num">Number to evaluate.</param>
        /// <returns><see cref="bool"/></returns>
        public bool IsInRange(string num)
        {
            return IsInRange(Signed<T, C>.Parse(num));
        }

        /// <summary>
        /// Converts the range to a string.  ex: 6-7,8,9-10
        /// </summary>
        public string RangeToString()
        {
            return Start == Stop
                ? Start.ToString(CultureInfo.InvariantCulture)
                : Start.ToString(CultureInfo.InvariantCulture) + "-" + Stop.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Indicates if the provided range intersects with this range.
        /// </summary>
        /// <param name="rng">Range to evaluate</param>
        /// <returns><see cref="bool"/></returns>
        public bool IntersectsWith(string rng)
        {
            return IntersectsWith(new Range<T, C>(rng));
        }

        /// <summary>
        /// Indicates if the provided range intersects with this range.
        /// </summary>
        /// <param name="rng">Range to evaluate</param>
        /// <returns><see cref="bool"/></returns>
        public bool IntersectsWith(Range<T, C> rng)
        {
            if (rng.Start >= Start && rng.Start <= Stop) return true;
            if (Start >= rng.Start && Start <= rng.Stop) return true;

            if (rng.Stop >= Start && rng.Stop <= Stop) return true;
            return Stop >= rng.Start && Stop <= rng.Stop;
        }

        /// <summary>
        /// Returns all the numbers in the range.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Signed<T, C>> GetNumbers()
        {
            for (var i = Start; i <= Stop; i = i + Signed<T, C>.One)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Converts the range to a string.  ex: 6-7,8,9-10
        /// </summary>
        public override string ToString()
        {
            return RangeToString();
        }

        #endregion Public Methods
    }
}