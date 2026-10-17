using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JMW.Extensions.Tests
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void ReverseOrderTest()
        {
            Assert.That(new List<int> { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, Is.EqualTo(new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.ReverseOrder().ToList()));
        }

        [Test]
        public void LastTest1()
        {
            var lst = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            Assert.That(new List<int> { 6, 7, 8, 9, 10 }, Is.EqualTo(lst.Last(5)));
            Assert.That(new List<int> { 8, 9, 10 }, Is.EqualTo(lst.Last(3)));
            Assert.That(0, Is.EqualTo(lst.Last(-3).Count()));
            Assert.That(0, Is.EqualTo(lst.Last(0).Count()));
            Assert.That(lst, Is.EqualTo(lst.Last(30)));
        }

        [Test]
        public void ToHashSetTest1()
        {
            var lst = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var hsh = lst.ToHashSet();

            Assert.That(10, Is.EqualTo(hsh.Count));
            Assert.That(typeof(HashSet<int>), Is.EqualTo(hsh.GetType()));
        }

        [Test]
        public void MergeTest1()
        {
            var lst1 = new HashSet<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var lst2 = new HashSet<int> { 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

            var lst = lst1.Merge(lst2);

            Assert.That(15, Is.EqualTo(lst.Count));
            lst.Merge(11);
            Assert.That(15, Is.EqualTo(lst.Count));
            lst.Merge(16);
            Assert.That(16, Is.EqualTo(lst.Count));

            lst.Merge(lst2.ToList());
            Assert.That(16, Is.EqualTo(lst.Count));
            lst.Merge(new List<int> { 3, 5, 18, 19 });
            Assert.That(18, Is.EqualTo(lst.Count));
        }

        [Test]
        public void CreateHistogramTest()
        {
            var lst1 = new List<int> { 1, 1, 1, 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6, 7, 8, 9, 9, 10 };

            var lst = lst1.CreateHistogram(i => i.ToString(CultureInfo.InvariantCulture));

            Assert.That(10, Is.EqualTo(lst.Count));
            Assert.That(3, Is.EqualTo(lst["1"].Count));
            Assert.That(1, Is.EqualTo(lst["2"].Count));
            Assert.That(2, Is.EqualTo(lst["3"].Count));
            Assert.That(4, Is.EqualTo(lst["4"].Count));
            Assert.That(3, Is.EqualTo(lst["5"].Count));
            Assert.That(2, Is.EqualTo(lst["6"].Count));
            Assert.That(1, Is.EqualTo(lst["7"].Count));
            Assert.That(1, Is.EqualTo(lst["8"].Count));
            Assert.That(2, Is.EqualTo(lst["9"].Count));
            Assert.That(1, Is.EqualTo(lst["10"].Count));

            var lst2 = lst1.CreateHistogram(i => i);

            Assert.That(10, Is.EqualTo(lst2.Count));
            Assert.That(3, Is.EqualTo(lst2[1].Count));
            Assert.That(1, Is.EqualTo(lst2[2].Count));
            Assert.That(2, Is.EqualTo(lst2[3].Count));
            Assert.That(4, Is.EqualTo(lst2[4].Count));
            Assert.That(3, Is.EqualTo(lst2[5].Count));
            Assert.That(2, Is.EqualTo(lst2[6].Count));
            Assert.That(1, Is.EqualTo(lst2[7].Count));
            Assert.That(1, Is.EqualTo(lst2[8].Count));
            Assert.That(2, Is.EqualTo(lst2[9].Count));
            Assert.That(1, Is.EqualTo(lst2[10].Count));
        }

        [Test]
        public void ToDelimitedStringTest()
        {
            var lst = new HashSet<int> { 1, 2, 3, 4 };
            Assert.That("1,2,3,4", Is.EqualTo(lst.ToDelimitedString(',')));
            Assert.That("1,2,3,4,", Is.EqualTo(lst.ToDelimitedString(',', false)));

            Assert.That("1,,2,,3,,4", Is.EqualTo(lst.ToDelimitedString(",,")));
            Assert.That("1,,2,,3,,4,,", Is.EqualTo(lst.ToDelimitedString(",,", false)));
        }

        [TestCase(new[] { "a," }, ",", "a,", "a,,")]
        [TestCase(new[] { "a", "b," }, ",", "a,b,", "a,b,,")]
        [TestCase(new[] { "a", "" }, ",", "a,", "a,,")]
        [TestCase(new[] { "", "" }, ",", ",", ",,")]
        [TestCase(new[] { "" }, ",", "", ",")]
        [TestCase(new[] { "a" }, ",", "a", "a,")]
        [TestCase(new string[0], ",", "", "")]
        [TestCase(new[] { "ab", "cba" }, "ab", "ababcba", "ababcbaab")]
        [TestCase(new[] { "x;", "y" }, ";", "x;;y", "x;;y;")]
        [TestCase(new[] { "a ", "b " }, "", "a b ", "a b ")]
        [TestCase(new[] { "a", "b", "c" }, ", ", "a, b, c", "a, b, c, ")]
        public void ToDelimitedStringKeepsTrailingData(string[] items, string delimiter, string trimmed, string untrimmed)
        {
            Assert.That(items.ToDelimitedString(delimiter), Is.EqualTo(trimmed));
            Assert.That(items.ToDelimitedString(delimiter), Is.EqualTo(string.Join(delimiter, items)));
            Assert.That(items.ToDelimitedString(delimiter, false), Is.EqualTo(untrimmed));
            if (delimiter.Length == 1)
            {
                Assert.That(items.ToDelimitedString(delimiter[0]), Is.EqualTo(trimmed));
                Assert.That(items.ToDelimitedString(delimiter[0], false), Is.EqualTo(untrimmed));
            }
        }

        private static readonly int[] sourceArray = [1, 2, 3, 4];

        [Test]
        public void IslastTest()
        {
            Assert.That(true, Is.EqualTo(sourceArray.IsLast(3)));
            Assert.That(true, Is.EqualTo(sourceArray.IsLast(3)));
            Assert.That(true, Is.EqualTo(sourceArray.Select(i => i).IsLast(3)));

            Assert.That(false, Is.EqualTo(sourceArray.Select(i => i).IsLast(2)));
            Assert.That(false, Is.EqualTo(sourceArray.IsLast(2)));
            Assert.That(false, Is.EqualTo(sourceArray.IsLast(2)));
        }

        [Test]
        public void ToListOfItemTest()
        {
            var lst = 1.ToListOfItem();
            Assert.That(1, Is.EqualTo(lst.Count));
        }

        [Test]
        public void YieldTest()
        {
            var lst = 1.Yield();
            Assert.That(1, Is.EqualTo(lst.Count()));
        }

        [Test]
        public void ShimTest()
        {
            var lst = new List<object> { 1 }.Shim<int>();
            Assert.That(1, Is.EqualTo(lst.Count()));
        }

        [Test]
        public void GetRangeSafeTest()
        {
            var lst = new List<int> { 1, 2, 3, 4 };
            Assert.That(new List<int> { 2, 3 }, Is.EquivalentTo(lst.GetRangeSafe(1, 2)));
            Assert.That(new List<int> { 2, 3, 4 }, Is.EquivalentTo(lst.GetRangeSafe(1, 3)));
            Assert.That(new List<int> { 2, 3, 4 }, Is.EquivalentTo(lst.GetRangeSafe(1, 5)));
            Assert.That(new List<int>(), Is.EquivalentTo(lst.GetRangeSafe(5, 2)));
        }

        [Test]
        public void EachTest()
        {
            var lst = new List<Foo> { new() { Value = 1 }, new() { Value = 2 }, new() { Value = 3 }, new() { Value = 4 } };
            lst.Each(i => i.Value *= 2);

            Assert.That(new List<int> { 2, 4, 6, 8 }, Is.EquivalentTo(lst.Select(i => i.Value)));
        }

        [Test]
        public void GroupIntoSetsTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0 },
                new() { Value = 1 },
                new() { Value = 2 },
                new() { Value = 3 },
                new() { Value = 4 },
                new() { Value = 5 },
                new() { Value = 6 },
                new() { Value = 7 },
                new() { Value = 8 },
                new() { Value = 9 }
            };

            var sets = lst.GroupIntoSets(o => o.Value % 3 == 0).ToList();

            Assert.That(sets.Count, Is.EqualTo(4));
            Assert.That(sets[0].Count, Is.EqualTo(3));
            Assert.That(sets[3].Count, Is.EqualTo(1));
        }

        [Test]
        public void ToDictionaryOfManyTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0, Bar=10 },
                new() { Value = 1, Bar=20 },
                new() { Value = 2, Bar=30 },
                new() { Value = 3, Bar=40 },
                new() { Value = 4, Bar=50 },
                new() { Value = 5, Bar=60 },
                new() { Value = 6, Bar=70 },
                new() { Value = 7, Bar=80 },
                new() { Value = 8, Bar=90 },
                new() { Value = 9, Bar=100 }
            };
            var dict = lst.ToDictionaryOfMany(o => new List<int> { o.Value, o.Bar });

            Assert.That(dict.Count, Is.EqualTo(20));
            Assert.That(dict[0], Is.EqualTo(dict[10]));
        }

        [Test]
        public void ToDictionaryOfHashSetsTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0, Bar=10 },
                new() { Value = 1, Bar=20 },
                new() { Value = 2, Bar=10 },
                new() { Value = 3, Bar=20 },
                new() { Value = 4, Bar=10 },
                new() { Value = 5, Bar=20 },
                new() { Value = 6, Bar=10 },
                new() { Value = 7, Bar=20 },
                new() { Value = 8, Bar=10 },
                new() { Value = 9, Bar=20 }
            };
            var dict = lst.ToDictionaryOfHashSets(o => o.Bar);

            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict[10].Count, Is.EqualTo(5));
        }

        [Test]
        public void ToDictionaryOfListsTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0, Bar=10 },
                new() { Value = 1, Bar=20 },
                new() { Value = 2, Bar=10 },
                new() { Value = 3, Bar=20 },
                new() { Value = 4, Bar=10 },
                new() { Value = 5, Bar=20 },
                new() { Value = 6, Bar=10 },
                new() { Value = 7, Bar=20 },
                new() { Value = 8, Bar=10 },
                new() { Value = 9, Bar=20 }
            };
            var dict = lst.ToDictionaryOfLists(o => o.Bar);

            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict[10].Count, Is.EqualTo(5));
        }

        [Test]
        public void ToDictionaryOfListsOfManyTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0, Bar=10 },
                new() { Value = 1, Bar=20 },
                new() { Value = 2, Bar=10 },
                new() { Value = 3, Bar=20 },
                new() { Value = 4, Bar=10 },
                new() { Value = 5, Bar=20 },
                new() { Value = 6, Bar=10 },
                new() { Value = 7, Bar=20 },
                new() { Value = 8, Bar=10 },
                new() { Value = 9, Bar=20 }
            };
            var dict = lst.ToDictionaryOfListsOfMany(o => new List<int> { o.Bar });

            Assert.That(dict.Count, Is.EqualTo(2));
            Assert.That(dict[10].Count, Is.EqualTo(5));
        }

        [Test]
        public void ToDictionaryOfHashSetsOfManyTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0, Bar=10 },
                new() { Value = 1, Bar=20 },
                new() { Value = 2, Bar=10 },
                new() { Value = 3, Bar=20 },
                new() { Value = 4, Bar=10 },
                new() { Value = 5, Bar=20 },
                new() { Value = 6, Bar=10 },
                new() { Value = 7, Bar=20 },
                new() { Value = 8, Bar=10 },
                new() { Value = 9, Bar=20 }
            };
            var dict = lst.ToDictionaryOfHashSetsOfMany(o => new List<int> { o.Bar, o.Value });

            Assert.That(dict.Count, Is.EqualTo(12));
            Assert.That(dict[10].Count, Is.EqualTo(5));
        }

        [Test]
        public void GetDuplicatesTest()
        {
            var lst = new List<int>
            {
                1,1,2,2,3,4,5,6,7,8,9,0,0,2,4,3,2,6
            };
            var dict = lst.GetDuplicates().ToList();

            Assert.That(dict.Count, Is.EqualTo(8));
        }

        [Test]
        public void DisctinctTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0, Bar=10 },
                new() { Value = 1, Bar=20 },
                new() { Value = 2, Bar=10 },
                new() { Value = 3, Bar=20 },
                new() { Value = 4, Bar=10 },
                new() { Value = 5, Bar=20 },
                new() { Value = 6, Bar=10 },
                new() { Value = 7, Bar=20 },
                new() { Value = 8, Bar=10 },
                new() { Value = 9, Bar=20 }
            };
            var dict = lst.Distinct(k => k.Bar).ToList();

            Assert.That(dict.Count, Is.EqualTo(2));
        }

        //[Test]
        //public void ShuffleTest()
        //{
        //    var lst = new List<int>
        //    {
        //        1,2,3,4,5,6,7,8,9,0
        //    };
        //    lst.Shuffle();

        //    Assert.That(lst[0] != 1);
        //}

        [Test]
        public void IfHasKeyTest()
        {
            var lst = new List<Foo>
            {
                new() { Value = 0, Bar=10 },
                new() { Value = 1, Bar=20 },
                new() { Value = 2, Bar=30 },
                new() { Value = 3, Bar=40 },
                new() { Value = 4, Bar=50 },
                new() { Value = 5, Bar=60 },
                new() { Value = 6, Bar=70 },
                new() { Value = 7, Bar=80 },
                new() { Value = 8, Bar=90 },
                new() { Value = 9, Bar=100 }
            };
            var dict = lst.ToDictionaryOfMany(o => new List<int> { o.Value, o.Bar });

            dict.IfHasKey(0, o =>
            {
                Assert.That(0, Is.EqualTo(o.Value));
                Assert.That(10, Is.EqualTo(o.Bar));
            });
            dict.IfHasKey(99, o => { Assert.Fail(); });

            var did = dict.TryAdd(0, new Foo());
            Assert.That(did, Is.False);
        }

        private class Foo
        {
            public int Value { get; set; } = 0;
            public int Bar { get; set; } = 0;
        }
    }
}