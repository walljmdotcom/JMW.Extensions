using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lambda.Generic.Arithmetic.Tests
{
    [TestFixture]
    public class SignedTests
    {
        [TestCase(1L, 2L, -1)]
        [TestCase(2L, 1L, 1)]
        [TestCase(5L, 5L, 0)]
        [TestCase(-3L, 4L, -1)]
        [TestCase(long.MinValue, long.MaxValue, -1)]
        public void DefaultComparersMatchTheValue(long a, long b, int expected)
        {
            Signed<long, LongMath> x = a;
            Signed<long, LongMath> y = b;

            Assert.That(Math.Sign(Comparer<Signed<long, LongMath>>.Default.Compare(x, y)), Is.EqualTo(expected));
            Assert.That(EqualityComparer<Signed<long, LongMath>>.Default.Equals(x, y), Is.EqualTo(expected == 0));
            Assert.That(x.Equals((object)y), Is.EqualTo(expected == 0));
        }

        // SortedList and Dictionary compare keys through the default comparers, which box a struct without these.
        [Test]
        public void DefaultComparersDoNotAllocate()
        {
            Signed<long, LongMath> x = 1;
            Signed<long, LongMath> y = 2;
            var comparer = Comparer<Signed<long, LongMath>>.Default;
            var equality = EqualityComparer<Signed<long, LongMath>>.Default;
            _ = comparer.Compare(x, y) + equality.GetHashCode(x);
            _ = equality.Equals(x, y);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var total = 0;
            for (var i = 0; i < 1000; i++)
            {
                total += comparer.Compare(x, y);
                if (equality.Equals(x, y))
                    total++;
            }

            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(total, Is.EqualTo(-1000));
        }
    }
}
