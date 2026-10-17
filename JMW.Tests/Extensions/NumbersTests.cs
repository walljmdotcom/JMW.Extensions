using JMW.Types;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JMW.Extensions.Tests
{
    [TestFixture]
    public class NumbersTests
    {
        [Test]
        public void IsIntTest()
        {
            Assert.That("1".IsInt(), Is.True);
            Assert.That("11234".IsInt(), Is.True);
            Assert.That("4321341".IsInt(), Is.True);
            Assert.That("1431234121".IsInt(), Is.True);
            Assert.That("143.1234121".IsInt(), Is.False);
            Assert.That("14a".IsInt(), Is.False);
        }

        [TestCase("0", true)]
        [TestCase("9", true)]
        [TestCase("1234567890", true)]
        [TestCase(" 12 ", true)]
        [TestCase(":", false)]
        [TestCase(";", false)]
        [TestCase("@", false)]
        [TestCase("A", false)]
        [TestCase("G", false)]
        [TestCase("1:3", false)]
        [TestCase("12A", false)]
        [TestCase("1/", false)]
        [TestCase("1.5", false)]
        [TestCase("-1", false)]
        [TestCase("a", false)]
        [TestCase("", false)]
        [TestCase(" ", false)]
        [TestCase("\t\n", false)]
        [TestCase("1 2", false)]
        [TestCase(" 1 2 ", false)]
        [TestCase("1\t2", false)]
        [TestCase("\t12\n", true)]
        [TestCase("0", true)]
        [TestCase("000", true)]
        [TestCase("2147483647", true)]
        [TestCase(" 2147483647 ", true)]
        [TestCase("0002147483647", true)]
        [TestCase("2147483648", false)]
        [TestCase("3000000000", false)]
        [TestCase("99999999999", false)]
        public void IsIntFastTest(string input, bool expected)
        {
            Assert.That(input.IsIntFast(), Is.EqualTo(expected));
        }

        [Test]
        public void IsDoubleTest()
        {
            Assert.That("1".IsDouble(), Is.True);
            Assert.That("11234".IsDouble(), Is.True);
            Assert.That("4321341".IsDouble(), Is.True);
            Assert.That("1123412341234".IsDouble(), Is.True);
            Assert.That("1431234121".IsDouble(), Is.True);
            Assert.That("143.1234121".IsDouble(), Is.True);
            Assert.That("14a".IsDouble(), Is.False);
        }

        [Test]
        public void ToDoubleTest()
        {
            Assert.That("1".ToDouble(), Is.EqualTo((double)1));
            Assert.That("11234".ToDouble(), Is.EqualTo((double)11234));
            Assert.That("4321341".ToDouble(), Is.EqualTo((double)4321341));
            Assert.That("1431234121".ToDouble(), Is.EqualTo((double)1431234121));

            Assert.That("1431234121aasdf".ToDoubleOrNeg1(), Is.EqualTo(-1));
            Assert.That("1431234121aasdf".ToDoubleOrDefault(), Is.EqualTo(-1));

            Assert.That("1431".ToDoubleOrNeg1(), Is.EqualTo(1431));
            Assert.That("1431".ToDoubleOrDefault(), Is.EqualTo(1431));

            Assert.That(1.ToDouble(), Is.EqualTo((double)1));
            Assert.That(11234.ToDouble(), Is.EqualTo((double)11234));
            Assert.That(4321341.ToDouble(), Is.EqualTo((double)4321341));
            Assert.That(1431234121.ToDouble(), Is.EqualTo((double)1431234121));

            Assert.That(((long)1).ToDouble(), Is.EqualTo((double)1));
            Assert.That(((long)11234).ToDouble(), Is.EqualTo((double)11234));
            Assert.That(((long)4321341).ToDouble(), Is.EqualTo((double)4321341));
            Assert.That(((long)1431234121).ToDouble(), Is.EqualTo((double)1431234121));
        }

        [Test]
        public void ToLongTest()
        {
            Assert.That("1".ToLong(), Is.EqualTo((long)1));
            Assert.That("11234".ToLong(), Is.EqualTo((long)11234));
            Assert.That("4321341".ToLong(), Is.EqualTo((long)4321341));
            Assert.That("1431234121".ToLong(), Is.EqualTo((long)1431234121));

            Assert.That(((decimal)1).ToLong(), Is.EqualTo((long)1));
            Assert.That(((decimal)11234).ToLong(), Is.EqualTo((long)11234));
            Assert.That(((decimal)4321341).ToLong(), Is.EqualTo((long)4321341));
            Assert.That(((decimal)1431234121).ToLong(), Is.EqualTo((long)1431234121));

            Assert.That(((double)1).ToLong(), Is.EqualTo((long)1));
            Assert.That(((double)11234).ToLong(), Is.EqualTo((long)11234));
            Assert.That(((double)4321341).ToLong(), Is.EqualTo((long)4321341));
            Assert.That(((double)1431234121).ToLong(), Is.EqualTo((long)1431234121));

            Assert.That("1".ToInt64(), Is.EqualTo((long)1));
            Assert.That("11234".ToInt64(), Is.EqualTo((long)11234));
            Assert.That("4321341".ToInt64(), Is.EqualTo((long)4321341));
            Assert.That("1431234121".ToInt64(), Is.EqualTo((long)1431234121));

            Assert.That("1431234121asdf".ToInt64OrNeg1(), Is.EqualTo(-1));
            Assert.That("143123412000001b".ToInt64OrNeg1(), Is.EqualTo(-1));
            Assert.That("1431234121".ToInt64OrNeg1(), Is.EqualTo(1431234121));
            Assert.That("1k".ToInt64OrNeg1(), Is.EqualTo(1000));
            Assert.That("1k".ToInt64(), Is.EqualTo(1000));
        }

        [TestCase("8.2m", 8200000L)]
        [TestCase("8.2M", 8200000L)]
        [TestCase("1k", 1000L)]
        [TestCase("1.5k", 1500L)]
        [TestCase("0.001k", 1L)]
        [TestCase("-1.5k", -1500L)]
        [TestCase("4.7g", 4700000000L)]
        [TestCase("4.7b", 4700000000L)]
        [TestCase("1.1t", 1100000000000L)]
        [TestCase("2.3p", 2300000000000000L)]
        [TestCase("9223.372036854775807p", long.MaxValue)]
        [TestCase("0k", 0L)]
        [TestCase("1,000k", 1000000L)]
        [TestCase("12", 12L)]
        [TestCase("-12", -12L)]
        [TestCase("9223372036854775807", long.MaxValue)]
        [TestCase("-9223372036854775808", long.MinValue)]
        public void ToInt64Abbreviated(string input, long expected)
        {
            Assert.That(input.ToInt64(), Is.EqualTo(expected));
            Assert.That(input.ToInt64orDefault(42), Is.EqualTo(expected));
        }

        [TestCase("1.5k", 1500L)]
        [TestCase("8.2m", 8200000L)]
        [TestCase("1,000k", 1000000L)]
        [TestCase("12", 12L)]
        [SetCulture("de-DE")]
        public void ToInt64IgnoresCurrentCulture(string input, long expected)
        {
            Assert.That(input.ToInt64(), Is.EqualTo(expected));
            Assert.That(input.ToInt64orDefault(), Is.EqualTo(expected));
        }

        [TestCase("1.5", 1.5)]
        [TestCase("1,000.5", 1000.5)]
        [TestCase("-2", -2.0)]
        [SetCulture("de-DE")]
        public void ToDoubleIgnoresCurrentCulture(string input, double expected)
        {
            Assert.That(input.ToDouble(), Is.EqualTo(expected));
        }

        [TestCase("", typeof(FormatException))]
        [TestCase("k", typeof(FormatException))]
        [TestCase("x", typeof(FormatException))]
        [TestCase("1.5", typeof(FormatException))]
        [TestCase("1.0005k", typeof(FormatException))]
        [TestCase("8.2x", typeof(FormatException))]
        [TestCase("abck", typeof(FormatException))]
        [TestCase("9223.372036854775808p", typeof(OverflowException))]
        [TestCase("143123412000001b", typeof(OverflowException))]
        [TestCase("1e20p", typeof(OverflowException))]
        [TestCase("-1e20p", typeof(OverflowException))]
        [TestCase("9223372036854775808", typeof(OverflowException))]
        public void ToInt64Invalid(string input, Type exception)
        {
            Assert.That(() => input.ToInt64(), Throws.TypeOf(exception));
            Assert.That(input.ToInt64orDefault(42), Is.EqualTo(42));
        }

        [TestCase("8.2m", 8200000)]
        [TestCase("8.2M", 8200000)]
        [TestCase("1k", 1000)]
        [TestCase("1.5k", 1500)]
        [TestCase("-1.5k", -1500)]
        [TestCase("2.147483647g", int.MaxValue)]
        [TestCase("-2.147483648g", int.MinValue)]
        [TestCase("0m", 0)]
        [TestCase("12", 12)]
        [TestCase("2147483647", int.MaxValue)]
        public void ToIntAbbreviated(string input, int expected)
        {
            Assert.That(input.ToInt(), Is.EqualTo(expected));
            Assert.That(input.ToIntOrDefault(42), Is.EqualTo(expected));
        }

        [TestCase("", typeof(FormatException))]
        [TestCase("m", typeof(FormatException))]
        [TestCase("x", typeof(FormatException))]
        [TestCase("1.5", typeof(FormatException))]
        [TestCase("1.0005k", typeof(FormatException))]
        [TestCase("2.147483648g", typeof(OverflowException))]
        [TestCase("1t", typeof(OverflowException))]
        [TestCase("2147483648", typeof(OverflowException))]
        public void ToIntInvalid(string input, Type exception)
        {
            Assert.That(() => input.ToInt(), Throws.TypeOf(exception));
            Assert.That(input.ToIntOrDefault(42), Is.EqualTo(42));
        }

        [Test]
        public void ToFloatTest()
        {
            Assert.That("1".ToFloat(), Is.EqualTo(1f));
            Assert.That("11234".ToFloat(), Is.EqualTo(11234f));
            Assert.That("4321341".ToFloat(), Is.EqualTo(4321341f));
            Assert.That("1431234121".ToFloat(), Is.EqualTo(1431234121f));

            float? fl = "143123412asdf1".ToFloat();
            Assert.That(fl, Is.EqualTo((float?)null));

            Assert.That("1431234121sdf".ToFloatOrNeg1(), Is.EqualTo(-1));
            Assert.That("1431234121sdf".ToFloatOrDefault(), Is.EqualTo(-1));

            Assert.That("1431".ToFloatOrNeg1(), Is.EqualTo(1431));
            Assert.That("1431".ToFloatOrDefault(), Is.EqualTo(1431));
        }

        [Test]
        public void ToIntTest()
        {
            Assert.That("1".ToInt(), Is.EqualTo(1));
            Assert.That("11234".ToInt(), Is.EqualTo(11234));
            Assert.That("1k".ToInt(), Is.EqualTo(1000));
            Assert.That("4321341".ToInt(), Is.EqualTo(4321341));
            Assert.That(int.MaxValue.ToString(CultureInfo.InvariantCulture).ToInt(), Is.EqualTo(int.MaxValue));
            Assert.That("1431234121".ToInt(), Is.EqualTo(1431234121));

            Assert.That("1".ToIntFast(), Is.EqualTo(1));
            Assert.That("11234".ToIntFast(), Is.EqualTo(11234));
            Assert.That("4321341".ToIntFast(), Is.EqualTo(4321341));
            Assert.That(int.MaxValue.ToString(CultureInfo.InvariantCulture).ToIntFast(), Is.EqualTo(int.MaxValue));
            Assert.That("1431234121".ToIntFast(), Is.EqualTo(1431234121));

            Assert.That("1".ToIntFast(), Is.EqualTo(1));
            Assert.That("-11234".ToIntFast(), Is.EqualTo(-11234));
            Assert.That(" 4321341 ".ToIntFast(), Is.EqualTo(4321341));
            Assert.That(" -4321341 ".ToIntFast(), Is.EqualTo(-4321341));
            Assert.That("1431234121".ToIntFast(), Is.EqualTo(1431234121));

            Assert.That("1".ToIntOrDefaultFast(), Is.EqualTo(1));
            Assert.That("11234".ToIntOrDefaultFast(), Is.EqualTo(11234));
            Assert.That("4321341".ToIntOrDefaultFast(), Is.EqualTo(4321341));
            Assert.That(int.MaxValue.ToString(CultureInfo.InvariantCulture).ToIntOrDefaultFast(), Is.EqualTo(int.MaxValue));
            Assert.That("1431234121".ToIntOrDefaultFast(), Is.EqualTo(1431234121));

            Assert.That("1".ToIntOrDefaultFast(), Is.EqualTo(1));
            Assert.That("-11234".ToIntOrDefaultFast(), Is.EqualTo(-11234));
            Assert.That(" 4321341 ".ToIntOrDefaultFast(), Is.EqualTo(4321341));
            Assert.That(" -4321341 ".ToIntOrDefaultFast(), Is.EqualTo(-4321341));
            Assert.That("1431234121".ToIntOrDefaultFast(), Is.EqualTo(1431234121));

            Assert.That(1.0.ToInt(), Is.EqualTo(1));
            Assert.That(11234.0.ToInt(), Is.EqualTo(11234));
            Assert.That(4321341.0.ToInt(), Is.EqualTo(4321341));
            Assert.That(1431234121.0.ToInt(), Is.EqualTo(1431234121));

            Assert.That(((decimal)1).ToInt(), Is.EqualTo(1));
            Assert.That(((decimal)11234).ToInt(), Is.EqualTo(11234));
            Assert.That(((decimal)4321341).ToInt(), Is.EqualTo(4321341));
            Assert.That(((decimal)1431234121).ToInt(), Is.EqualTo(1431234121));

            Assert.That(((double)1).ToInt(), Is.EqualTo(1));
            Assert.That(((double)11234).ToInt(), Is.EqualTo(11234));
            Assert.That(((double)4321341).ToInt(), Is.EqualTo(4321341));
            Assert.That(((double)1431234121).ToInt(), Is.EqualTo(1431234121));
        }

        [Test]
        public void ToIntOrNeg1Test()
        {
            Assert.That("basdf".ToIntOrDefaultFast(-1), Is.EqualTo(-1));
            Assert.That(int.MaxValue.ToString(CultureInfo.InvariantCulture).ToIntOrDefaultFast(-1), Is.EqualTo(int.MaxValue));

            Assert.That("basdf".ToIntOrNeg1(), Is.EqualTo(-1));
            Assert.That(int.MaxValue.ToString(CultureInfo.InvariantCulture).ToIntOrNeg1(), Is.EqualTo(int.MaxValue));
            Assert.That("1k".ToIntOrNeg1(), Is.EqualTo(1000));
            Assert.That("143123412000001b".ToIntOrNeg1(), Is.EqualTo(-1));
        }

        [Test]
        public void IsLongTest()
        {
            Assert.That("1".IsLong(), Is.True);
            Assert.That("11234".IsLong(), Is.True);
            Assert.That("4321341".IsLong(), Is.True);
            Assert.That("1123412341234".IsLong(), Is.True);
            Assert.That("1431234121".IsLong(), Is.True);
            Assert.That("14a".IsLong(), Is.False);
            Assert.That(long.MaxValue.ToString(CultureInfo.InvariantCulture).IsLong(), Is.True);
            Assert.That(long.MinValue.ToString(CultureInfo.InvariantCulture).IsLong(), Is.True);
        }

        [TestCase(new int[0], "")]
        [TestCase(new[] { 5 }, "5")]
        [TestCase(new[] { 5, 6 }, "5-6")]
        [TestCase(new[] { 5, 7 }, "5, 7")]
        [TestCase(new[] { 1, 2, 4 }, "1-2, 4")]
        [TestCase(new[] { 1, 3, 4 }, "1, 3-4")]
        [TestCase(new[] { 4, 1, 2, 2, 4, 3 }, "1-4")]
        [TestCase(new[] { 1, 3, 5, 7 }, "1, 3, 5, 7")]
        [TestCase(new[] { 3, 1, 1, 3, 3 }, "1, 3")]
        [TestCase(new[] { 7, 7 }, "7")]
        [TestCase(new[] { -3, -2, -1, 1 }, "-3--1, 1")]
        [TestCase(new[] { -1, 0, 1 }, "-1-1")]
        [TestCase(new[] { -1 }, "-1")]
        [TestCase(new[] { int.MaxValue - 1, int.MaxValue }, "2147483646-2147483647")]
        [TestCase(new[] { int.MinValue, int.MinValue + 1, 0 }, "-2147483648--2147483647, 0")]
        public void CollapseToRangesFormats(int[] values, string expected)
        {
            Assert.That(values.CollapseIntsToRanges(), Is.EqualTo(expected));
            Assert.That(values.Select(static v => (long)v).CollapseLongsToRanges(), Is.EqualTo(expected));
        }

        [Test]
        public void CollapseIntsToRangesTest()
        {
            Assert.That(new List<string> { "1", "4", "5", "6", "7", "8", "11" }.CollapseIntsToRanges(), Is.EqualTo("1, 4-8, 11"));
            Assert.That(new List<string> { "1" }.CollapseIntsToRanges(), Is.EqualTo("1"));
        }

        [Test]
        public void CollapseLongtsToRangesTest()
        {
            Assert.That(new List<string> { "1", "4", "5", "6", "7", "8", "11" }.CollapseLongsToRanges(), Is.EqualTo("1, 4-8, 11"));
            Assert.That(new List<string> { "1" }.CollapseLongsToRanges(), Is.EqualTo("1"));
        }

        [Test]
        public void CollapseIntegerRangesToRangesTest()
        {
            var expected = new List<IntegerRange>
            {
                new(1,1),
                new(4,8),
                new(11,11)
            };

            Assert.That(expected.Count, Is.EqualTo(new List<int> { 1, 4, 5, 6, 7, 8, 11 }.CollapseIntsToIntegerRanges().ToList().Count));
            Assert.That(expected[1].Start, Is.EqualTo(new List<int> { 1, 4, 5, 6, 7, 8, 11 }.CollapseIntsToIntegerRanges().ToList()[1].Start));
            Assert.That(expected[1].Stop, Is.EqualTo(new List<int> { 1, 4, 5, 6, 7, 8, 11 }.CollapseIntsToIntegerRanges().ToList()[1].Stop));
        }

        [Test]
        public void IsNearlyEqualTest()
        {
            Assert.That(((double)1.2).NearlyEqual(1.2, 0.0000001), Is.True);
            Assert.That(((double)1.20001).NearlyEqual(1.20002, 0.0001), Is.True);
            Assert.That(((double)1.20001).NearlyEqual(1.20002, 0.000001), Is.False);
        }
    }
}