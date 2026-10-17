using System;
using System.Collections.Generic;
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
        /// <param name="rng"></param>
        public Range(string rng)
        {
            if (rng.Contains('-'))
            {
                Start = Signed<T, C>.Parse(rng.Split('-')[0]);
                Stop = Signed<T, C>.Parse(rng.Split('-')[1]);
            }
            else if (rng.Contains(','))
            {
                Start = Signed<T, C>.Parse(rng.Split(',')[0]);
                Stop = Signed<T, C>.Parse(rng.Split(',')[1]);
            }
            else if (rng.Contains(':'))
            {
                Start = Signed<T, C>.Parse(rng.Split(':')[0]);
                Stop = Signed<T, C>.Parse(rng.Split(':')[1]);
            }
            else
            {
                Start = Signed<T, C>.Parse(rng.Split('-')[0]);
                Stop = Start;
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
            return Start == Stop ? Start.ToString() : Start + "-" + Stop;
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