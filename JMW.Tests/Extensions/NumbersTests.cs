using JMW.Types;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Extensions.Numbers.Tests
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
        public void IsNotNullTest()
        {
            string? test = null;

            Assert.That("blah".IsNotNull(), Is.True);
            Assert.That(test.IsNotNull(), Is.False);
        }

        [Test]
        public void IsNullTest()
        {
            string? test = null;

            Assert.That("blah".IsNull(), Is.False);
            Assert.That(test.IsNull(), Is.True);
        }

        [Test]
        public void ToDoubleTest()
        {
            Assert.That((double)1, Is.EqualTo("1".ToDouble()));
            Assert.That((double)11234, Is.EqualTo("11234".ToDouble()));
            Assert.That((double)4321341, Is.EqualTo("4321341".ToDouble()));
            Assert.That((double)1431234121, Is.EqualTo("1431234121".ToDouble()));

            Assert.That(-1, Is.EqualTo("1431234121aasdf".ToDoubleOrNeg1()));
            Assert.That(-1, Is.EqualTo("1431234121aasdf".ToDoubleOrDefault()));

            Assert.That(1431, Is.EqualTo("1431".ToDoubleOrNeg1()));
            Assert.That(1431, Is.EqualTo("1431".ToDoubleOrDefault()));

            Assert.That((double)1, Is.EqualTo(1.ToDouble()));
            Assert.That((double)11234, Is.EqualTo(11234.ToDouble()));
            Assert.That((double)4321341, Is.EqualTo(4321341.ToDouble()));
            Assert.That((double)1431234121, Is.EqualTo(1431234121.ToDouble()));

            Assert.That((double)1, Is.EqualTo(((long)1).ToDouble()));
            Assert.That((double)11234, Is.EqualTo(((long)11234).ToDouble()));
            Assert.That((double)4321341, Is.EqualTo(((long)4321341).ToDouble()));
            Assert.That((double)1431234121, Is.EqualTo(((long)1431234121).ToDouble()));
        }

        [Test]
        public void ToLongTest()
        {
            Assert.That((long)1, Is.EqualTo("1".ToLong()));
            Assert.That((long)11234, Is.EqualTo("11234".ToLong()));
            Assert.That((long)4321341, Is.EqualTo("4321341".ToLong()));
            Assert.That((long)1431234121, Is.EqualTo("1431234121".ToLong()));

            Assert.That((long)1, Is.EqualTo(((decimal)1).ToLong()));
            Assert.That((long)11234, Is.EqualTo(((decimal)11234).ToLong()));
            Assert.That((long)4321341, Is.EqualTo(((decimal)4321341).ToLong()));
            Assert.That((long)1431234121, Is.EqualTo(((decimal)1431234121).ToLong()));

            Assert.That((long)1, Is.EqualTo(((double)1).ToLong()));
            Assert.That((long)11234, Is.EqualTo(((double)11234).ToLong()));
            Assert.That((long)4321341, Is.EqualTo(((double)4321341).ToLong()));
            Assert.That((long)1431234121, Is.EqualTo(((double)1431234121).ToLong()));

            Assert.That((long)1, Is.EqualTo("1".ToInt64()));
            Assert.That((long)11234, Is.EqualTo("11234".ToInt64()));
            Assert.That((long)4321341, Is.EqualTo("4321341".ToInt64()));
            Assert.That((long)1431234121, Is.EqualTo("1431234121".ToInt64()));

            Assert.That(-1, Is.EqualTo("1431234121asdf".ToInt64OrNeg1()));
            Assert.That(-1, Is.EqualTo("143123412000001b".ToInt64OrNeg1()));
            Assert.That(1431234121, Is.EqualTo("1431234121".ToInt64OrNeg1()));
            Assert.That(1000, Is.EqualTo("1k".ToInt64OrNeg1()));
            Assert.That(1000, Is.EqualTo("1k".ToInt64()));
        }

        [Test]
        public void ToFloatTest()
        {
            Assert.That(1f, Is.EqualTo("1".ToFloat()));
            Assert.That(11234f, Is.EqualTo("11234".ToFloat()));
            Assert.That(4321341f, Is.EqualTo("4321341".ToFloat()));
            Assert.That(1431234121f, Is.EqualTo("1431234121".ToFloat()));

            float? fl = "143123412asdf1".ToFloat();
            Assert.That((float?)null, Is.EqualTo(fl));

            Assert.That(-1, Is.EqualTo("1431234121sdf".ToFloatOrNeg1()));
            Assert.That(-1, Is.EqualTo("1431234121sdf".ToFloatOrDefault()));

            Assert.That(1431, Is.EqualTo("1431".ToFloatOrNeg1()));
            Assert.That(1431, Is.EqualTo("1431".ToFloatOrDefault()));
        }

        [Test]
        public void ToIntTest()
        {
            Assert.That(1, Is.EqualTo("1".ToInt()));
            Assert.That(11234, Is.EqualTo("11234".ToInt()));
            Assert.That(1000, Is.EqualTo("1k".ToInt()));
            Assert.That(4321341, Is.EqualTo("4321341".ToInt()));
            Assert.That(int.MaxValue, Is.EqualTo(int.MaxValue.ToString().ToInt()));
            Assert.That(1431234121, Is.EqualTo("1431234121".ToInt()));

            Assert.That(1, Is.EqualTo("1".ToIntFast()));
            Assert.That(11234, Is.EqualTo("11234".ToIntFast()));
            Assert.That(4321341, Is.EqualTo("4321341".ToIntFast()));
            Assert.That(int.MaxValue, Is.EqualTo(int.MaxValue.ToString().ToIntFast()));
            Assert.That(1431234121, Is.EqualTo("1431234121".ToIntFast()));

            Assert.That(1, Is.EqualTo("1".ToIntFast()));
            Assert.That(-11234, Is.EqualTo("-11234".ToIntFast()));
            Assert.That(4321341, Is.EqualTo(" 4321341 ".ToIntFast()));
            Assert.That(-4321341, Is.EqualTo(" -4321341 ".ToIntFast()));
            Assert.That(1431234121, Is.EqualTo("1431234121".ToIntFast()));

            Assert.That(1, Is.EqualTo("1".ToIntOrDefaultFast()));
            Assert.That(11234, Is.EqualTo("11234".ToIntOrDefaultFast()));
            Assert.That(4321341, Is.EqualTo("4321341".ToIntOrDefaultFast()));
            Assert.That(int.MaxValue, Is.EqualTo(int.MaxValue.ToString().ToIntOrDefaultFast()));
            Assert.That(1431234121, Is.EqualTo("1431234121".ToIntOrDefaultFast()));

            Assert.That(1, Is.EqualTo("1".ToIntOrDefaultFast()));
            Assert.That(-11234, Is.EqualTo("-11234".ToIntOrDefaultFast()));
            Assert.That(4321341, Is.EqualTo(" 4321341 ".ToIntOrDefaultFast()));
            Assert.That(-4321341, Is.EqualTo(" -4321341 ".ToIntOrDefaultFast()));
            Assert.That(1431234121, Is.EqualTo("1431234121".ToIntOrDefaultFast()));

            Assert.That(1, Is.EqualTo(1.0.ToInt()));
            Assert.That(11234, Is.EqualTo(11234.0.ToInt()));
            Assert.That(4321341, Is.EqualTo(4321341.0.ToInt()));
            Assert.That(1431234121, Is.EqualTo(1431234121.0.ToInt()));

            Assert.That(1, Is.EqualTo(((decimal)1).ToInt()));
            Assert.That(11234, Is.EqualTo(((decimal)11234).ToInt()));
            Assert.That(4321341, Is.EqualTo(((decimal)4321341).ToInt()));
            Assert.That(1431234121, Is.EqualTo(((decimal)1431234121).ToInt()));

            Assert.That(1, Is.EqualTo(((double)1).ToInt()));
            Assert.That(11234, Is.EqualTo(((double)11234).ToInt()));
            Assert.That(4321341, Is.EqualTo(((double)4321341).ToInt()));
            Assert.That(1431234121, Is.EqualTo(((double)1431234121).ToInt()));
        }

        [Test]
        public void ToIntOrNeg1Test()
        {
            Assert.That(-1, Is.EqualTo("basdf".ToIntOrDefaultFast(-1)));
            Assert.That(int.MaxValue, Is.EqualTo(int.MaxValue.ToString().ToIntOrDefaultFast(-1)));

            Assert.That(-1, Is.EqualTo("basdf".ToIntOrNeg1()));
            Assert.That(int.MaxValue, Is.EqualTo(int.MaxValue.ToString().ToIntOrNeg1()));
            Assert.That(1000, Is.EqualTo("1k".ToIntOrNeg1()));
            Assert.That(-1, Is.EqualTo("143123412000001b".ToIntOrNeg1()));
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
            Assert.That(long.MaxValue.ToString().IsLong(), Is.True);
            Assert.That(long.MinValue.ToString().IsLong(), Is.True);
        }

        [Test]
        public void CollapseIntsToRangesTest()
        {
            Assert.That("1, 4-8, 11", Is.EqualTo(new List<string> { "1", "4", "5", "6", "7", "8", "11" }.CollapseIntsToRanges()));
            Assert.That("1", Is.EqualTo(new List<string> { "1" }.CollapseIntsToRanges()));
        }

        [Test]
        public void CollapseLongtsToRangesTest()
        {
            Assert.That("1, 4-8, 11", Is.EqualTo(new List<string> { "1", "4", "5", "6", "7", "8", "11" }.CollapseLongsToRanges()));
            Assert.That("1", Is.EqualTo(new List<string> { "1" }.CollapseLongsToRanges()));
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