using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JMW.Extensions;
using JMW.Types;
using Lambda.Generic.Arithmetic;

namespace JMW.Collections
{
    public class LongRangeCollection : RangeCollection<long, LongMath>
    {
        public LongRangeCollection()
        {
        }

        public LongRangeCollection(IEnumerable<Range<long, LongMath>> rng) : base(rng)
        {
        }

        public LongRangeCollection(Range<long, LongMath> rng) : base(rng)
        {
        }

        public LongRangeCollection(Signed<long, LongMath> start, Signed<long, LongMath> stop) : base(start, stop)
        {
        }

        public LongRangeCollection(string rng) : base(rng)
        {
        }

        public LongRangeCollection(IEnumerable<string> rng) : base(rng)
        {
        }

        public LongRangeCollection(IEnumerable<Signed<long, LongMath>> rng) : base(rng)
        {
        }
    }

    public class IntegerRangeCollection : RangeCollection<int, IntMath>
    {
        public IntegerRangeCollection()
        {
        }

        public IntegerRangeCollection(IEnumerable<Range<int, IntMath>> rng) : base(rng)
        {
        }

        public IntegerRangeCollection(Range<int, IntMath> rng) : base(rng)
        {
        }

        public IntegerRangeCollection(Signed<int, IntMath> start, Signed<int, IntMath> stop) : base(start, stop)
        {
        }

        public IntegerRangeCollection(string rng) : base(rng)
        {
        }

        public IntegerRangeCollection(IEnumerable<string> rng) : base(rng)
        {
        }

        public IntegerRangeCollection(IEnumerable<Signed<int, IntMath>> rng) : base(rng)
        {
        }
    }

