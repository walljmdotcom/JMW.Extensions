using JMW.Extensions.Enumerable;
using JMW.Types;
using Lambda.Generic.Arithmetic;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace JMW.Collections.Tests
{
    [TestFixture]
    public class RangeCollectionTests
    {
        [Test]
        public void RangeTest1()
        {
            // arrange/act
            var foo = new IntegerRangeCollection("1-4,5,7,10,8, 12-15, 8-10, 19-34, 20-24, 45-50, 40-46");

            // assert
            Assert.That(foo.ToString(), Is.EqualTo("1-5,7-10,12-15,19-34,40-50"));
            Assert.That(foo.IsInRange(9), Is.EqualTo(true));
            Assert.That(foo.IsInRange(3), Is.EqualTo(true));
            Assert.That(foo.IsInRange(13), Is.EqualTo(true));
            Assert.That(foo.IsInRange(0), Is.EqualTo(false));

            Assert.That(foo.IsInRange("13"), Is.EqualTo(true));
            Assert.That(foo.IsInRange("0"), Is.EqualTo(false));

            Assert.That(foo.IsInRange(16), Is.EqualTo(false));
            Assert.That(foo.Contains(16), Is.EqualTo(false));
            Assert.That(foo.Contains(10), Is.EqualTo(true));
            Assert.That(foo.Contains("16"), Is.EqualTo(false));
            Assert.That(foo.Contains("10"), Is.EqualTo(true));
            Assert.That(new IntegerRange("8-12").Contains("10"), Is.EqualTo(true));
            Assert.That(new IntegerRange(new IntegerRange("8-12")).Contains("10"), Is.EqualTo(true));
        }

        [Test]
        public void RangeTest2()
        {
            var rng = new IntegerRange(2, 6);
            Assert.That(false, Is.EqualTo(rng.Contains(1)));
            Assert.That(true, Is.EqualTo(rng.Contains(2)));
            Assert.That(true, Is.EqualTo(rng.Contains(3)));
            Assert.That(true, Is.EqualTo(rng.Contains(4)));
            Assert.That(true, Is.EqualTo(rng.Contains(5)));
            Assert.That(true, Is.EqualTo(rng.Contains(6)));
            Assert.That(false, Is.EqualTo(rng.Contains(7)));

            Assert.That(true, Is.EqualTo(rng.Contains("5")));

            Assert.That("2-6", Is.EqualTo(rng.ToString()));
            Assert.That("2,3,4,5,6", Is.EqualTo(rng.GetNumbers().ToDelimitedString(',')));

            rng = new IntegerRange
            {
                Start = 2,
                Stop = 6
            };
            Assert.That(true, Is.EqualTo(rng.IntersectsWith("1,5")));
            Assert.That(true, Is.EqualTo(rng.IntersectsWith("3:5")));
            Assert.That(true, Is.EqualTo(rng.IntersectsWith("3-8")));
            Assert.That(true, Is.EqualTo(rng.Contains(new IntegerRange(3, 5))));
            Assert.That(true, Is.EqualTo(rng.Contains(new IntegerRange(2, 6))));
            Assert.That(false, Is.EqualTo(rng.Contains(new IntegerRange(3, 8))));
            Assert.That(false, Is.EqualTo(rng.Contains(new IntegerRange(1, 5))));
        }

        [TestCase(1, 10, 0, 20, false)]
        [TestCase(1, 10, 1, 11, false)]
        [TestCase(1, 10, 0, 10, false)]
        [TestCase(1, 10, 11, 20, false)]
        [TestCase(1, 10, -5, 0, false)]
        [TestCase(5, 5, 4, 6, false)]
        [TestCase(1, 10, 1, 10, true)]
        [TestCase(1, 10, 2, 9, true)]
        [TestCase(1, 10, 1, 1, true)]
        [TestCase(1, 10, 10, 10, true)]
        [TestCase(5, 5, 5, 5, true)]
        [TestCase(int.MinValue, int.MaxValue, 0, 0, true)]
        [TestCase(0, 0, int.MinValue, int.MaxValue, false)]
        public void RangeContainsRange(int start, int stop, int otherStart, int otherStop, bool expected)
        {
            var rng = new IntegerRange(start, stop);
            var other = new IntegerRange(otherStart, otherStop);

            Assert.That(rng.Contains(other), Is.EqualTo(expected));
            Assert.That(new LongRange(start, stop).Contains(new LongRange(otherStart, otherStop)), Is.EqualTo(expected));
        }

        [TestCase("1-10", "0-20", false)]
        [TestCase("1-10,15-20", "1-20", false)]
        [TestCase("1-10,15-20", "9-16", false)]
        [TestCase("1-10,15-20", "2-3", true)]
        [TestCase("1-10,15-20", "15-20", true)]
        [TestCase("1-10,15-20", "16", true)]
        [TestCase("1-10,15-20", "12", false)]
        public void RangeCollectionContainsRange(string collection, string range, bool expected)
        {
            var rngs = new IntegerRangeCollection(collection);

            Assert.That(rngs.Contains(range), Is.EqualTo(expected));
            Assert.That(rngs.Contains(new IntegerRange(range)), Is.EqualTo(expected));
        }

        [TestCase("", new[] { "10-12", "1-3" }, "1-3,10-12")]
        [TestCase("", new[] { "1-3", "10-12" }, "1-3,10-12")]
        [TestCase("", new[] { "20", "10-12", "1-3" }, "1-3,10-12,20")]
        [TestCase("", new[] { "5-6", "1-2", "5-7" }, "1-2,5-7")]
        [TestCase("", new[] { "1-3", "1-5" }, "1-5")]
        [TestCase("", new[] { "1-5", "1-3" }, "1-5")]
        [TestCase("", new[] { "4-6", "1-3" }, "1-6")]
        [TestCase("", new[] { "7" }, "7")]
        [TestCase("", new string[0], "")]
        [TestCase("5-6", new[] { "10-12", "1-3" }, "1-3,5-6,10-12")]
        [TestCase("5-6", new[] { "5-9", "5-7" }, "5-9")]
        [TestCase("5-6", new[] { "5-6" }, "5-6")]
        [TestCase("5-6,20-30", new[] { "25-40", "1-4" }, "1-6,20-40")]
        public void AddRangeUnsorted(string initial, string[] items, string expected)
        {
            var rngs = new IntegerRangeCollection(initial);
            rngs.AddRange(items);
            Assert.That(rngs.ToString(), Is.EqualTo(expected));

            var longRngs = new LongRangeCollection(initial);
            longRngs.AddRange(items);
            Assert.That(longRngs.ToString(), Is.EqualTo(expected));

            var added = new IntegerRangeCollection(initial);
            foreach (var item in items)
            {
                added.Add(new IntegerRange(item));
            }
            Assert.That(added.ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void AddRangeAtMaxValue()
        {
            var rngs = new IntegerRangeCollection();
            rngs.AddRange([new IntegerRange(10, int.MaxValue), new IntegerRange(20, 30)]);
            Assert.That(rngs.Count, Is.EqualTo(1));
            Assert.That(rngs.Ranges.Values[0].Start.ToString(CultureInfo.InvariantCulture), Is.EqualTo("10"));
            Assert.That(rngs.Ranges.Values[0].Stop.ToString(CultureInfo.InvariantCulture), Is.EqualTo(int.MaxValue.ToString(CultureInfo.InvariantCulture)));

            rngs.AddRange([new IntegerRange(int.MaxValue, int.MaxValue)]);
            Assert.That(rngs.Count, Is.EqualTo(1));
        }

        [TestCase("1:3", "1-3")]
        [TestCase("1:3,5", "1-3,5")]
        [TestCase("1-3,5:7", "1-3,5-7")]
        [TestCase("1-4,7", "1-4,7")]
        [TestCase("a,1-2", "1-2")]
        [TestCase("1-2, ,3", "1-3")]
        public void ConstructFromStringFormats(string input, string expected)
        {
            Assert.That(new IntegerRangeCollection(input).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void RangeTest3()
        {
            var rng = new LongRange(2, 6);
            Assert.That(false, Is.EqualTo(rng.Contains(1)));
            Assert.That(true, Is.EqualTo(rng.Contains(2)));
            Assert.That(true, Is.EqualTo(rng.Contains(3)));
            Assert.That(true, Is.EqualTo(rng.Contains(4)));
            Assert.That(true, Is.EqualTo(rng.Contains(5)));
            Assert.That(true, Is.EqualTo(rng.Contains(6)));
            Assert.That(false, Is.EqualTo(rng.Contains(7)));
            Assert.That(true, Is.EqualTo(rng.Contains("5")));
            Assert.That("2-6", Is.EqualTo(rng.ToString()));
            Assert.That("2,3,4,5,6", Is.EqualTo(rng.GetNumbers().ToDelimitedString(',')));

            rng = new LongRange
            {
                Start = 2,
                Stop = 6
            };
            Assert.That(true, Is.EqualTo(rng.IntersectsWith("1,5")));
            Assert.That(true, Is.EqualTo(rng.IntersectsWith("3:5")));
            Assert.That(true, Is.EqualTo(rng.IntersectsWith("3-8")));

            var rng1 = new LongRange(rng)
            {
                Stop = 11,
                Start = 6
            };
            Assert.That(false, Is.EqualTo(rng1.IntersectsWith("1,5")));
            Assert.That(true, Is.EqualTo(rng1.IntersectsWith("6:8")));
            Assert.That(true, Is.EqualTo(rng1.IntersectsWith("3-15")));
            Assert.That(false, Is.EqualTo(rng1.IntersectsWith("12-15")));
            Assert.That(false, Is.EqualTo(rng.IntersectsWith("0-1")));
            Assert.That(false, Is.EqualTo(rng.IntersectsWith("7-8")));

            try
            {
                _ = new IntegerRange("3,1");
            }
            catch (ArgumentException ex)
            {
                Assert.That("Stop cannot be less than Start (Parameter 'rng')", Is.EqualTo(ex.Message));
            }
        }

        [Test]
        public void RangeConstructors()
        {
            var foo = new IntegerRangeCollection("1-4,5,7,10,8, 12-15, 8-10, 19-34, 20-24, 45-50, 40-46");
            Assert.That(foo.ToString(), Is.EqualTo("1-5,7-10,12-15,19-34,40-50"));

            foo = new IntegerRangeCollection(new List<IntegerRange>{
                new(1,5),
                new(7,10),
                new(12,15),
                new(19,34),
                new(40,50)
            });
            Assert.That(foo.ToString(), Is.EqualTo("1-5,7-10,12-15,19-34,40-50"));

            var foo1 = new LongRangeCollection("1-4,5,7,10,8, 12-15, 8-10, 19-34, 20-24, 45-50, 40-46");
            Assert.That(foo1.ToString(), Is.EqualTo("1-5,7-10,12-15,19-34,40-50"));
            _ = new LongRangeCollection(new List<LongRange>{
                new(1,5),
                new(new Range<long, LongMath>(7,10)),
                new(12,15),
                new(19,34),
                new(40,50)
            });
            Assert.That(foo.ToString(), Is.EqualTo("1-5,7-10,12-15,19-34,40-50"));
        }

        [Test]
        public void TestAddIntegerRanges()
        {
            // arrange/act
            var foo = new IntegerRangeCollection("1-4,10,8, 12-15,19")
            {
                new IntegerRange("3-11")
            };

            // assert
            Assert.That(foo.ToString(), Is.EqualTo("1-15,19"));
            foo.Clear();
            Assert.That(0, Is.EqualTo(foo.Count));

            var foo1 = new LongRangeCollection("1-4,10,8, 12-15,19");
            foo1.AddRange(new List<LongRange> { new("3-9"), new("12-20") });

            // assert
            Assert.That(foo1.ToString(), Is.EqualTo("1-10,12-20"));
            Assert.That(foo1.Contains("1"), Is.True);
            Assert.That(foo1.Contains("2"), Is.True);
            Assert.That(foo1.Contains("3"), Is.True);
            Assert.That(foo1.Contains("4"), Is.True);
            Assert.That(foo1.Contains("5"), Is.True);
            Assert.That(foo1.Contains("6"), Is.True);
            Assert.That(foo1.Contains("7"), Is.True);
            Assert.That(foo1.Contains("8"), Is.True);
            Assert.That(foo1.Contains("9"), Is.True);
            Assert.That(foo1.Contains("10"), Is.True);
            Assert.That(foo1.Contains("12"), Is.True);
            Assert.That(foo1.Contains("13"), Is.True);
            Assert.That(foo1.Contains("14"), Is.True);
            Assert.That(foo1.Contains("15"), Is.True);
            Assert.That(foo1.Contains("16"), Is.True);
            Assert.That(foo1.Contains("17"), Is.True);
            Assert.That(foo1.Contains("18"), Is.True);
            Assert.That(foo1.Contains("19"), Is.True);
            Assert.That(foo1.Contains("20"), Is.True);

            Assert.That(foo1.Contains("11"), Is.False);
            Assert.That(foo1.Contains("21"), Is.False);
            Assert.That("1,2,3,4,5,6,7,8,9,10,12,13,14,15,16,17,18,19,20", Is.EqualTo(foo1.GetNumbers().ToDelimitedString(",")));
            Assert.That("1,2,3,4,5,6,7,8,9,10,12,13,14,15,16,17,18,19,20", Is.EqualTo(IntegerRangeCollection.ExplodeRange("1-10,12-20").ToDelimitedString(",")));
            Assert.That(true, Is.EqualTo(foo1.IntersectsWith(new LongRange(0, 8))));
            Assert.That(true, Is.EqualTo(foo1.IntersectsWith(new LongRange(0, 30))));
            Assert.That(true, Is.EqualTo(foo1.IntersectsWith(new LongRange(17, 30))));
            Assert.That(true, Is.EqualTo(foo1.IntersectsWith(new LongRange(0, 5))));
            Assert.That(true, Is.EqualTo(foo1.IntersectsWith(new LongRange(5, 15))));
            Assert.That(false, Is.EqualTo(foo1.IntersectsWith(new LongRange(21, 22))));
            Assert.That(false, Is.EqualTo(foo1.IntersectsWith(new LongRange(-3, 0))));

            foo = new IntegerRangeCollection("1-4,10,8, 12-15,19");
            foo.AddRange(new List<string> { "3-6", "11", "35" });

            // assert
            Assert.That(foo.ToString(), Is.EqualTo("1-6,8,10-15,19,35"));

            foo = new IntegerRangeCollection("1-4,10,8, 12-15,19");
            foo.AddRange(new List<IntegerRange> { new("3-11"), new("13-14"), new("19-21") });

            // assert
            Assert.That(foo.ToString(), Is.EqualTo("1-15,19-21"));
        }
    }
}