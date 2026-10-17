using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JMW.Collections.Tests
{
    [TestFixture]
    public class RingBufferTests
    {
        [Test]
        public void RingBufferTest()
        {
            var buff = new RingBuffer<int>(10);
            for (var i = 0; i < 15; i++)
                buff.Add(i);

            Assert.That(buff.Count, Is.EqualTo(10));
            Assert.That(buff.Last(), Is.EqualTo(14));

            var c = 5;
            foreach (var i in buff)
                Assert.That(i, Is.EqualTo(c++));

            Assert.That(c, Is.EqualTo(15));
            Assert.That(buff.Contains(11), Is.True);
            buff.Clear();
            Assert.That(buff.Count, Is.EqualTo(0));
            Assert.That(buff.IsReadOnly, Is.False);
        }

        [Test]
        public void RingBufferTest1()
        {
            var buff = new RingBuffer<int>(10, true);
            for (var i = 0; i < 15; i++)
                buff.Add(i);

            var c = 14;
            foreach (var i in buff)
                Assert.That(i, Is.EqualTo(c--));

            Assert.That(c, Is.EqualTo(4));
            Assert.That(buff.Count, Is.EqualTo(10));
        }

        [Test]
        public void RingBufferTest2()
        {
            var buff = new RingBuffer<int>([3, 4, 5, 6, 7, 8, 9, 10, 11, 12]);
            Assert.That(buff.Count, Is.EqualTo(10));
            Assert.That(buff.Contains(11), Is.True);
            buff.Clear();
            Assert.That(buff.Count, Is.EqualTo(0));
        }

        [Test]
        public void RingBufferTest3()
        {
            var buff = new RingBuffer<int>([3, 4, 5, 6, 7, 8, 9, 10, 11, 12], true);

            var c = 12;
            foreach (var i in buff)
                Assert.That(i, Is.EqualTo(c--));
            Assert.That(c, Is.EqualTo(2));
            Assert.That(buff.Count, Is.EqualTo(10));
        }

        [Test]
        public void RingBufferTest4()
        {
            var buff = new RingBuffer<int>([3, 4, 5]);
            Assert.That(buff.Last(), Is.EqualTo(5));
            buff = new RingBuffer<int>([3, 4, 5], true);
            Assert.That(buff.Last(), Is.EqualTo(3));

            buff = new RingBuffer<int>(10, [3, 4, 5]);
            Assert.That(buff.Last(), Is.EqualTo(5));
            Assert.That(buff.Count, Is.EqualTo(3));

            buff = new RingBuffer<int>(10, [3, 4, 5], true);
            Assert.That(buff.Last(), Is.EqualTo(3));
        }

        [Test]
        public void RingBufferTest5()
        {
            var buff = new RingBuffer<int>([3, 4, 5]);
            Assert.That(buff[2], Is.EqualTo(5));
            Assert.That(buff[1], Is.EqualTo(4));
            Assert.That(buff[0], Is.EqualTo(3));
            Assert.That(buff.First(), Is.EqualTo(3));
            Assert.That(buff.Last(), Is.EqualTo(5));

            buff.Add(6);
            Assert.That(buff[2], Is.EqualTo(6));
            Assert.That(buff[1], Is.EqualTo(5));
            Assert.That(buff[0], Is.EqualTo(4));
            Assert.That(buff.First(), Is.EqualTo(4));
            Assert.That(buff.Last(), Is.EqualTo(6));

            buff.Add(7);
            Assert.That(buff[2], Is.EqualTo(7));
            Assert.That(buff[1], Is.EqualTo(6));
            Assert.That(buff[0], Is.EqualTo(5));
            Assert.That(buff.First(), Is.EqualTo(5));
            Assert.That(buff.Last(), Is.EqualTo(7));
        }

        [TestCase(1, 1)]
        [TestCase(3, 1)]
        [TestCase(3, 2)]
        [TestCase(3, 3)]
        [TestCase(3, 4)]
        [TestCase(3, 7)]
        [TestCase(10, 5)]
        [TestCase(10, 10)]
        [TestCase(10, 25)]
        public void IndexFirstLastAfterAdds(int capacity, int adds)
        {
            var buff = new RingBuffer<string>(capacity);
            for (var i = 0; i < adds; i++)
                buff.Add(i.ToString(CultureInfo.InvariantCulture));

            var expected = Enumerable.Range(0, adds).Skip(Math.Max(0, adds - capacity))
                .Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray();

            Assert.That(buff.Count, Is.EqualTo(expected.Length));
            for (var i = 0; i < expected.Length; i++)
                Assert.That(buff[i], Is.EqualTo(expected[i]));

            Assert.That(buff.First(), Is.EqualTo(expected[0]));
            Assert.That(buff.Last(), Is.EqualTo(expected[^1]));
            Assert.That(buff.ToArray(), Is.EqualTo(expected));
            Assert.That(buff.Contains(expected[0]), Is.True);
            Assert.That(buff.Contains("missing"), Is.False);

            buff.ReverseIteration = true;
            Assert.That(buff.ToArray(), Is.EqualTo(expected.Reverse().ToArray()));
            Assert.That(buff.Last(), Is.EqualTo(expected[0]));
        }

        [TestCase(5, new[] { 1, 2, 3 }, 4, new[] { 1, 2, 3, 4 })]
        [TestCase(5, new[] { 1, 2, 3 }, 6, new[] { 2, 3, 4, 5, 6 })]
        [TestCase(3, new[] { 1, 2, 3 }, 4, new[] { 2, 3, 4 })]
        [TestCase(2, new[] { 1, 2, 3 }, 4, new[] { 2, 3, 4 })]
        [TestCase(4, new int[0], 2, new[] { 1, 2 })]
        public void SizedConstructorThenAdd(int size, int[] initial, int addUpTo, int[] expected)
        {
            var buff = new RingBuffer<int>(size, initial);
            for (var i = initial.Length + 1; i <= addUpTo; i++)
                buff.Add(i);

            Assert.That(buff.ToArray(), Is.EqualTo(expected));
            Assert.That(buff[0], Is.EqualTo(expected[0]));
            Assert.That(buff.Last(), Is.EqualTo(expected[^1]));
        }

        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void IndexOutOfRange(int idx)
        {
            var buff = new RingBuffer<int>(5) { 1, 2, 3 };
            Assert.That(() => buff[idx], Throws.TypeOf<IndexOutOfRangeException>());
            Assert.That(() => buff[idx] = 9, Throws.TypeOf<IndexOutOfRangeException>());
        }

        [Test]
        public void EmptyBuffer()
        {
            var buff = new RingBuffer<int>(3);
            Assert.That(() => buff.First(), Throws.InvalidOperationException);
            Assert.That(() => buff.Last(), Throws.InvalidOperationException);
            Assert.That(buff.ToArray(), Is.Empty);
            Assert.That(buff.Contains(0), Is.False);

            buff.Add(0);
            buff.Clear();
            Assert.That(buff.Contains(0), Is.False);

            var zero = new RingBuffer<int>(0);
            zero.Add(1);
            Assert.That(zero.Count, Is.EqualTo(0));
            Assert.That(zero.ToArray(), Is.Empty);
        }

        [Test]
        public void ContainsIgnoresUnusedSlots()
        {
            var buff = new RingBuffer<int>(5) { 1 };
            Assert.That(buff.Contains(0), Is.False);
            Assert.That(buff.Contains(1), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnumeratorReset(bool reverse)
        {
            var buff = new RingBuffer<int>(4, reverse);
            for (var i = 0; i < 6; i++)
                buff.Add(i);

            using var e = buff.GetEnumerator();
            var first = new List<int>();
            while (e.MoveNext())
                first.Add(e.Current);

            e.Reset();
            var second = new List<int>();
            while (e.MoveNext())
                second.Add(e.Current);

            Assert.That(first, Has.Count.EqualTo(4));
            Assert.That(second, Is.EqualTo(first));
        }

        [TestCase(3, 1, false, 0, 0)]
        [TestCase(3, 1, true, 0, 0)]
        [TestCase(3, 2, false, 0, 1)]
        [TestCase(3, 2, true, 1, 0)]
        [TestCase(3, 3, false, 0, 2)]
        [TestCase(3, 3, true, 2, 0)]
        [TestCase(3, 4, false, 1, 3)]
        [TestCase(3, 4, true, 3, 1)]
        [TestCase(3, 7, false, 4, 6)]
        [TestCase(3, 7, true, 6, 4)]
        [TestCase(1, 5, false, 4, 4)]
        [TestCase(1, 5, true, 4, 4)]
        public void FirstAndLastFollowEnumerationOrder(int capacity, int adds, bool reverse, int first, int last)
        {
            var buff = new RingBuffer<int>(capacity, reverse);
            for (var i = 0; i < adds; i++)
                buff.Add(i);

            var enumerated = buff.ToArray();
            Assert.That(buff.First(), Is.EqualTo(first));
            Assert.That(buff.Last(), Is.EqualTo(last));
            Assert.That(buff.First(), Is.EqualTo(enumerated[0]));
            Assert.That(buff.Last(), Is.EqualTo(enumerated[^1]));
        }

        [TestCase(false, 3, 5)]
        [TestCase(true, 5, 3)]
        public void FirstAndLastFromArrayConstructor(bool reverse, int first, int last)
        {
            var buff = new RingBuffer<int>([3, 4, 5], reverse);
            Assert.That(buff.First(), Is.EqualTo(first));
            Assert.That(buff.Last(), Is.EqualTo(last));

            var sized = new RingBuffer<int>(10, [3, 4, 5], reverse);
            Assert.That(sized.First(), Is.EqualTo(first));
            Assert.That(sized.Last(), Is.EqualTo(last));
        }

        [Test]
        public void FirstAndLastTrackReverseToggle()
        {
            var buff = new RingBuffer<int>(4) { 1, 2, 3, 4, 5 };
            Assert.That((buff.First(), buff.Last()), Is.EqualTo((2, 5)));

            buff.ReverseIteration = true;
            Assert.That((buff.First(), buff.Last()), Is.EqualTo((5, 2)));

            buff.ReverseIteration = false;
            Assert.That((buff.First(), buff.Last()), Is.EqualTo((2, 5)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FirstAndLastThrowWhenEmpty(bool reverse)
        {
            var buff = new RingBuffer<int>(3, reverse);
            Assert.That(() => buff.First(), Throws.InvalidOperationException);
            Assert.That(() => buff.Last(), Throws.InvalidOperationException);

            var zero = new RingBuffer<int>(0, reverse);
            zero.Add(1);
            Assert.That(() => zero.First(), Throws.InvalidOperationException);
        }

        [Test]
        public void RingBufferTest6()
        {
            var buff = new RingBuffer<int>(100)
            {
                3,
                4,
                5
            };
            var arr2 = buff.ToArray();
            Assert.That(arr2.Length, Is.EqualTo(buff.Count));

            buff.ReverseIteration = true;
            arr2 = [.. buff];
            Assert.That(arr2.Length, Is.EqualTo(buff.Count));
        }
    }
}