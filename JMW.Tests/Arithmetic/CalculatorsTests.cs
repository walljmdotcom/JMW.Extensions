using NUnit.Framework;

namespace Lambda.Generic.Arithmetic.Tests
{
    [TestFixture]
    public class CalculatorsTests
    {
        [TestCase(1, 2, 1, 2)]
        [TestCase(2, 1, 1, 2)]
        [TestCase(5, 5, 5, 5)]
        [TestCase(-3, 4, -3, 4)]
        [TestCase(-3, -4, -4, -3)]
        [TestCase(0, 0, 0, 0)]
        [TestCase(int.MinValue, int.MaxValue, int.MinValue, int.MaxValue)]
        [TestCase(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue)]
        public void IntMathMinMax(int a, int b, int expectedMin, int expectedMax)
        {
            var calc = new IntMath();

            Assert.That(calc.Min(a, b), Is.EqualTo(expectedMin));
            Assert.That(calc.Max(a, b), Is.EqualTo(expectedMax));
        }

        [TestCase(1L, 2L, 1L, 2L)]
        [TestCase(2L, 1L, 1L, 2L)]
        [TestCase(5L, 5L, 5L, 5L)]
        [TestCase(-3L, 4L, -3L, 4L)]
        [TestCase(-3L, -4L, -4L, -3L)]
        [TestCase(0L, 0L, 0L, 0L)]
        [TestCase(long.MinValue, long.MaxValue, long.MinValue, long.MaxValue)]
        [TestCase(long.MaxValue, long.MinValue, long.MinValue, long.MaxValue)]
        public void LongMathMinMax(long a, long b, long expectedMin, long expectedMax)
        {
            var calc = new LongMath();

            Assert.That(calc.Min(a, b), Is.EqualTo(expectedMin));
            Assert.That(calc.Max(a, b), Is.EqualTo(expectedMax));
        }
    }
}