    public class RangeCollection<T, C> : IList<Range<T, C>>
        where T : IComparable<T>, IComparable, IFormattable, IConvertible, IEquatable<T>, new()
        where C : ISignedMath<T>, new()
    {
        #region Constructors

        public RangeCollection()
        {
        }

        public RangeCollection(IEnumerable<Range<T, C>> rng)
        {
            collapse(rng.OrderBy(r => r.Start).ToList()).Each(i => Ranges.Add(i.Start, i)); // add them to the range.
        }

        public RangeCollection(Range<T, C> rng)
        {
            Ranges.Add(rng.Start, new Range<T, C>(rng));
        }

        public RangeCollection(Signed<T, C> start, Signed<T, C> stop)
        {
            Ranges.Add(start, new Range<T, C>(start, stop));
        }

        public RangeCollection(string rng)
        {
            getCleanNumbers(rng.Split(',')).Each(i => Ranges.Add(i.Start, i)); // add them to the range.
        }

        public RangeCollection(IEnumerable<string> rng)
        {
            getCleanNumbers(rng).Each(i => Ranges.Add(i.Start, i)); // add them to the range.
        }

        public RangeCollection(IEnumerable<Signed<T, C>> rng)
        {
            rng.CollapseNumbersToRanges() // turn back into objects
               .Each(i => Ranges.Add(i.Start, i)); // add them to the range.
        }

        private static IEnumerable<Range<T, C>> getCleanNumbers(IEnumerable<string> rng)
        {
            var rngs = rng.Select(o => o.Trim()) // trim whitespace
                .Where(o => o.Length > 0 && (o.Contains('-') || o.Contains(':') || o.IsIntFast())) // eliminate empty strings
                .Select(o => new Range<T, C>(o))// create ranges
                .OrderBy(r => r.Start)
                .ToList();
            return collapse(rngs);
        }

        #endregion Constructors

        #region Properties

        public SortedList<Signed<T, C>, Range<T, C>> Ranges { get; set; } = [];

        #endregion Properties

        #region Public Methods

        public void Add(Range<T, C> item)
        {
            ArgumentNullException.ThrowIfNull(item);

            var start = item.Start;
            var stop = item.Stop;
            var merged = false;

            // the only earlier range that can touch the item is the last one starting at or before it
            var i = lastStartingAtOrBefore(start);
            if (i >= 0 && touches(Ranges.Values[i].Stop, start))
            {
                start = Ranges.Values[i].Start;
                if (Ranges.Values[i].Stop > stop) stop = Ranges.Values[i].Stop;
                Ranges.RemoveAt(i);
                merged = true;
            }
            else
            {
                i++;
            }

            while (i < Ranges.Count && touches(stop, Ranges.Values[i].Start))
            {
                if (Ranges.Values[i].Stop > stop) stop = Ranges.Values[i].Stop;
                Ranges.RemoveAt(i);
                merged = true;
            }

            Ranges.Add(start, merged ? new Range<T, C>(start, stop) : item);
        }

        public void AddRange(IEnumerable<Range<T, C>> items)
        {
            var all = collapse(Ranges.Values.Concat(items).OrderBy(r => r.Start).ToList());
            Ranges.Clear();
            Ranges.Capacity = all.Count;
            foreach (var r in all)
            {
                Ranges.Add(r.Start, r);
            }
        }

        public void AddRange(IEnumerable<string> items)
        {
            AddRange(items.Select(i => new Range<T, C>(i)));
        }

        /// <summary>
        /// Returns true if the provided int is included in the ranges.
        /// </summary>
        /// <param name="num">number to check</param>
        public bool Contains(Signed<T, C> num)
        {
            return IsInRange(num);
        }

        /// <summary>
        /// Returns true if the provided number or range is included in the ranges.
        /// </summary>
        /// <param name="num">number to check</param>
        public bool Contains(string num)
        {
            return Contains(new Range<T, C>(num));
        }

        /// <summary>
        /// Returns true if the provided Range is included in the ranges.
        /// </summary>
        /// <param name="item">number to check</param>
        public bool Contains(Range<T, C> item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return Ranges.Values.Any(i => i.Contains(item));
        }

        /// <summary>
        /// Returns true if the provided int is included in the ranges.
        /// </summary>
        /// <param name="num">number to check</param>
        public bool IsInRange(Signed<T, C> num)
        {
            var lo = 0;
            var hi = Ranges.Count - 1;
            while (lo <= hi)
            {
                var i = lo + ((hi - lo) >> 1);
                var order = Ranges.Values[i].IsInRange(num) ? 0 : Ranges.Values[i].Start.CompareTo(num);

                if (order == 0) return true;
                if (order < 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the provided int is included in the ranges.
        /// </summary>
        /// <param name="num">number to check</param>
        public bool IsInRange(string num)
        {
            return IsInRange(Signed<T, C>.Parse(num));
        }

        public string RangeToString()
        {
            return Ranges.Values.Select(o => o.ToString()).ToDelimitedString(',');
        }

        public bool IntersectsWith(Range<T, C> rng)
        {
            return Ranges.Any(o => o.Value.IntersectsWith(rng));
        }

        public IEnumerable<Signed<T, C>> GetNumbers()
        {
            return Ranges.Values.SelectMany(o => o.GetNumbers()).OrderBy(o => o);
        }

        public override string ToString()
        {
            return RangeToString();
        }

        #endregion Public Methods

        #region Public Static

        public static IEnumerable<Signed<T, C>> ExplodeRange(string rng)
        {
            var lst = new RangeCollection<T, C>(rng);
            return lst.GetNumbers();
        }

        #endregion Public Static

        #region IList

        IEnumerator<Range<T, C>> IEnumerable<Range<T, C>>.GetEnumerator()
        {
            return Ranges.Values.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)Ranges.Values).GetEnumerator();
        }

        public void CopyTo(Array array, Signed<T, C> index)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Range<T, C> item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return Ranges.Remove(item.Start);
        }

        public int Count => Ranges.Count;

        public object SyncRoot => ((ICollection)Ranges.Values).SyncRoot;

        public bool IsSynchronized => ((ICollection)Ranges.Values).IsSynchronized;

        public void Clear()
        {
            Ranges.Clear();
        }

        public void CopyTo(Range<T, C>[] array, int arrayIndex)
        {
            Ranges.Values.CopyTo(array, arrayIndex);
        }

        public int IndexOf(Range<T, C> item)
        {
            return Ranges.Values.IndexOf(item);
        }

        public void Insert(int index, Range<T, C> item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            Ranges.RemoveAt(index);
        }

        Range<T, C> IList<Range<T, C>>.this[int index]
        {
            get { return Ranges.Values[index]; }
            set { Ranges.RemoveAt(index); Add(value); }
        }

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        #endregion IList

        // A range ending at stop overlaps or adjoins one beginning at start. stop + One wraps when stop is the type's
        // MaxValue, so overlap is tested separately.
        private static bool touches(Signed<T, C> stop, Signed<T, C> start)
        {
            return stop >= start || stop + Signed<T, C>.One == start;
        }

        // Merges overlapping and adjoining ranges in a list sorted by Start.
        private static List<Range<T, C>> collapse(List<Range<T, C>> sorted)
        {
            var result = new List<Range<T, C>>(sorted.Count);
            foreach (var next in sorted)
            {
                var last = result.Count - 1;
                if (last >= 0 && touches(result[last].Stop, next.Start))
                {
                    if (result[last].Stop < next.Stop)
                        result[last] = new Range<T, C>(result[last].Start, next.Stop);
                }
                else
                {
                    result.Add(next);
                }
            }

            return result;
        }

        private int lastStartingAtOrBefore(Signed<T, C> start)
        {
            var keys = Ranges.Keys;
            var lo = 0;
            var hi = keys.Count - 1;
            while (lo <= hi)
            {
                var mid = lo + ((hi - lo) >> 1);
                if (keys[mid] <= start)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            return hi;
        }
    }
}