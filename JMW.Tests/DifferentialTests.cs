using JMW.Types;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace JMW.Differentials.Tests
{
    [TestFixture]
    public class DifferentialTests
    {
        [Test]
        public void CollectionDifferential1()
        {
            var lstA = new List<string> { "b", "c", "d" };
            var lstB = new List<string> { "A", "B", "c" };

            var diff = Differential.RunCollectionDifferential(
                lstA,
                lstB,
                (k, lst) =>
                {
                    return lst.FirstOrDefault(i => i.Equals(k, StringComparison.OrdinalIgnoreCase));
                },
                (a, b) =>
                {
                    return a != b ? new ObjectDifferential<string, MyDiff>(a, b, [new MyDiff { Difference = a + " " + b }]) : null;
                });

            Assert.That(diff.OnlyInListA.Count, Is.EqualTo(1));
            Assert.That(diff.OnlyInListB.Count, Is.EqualTo(1));
            Assert.That(diff.Modified.Count, Is.EqualTo(1));
            Assert.That(diff.Modified.First().Differences.First().Difference, Is.EqualTo("b B"));
            Assert.That(diff.Modified.First().ObjectA, Is.EqualTo("b"));
            Assert.That(diff.Modified.First().ObjectB, Is.EqualTo("B"));
        }

        [Test]
        public void PairTest1()
        {
            var o1 = "Jason";
            var o2 = "Wall";
            var o3 = "Jason";
            var o4 = "Wall";

            var p1 = new Pair<string>(o1, o2);
            var p2 = new Pair<string>(o3, o4);

            Assert.That(p1, Is.EqualTo(p2));
            Assert.That(p1.Equals(p2), Is.True);
            Assert.That(p1.Equals((object)p2), Is.True);
            Assert.That(p1.Equals(p1, p2), Is.True);

            Assert.That(((IStructuralEquatable)p1).Equals(p2), Is.True);
            Assert.That(p1.Equals((object)p2), Is.True);

            Assert.That(((IStructuralEquatable)p1).Equals(p2, EqualityComparer<string>.Default), Is.True);
            Assert.That(p1.Equals((object)p2), Is.True);

            Assert.That(p1 == p2, Is.True);
            Assert.That(p1 == p2, Is.True);
            Assert.That(p1 == p2, Is.True);
            Assert.That(p1 == p2, Is.True);

            Assert.That(p1.CompareTo(p2), Is.EqualTo(0));

            o1 = "1Jason";
            p1 = new Pair<string>(o1, o2);
            Assert.That(p1.CompareTo(p2), Is.EqualTo(-1));
            o1 = "Jason1";
            p1 = new Pair<string>(o1, o2);
            Assert.That(p1.CompareTo(p2), Is.EqualTo(1));
            Assert.That(p1.CompareTo(null), Is.EqualTo(1));

            Assert.That(((IComparable)p1).CompareTo(p2), Is.EqualTo(1));
            Assert.That(((IComparable)p1).CompareTo(null), Is.EqualTo(1));

            o1 = "Jason";
            o2 = "Wall";
            o3 = "Jason";
            o4 = "Wall1";

            p1 = new Pair<string>(o1, o2);
            p2 = new Pair<string>(o3, o4);

            Assert.That(p1, Is.Not.EqualTo(p2));
            Assert.That(p1.Equals(p2), Is.False);
            Assert.That(p1.Equals(null), Is.False);
            Assert.That(null == p1, Is.False);
            Assert.That(p1 is null, Is.False);
            Assert.That(p1 != p2, Is.True);
            Assert.That(p1 is not null, Is.True);
            Assert.That(null != p2, Is.True);
        }

        [Test]
        public void PairTest2()
        {
            var o1 = "Jason";
            var o2 = 1;
            var o3 = "Jason";
            var o4 = 1;

            var p1 = new Pair<string, int>(o1, o2);
            var p2 = new Pair<string, int>(o3, o4);

            Assert.That(p1, Is.EqualTo(p2));

            Assert.That(p1, Is.EqualTo(p2));
            Assert.That(p1.Equals(p2), Is.True);
            Assert.That(p1.Equals((object)p2), Is.True);
            Assert.That(p1.Equals(p1, p2), Is.True);

            Assert.That(((IStructuralEquatable)p1).Equals(p2), Is.True);
            Assert.That(p1.Equals((object)p2), Is.True);

            Assert.That(((IStructuralEquatable)p1).Equals(p2, EqualityComparer<string>.Default), Is.True);
            Assert.That(p1.Equals((object)p2), Is.True);

            Assert.That(p1 == p2, Is.True);
            Assert.That(p1 == p2, Is.True);
            Assert.That(p1 == p2, Is.True);
            Assert.That(p1 == p2, Is.True);

            Assert.That(p1.CompareTo(p2), Is.EqualTo(0));

            o1 = "1Jason";
            p1 = new Pair<string, int>(o1, o2);
            Assert.That(p1.CompareTo(p2), Is.EqualTo(-1));
            o1 = "Jason1";
            p1 = new Pair<string, int>(o1, o2);
            Assert.That(p1.CompareTo(p2), Is.EqualTo(1));
            Assert.That(p1.CompareTo(null), Is.EqualTo(1));

            Assert.That(((IComparable)p1).CompareTo(p2), Is.EqualTo(1));
            Assert.That(((IComparable)p1).CompareTo(null), Is.EqualTo(1));
            Assert.That(new PairComparer<string, int>().Compare(null, null), Is.EqualTo(0));
            Assert.That(new PairComparer<string, int>().Compare(null, p2), Is.EqualTo(1));

            o1 = "Jason";
            o2 = 1;
            o3 = "Jason";
            o4 = 2;

            p1 = new Pair<string, int>(o1, o2);
            p2 = new Pair<string, int>(o3, o4);

            Assert.That(p1, Is.Not.EqualTo(p2));
            Assert.That(p1.Equals(p2), Is.False);
            Assert.That(p1.Equals(null), Is.False);
            Assert.That(null == p1, Is.False);
            Assert.That(p1 is null, Is.False);
            Assert.That(p1 != p2, Is.True);
            Assert.That(p1 is not null, Is.True);
            Assert.That(null != p2, Is.True);
        }

        private static IEnumerable<TestCaseData> ForeignObjects()
        {
            yield return new TestCaseData("Jason");
            yield return new TestCaseData(1);
            yield return new TestCaseData(new object());
            yield return new TestCaseData(new Pair<int>(1, 2));
            yield return new TestCaseData(new Pair<int, string>(1, "a"));
            yield return new TestCaseData(new Pair<string, int>("Jason", 1));
            yield return new TestCaseData(new Pair<string>("Jason", "Wall"));
        }

        [TestCaseSource(nameof(ForeignObjects))]
        public void PairEqualsForeignTypeIsFalse(object other)
        {
            var same = new Pair<string>("Jason", "Wall1");
            var mixed = new Pair<string, int>("Jason", 2);

            Assert.That(same.Equals(other), Is.False);
            Assert.That(mixed.Equals(other), Is.False);
            Assert.That(same.Equals((object?)null), Is.False);
            Assert.That(mixed.Equals((object?)null), Is.False);
        }

        [TestCase(1, 2, 1, 3)]
        [TestCase(1, 2, 2, 2)]
        [TestCase(0, 0, 0, 1)]
        [TestCase(5, -5, 5, 5)]
        [TestCase(int.MaxValue, 0, int.MaxValue, 1)]
        public void PairStructuralHashUsesBothItems(int a1, int b1, int a2, int b2)
        {
            var comparer = EqualityComparer<object>.Default;

            IStructuralEquatable same1 = new Pair<int>(a1, b1);
            IStructuralEquatable same2 = new Pair<int>(a2, b2);
            Assert.That(same1.GetHashCode(comparer), Is.Not.EqualTo(same2.GetHashCode(comparer)));
            Assert.That(same1.GetHashCode(comparer), Is.EqualTo(((IStructuralEquatable)new Pair<int>(a1, b1)).GetHashCode(comparer)));

            var mixed1 = new Pair<int, int>(a1, b1);
            var mixed2 = new Pair<int, int>(a2, b2);
            Assert.That(mixed1.GetHashCode(), Is.Not.EqualTo(mixed2.GetHashCode()));
            Assert.That(mixed1.GetHashCode(), Is.EqualTo(new Pair<int, int>(a1, b1).GetHashCode()));
        }

        private class MyDiff
        {
            public string Difference { get; set; } = string.Empty;
        }
    }

    [TestFixture]
    public class DifferTests
    {
        [Test]
        public void RunCollectionDifferential_Selector_test()
        {
            var lst_a = new List<T1>
            {
                new() { Foo="1"},
                new() { Foo="2"},
                new() { Foo="3"},
                new() { Foo="4"}
            };
            var lst_b = new List<T1>
            {
                new() { Foo="2"},
                new() { Foo="4"},
                new() { Foo="5"},
                new() { Foo="6"}
            };

            var diff = Differential.RunCollectionDifferential(lst_a, lst_b, p => p.Foo);
            Assert.That(diff.OnlyInListA.Count, Is.EqualTo(2));
            Assert.That(diff.OnlyInListB.Count, Is.EqualTo(2));
            Assert.That(diff.Shared.Count, Is.EqualTo(2));
        }

        [Test]
        public void RunCollectionDifferential_Selector_Two_Types_test()
        {
            var lst_a = new List<T1>
            {
                new() { Foo="1"},
                new() { Foo="2"},
                new() { Foo="3"},
                new() { Foo="4"}
            };
            var lst_b = new List<T2>
            {
                new() { Foo="2"},
                new() { Foo="4"},
                new() { Foo="5"},
                new() { Foo="6"}
            };

            var diff = Differential.RunCollectionDifferential(lst_a, lst_b, p => p.Foo, p => p.Foo);
            Assert.That(diff.OnlyInListA.Count, Is.EqualTo(2));
            Assert.That(diff.OnlyInListB.Count, Is.EqualTo(2));
            Assert.That(diff.Shared.Count, Is.EqualTo(2));
        }
    }

    [TestFixture]
    public class DifferKeyMergeTests
    {
        // a, b, expected only in a, expected only in b, expected shared
        [TestCase(new string[0], new string[0], new string[0], new string[0], new string[0])]
        [TestCase(new string[0], new[] { "1", "2" }, new string[0], new[] { "1", "2" }, new string[0])]
        [TestCase(new[] { "1", "2" }, new string[0], new[] { "1", "2" }, new string[0], new string[0])]
        [TestCase(new[] { "1" }, new string[0], new[] { "1" }, new string[0], new string[0])]
        [TestCase(new string[0], new[] { "1" }, new string[0], new[] { "1" }, new string[0])]
        [TestCase(new[] { "1" }, new[] { "1" }, new string[0], new string[0], new[] { "1" })]
        [TestCase(new[] { "1" }, new[] { "2" }, new[] { "1" }, new[] { "2" }, new string[0])]
        [TestCase(new[] { "2" }, new[] { "1" }, new[] { "2" }, new[] { "1" }, new string[0])]
        [TestCase(new[] { "1", "2", "3", "4" }, new[] { "2", "4", "5", "6" }, new[] { "1", "3" }, new[] { "5", "6" }, new[] { "2", "4" })]
        [TestCase(new[] { "4", "3", "2", "1" }, new[] { "6", "5", "4", "2" }, new[] { "1", "3" }, new[] { "5", "6" }, new[] { "2", "4" })]
        [TestCase(new[] { "5", "6" }, new[] { "1", "2" }, new[] { "5", "6" }, new[] { "1", "2" }, new string[0])]
        [TestCase(new[] { "1", "1" }, new[] { "1" }, new[] { "1" }, new string[0], new[] { "1" })]
        [TestCase(new[] { "1" }, new[] { "1", "1" }, new string[0], new[] { "1" }, new[] { "1" })]
        public void AllItemsReported(string[] a, string[] b, string[] onlyA, string[] onlyB, string[] shared)
        {
            var lst_a = a.Select(k => new T1 { Foo = k }).ToList();
            var lst_b = b.Select(k => new T1 { Foo = k }).ToList();
            var lst_b2 = b.Select(k => new T2 { Foo = k }).ToList();

            var simple = Differential.RunCollectionDifferential(lst_a, lst_b, p => p.Foo);
            Assert.That(simple.OnlyInListA.Select(o => o.Foo), Is.EqualTo(onlyA));
            Assert.That(simple.OnlyInListB.Select(o => o.Foo), Is.EqualTo(onlyB));
            Assert.That(simple.Shared.Select(o => o.Foo), Is.EqualTo(shared));

            var twoTypes = Differential.RunCollectionDifferential(lst_a, lst_b2, p => p.Foo, p => p.Foo);
            Assert.That(twoTypes.OnlyInListA.Select(o => o.Foo), Is.EqualTo(onlyA));
            Assert.That(twoTypes.OnlyInListB.Select(o => o.Foo), Is.EqualTo(onlyB));
            Assert.That(twoTypes.Shared.Select(o => o.A.Foo), Is.EqualTo(shared));
            Assert.That(twoTypes.Shared.Select(o => o.B.Foo), Is.EqualTo(shared));

            var withDiffer = Differential.RunCollectionDifferential<T1, string, string>(
                lst_a,
                lst_b,
                p => p.Foo,
                (x, y) => new ObjectDifferential<T1, string>(x, y, [x.Foo])
            );
            Assert.That(withDiffer.OnlyInListA.Select(o => o.Foo), Is.EqualTo(onlyA));
            Assert.That(withDiffer.OnlyInListB.Select(o => o.Foo), Is.EqualTo(onlyB));
            Assert.That(withDiffer.Shared.Select(o => o.A.Foo), Is.EqualTo(shared));
            Assert.That(withDiffer.Modified.Select(o => o.Differences[0]), Is.EqualTo(shared));
            Assert.That(withDiffer.HasDifferences, Is.EqualTo(onlyA.Length > 0 || onlyB.Length > 0 || shared.Length > 0));
        }
    }

    internal class StringComparer : IComparer<string>
    {
        #region Implementation of IComparer<in string>

        public int Compare(string? x, string? y)
        {
            return string.Compare(x, y, StringComparison.Ordinal);
        }

        #endregion Implementation of IComparer<in string>
    }

    internal class T1
    {
        public string Foo { get; set; } = string.Empty;

        public int Bar { get; set; } = -1;

        public List<string> Jason { get; set; } = [];
    }

    internal class T2
    {
        public string Foo { get; set; } = string.Empty;

        public List<int> Jason { get; set; } = [];

        public bool Wall { get; set; } = false;
    }
}