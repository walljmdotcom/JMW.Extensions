using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace JMW.Collections.Tests
{
    // How property values become index keys, and that every path (add, change, remove, lookup) agrees.
    [TestFixture]
    public class IndexedCollectionKeyTests
    {
        private sealed class Reading : IndexedClass
        {
            private string id = string.Empty;
            private double value;
            private int count;
            private Dictionary<string, string> labels = [];
            private List<string>? codes = [];

            [Indexed(true)]
            public string Id { get => id; set => Set(ref id, value); }

            [Indexed]
            public double Value { get => value; set => Set(ref this.value, value); }

            [Indexed]
            public int Count { get => count; set => Set(ref count, value); }

            [Indexed]
            public Dictionary<string, string> Labels { get => labels; set => Set(ref labels, value); }

            [Indexed(true)]
            public List<string>? Codes { get => codes; set => Set(ref codes, value); }
        }

        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        [TestCase("en-US")]
        public void FormattableKeysUseTheInvariantCulture(string culture)
        {
            var previous = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);
            try
            {
                var c = new IndexedCollection<Reading>();
                var r = new Reading { Id = "a", Value = 1.5 };
                c.Add(r);

                Assert.That(c.GetIndex("Value").Keys, Is.EquivalentTo(new[] { "1.5" }));
                Assert.That(c.TryGetByIndex("Value", 1.5, out var byNumber) && byNumber.Contains(r), Is.True);
                Assert.That(c.TryGetByIndex("Value", "1.5", out var byText) && byText.Contains(r), Is.True);
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = previous;
            }
        }

        [TestCase(0)]
        [TestCase(7)]
        [TestCase(-3)]
        [TestCase(int.MaxValue)]
        public void ValueTypeKeysMatchWhetherLookedUpByValueOrText(int count)
        {
            var c = new IndexedCollection<Reading>();
            var r = new Reading { Id = "a", Count = count };
            c.Add(r);

            Assert.That(c.GetByIndex("Count", count), Is.EqualTo(new[] { r }));
            Assert.That(c.GetByIndex("Count", count.ToString(System.Globalization.CultureInfo.InvariantCulture)), Is.EqualTo(new[] { r }));
        }

        [Test]
        public void NonUniqueDictionaryIsKeyedByItsValuesOnAddChangeAndRemove()
        {
            var c = new IndexedCollection<Reading>();
            var r = new Reading { Id = "a", Labels = new() { ["k1"] = "red", ["k2"] = "blue" } };
            c.Add(r);
            Assert.That(c.GetIndex("Labels").Keys, Is.EquivalentTo(new[] { "red", "blue" }));

            r.Labels = new() { ["k1"] = "green" };
            Assert.That(c.GetIndex("Labels").Keys, Is.EquivalentTo(new[] { "green" }));

            c.Remove(r);
            Assert.That(c.GetIndex("Labels"), Is.Empty);
        }

        [Test]
        public void ScalarUniqueViolationOnChangeLeavesEveryIndexIntact()
        {
            var c = new IndexedCollection<Reading>();
            var a = new Reading { Id = "a" };
            var b = new Reading { Id = "b" };
            c.Add(a);
            c.Add(b);

            Assert.That(() => a.Id = "b", Throws.ArgumentException);

            Assert.That(a.Id, Is.EqualTo("a"));
            Assert.That(c.GetByUniqueIndex("Id", "a"), Is.SameAs(a));
            Assert.That(c.GetByUniqueIndex("Id", "b"), Is.SameAs(b));
        }

        private static IEnumerable<TestCaseData> CollectionViolations()
        {
            yield return new TestCaseData(new List<string> { "x", "taken" }).SetName("one new code is held by another item");
            yield return new TestCaseData(new List<string> { "taken" }).SetName("the only new code is held by another item");
            yield return new TestCaseData(new List<string> { "x", "x" }).SetName("the new codes repeat");
        }

        [TestCaseSource(nameof(CollectionViolations))]
        public void CollectionUniqueViolationOnChangeLeavesEveryIndexIntact(List<string> newCodes)
        {
            var c = new IndexedCollection<Reading>();
            var a = new Reading { Id = "a", Codes = ["a1", "a2"] };
            var other = new Reading { Id = "b", Codes = ["taken"] };
            c.Add(a);
            c.Add(other);

            Assert.That(() => a.Codes = newCodes, Throws.ArgumentException);

            Assert.That(a.Codes, Is.EqualTo(new[] { "a1", "a2" }));
            Assert.That(c.GetUniqueIndex("Codes").Keys, Is.EquivalentTo(new[] { "a1", "a2", "taken" }));
            Assert.That(c.GetByUniqueIndex("Codes", "a1"), Is.SameAs(a));
            Assert.That(c.GetByUniqueIndex("Codes", "taken"), Is.SameAs(other));
        }

        [Test]
        public void ChangingAUniqueCollectionToCodesItAlreadyHoldsIsAllowed()
        {
            var c = new IndexedCollection<Reading>();
            var a = new Reading { Id = "a", Codes = ["a1", "a2"] };
            c.Add(a);

            a.Codes = ["a2", "a3"];

            Assert.That(c.GetUniqueIndex("Codes").Keys, Is.EquivalentTo(new[] { "a2", "a3" }));
        }

        [Test]
        public void NullValuesAreNeverIndexed()
        {
            var c = new IndexedCollection<Reading>();
            var r = new Reading { Id = "a", Codes = ["x"] };
            c.Add(r);

            r.Codes = null;
            Assert.That(c.GetUniqueIndex("Codes"), Is.Empty);

            r.Codes = ["y"];
            Assert.That(c.GetUniqueIndex("Codes").Keys, Is.EquivalentTo(new[] { "y" }));
        }

        [Test]
        public void CollectionsOfTheSameTypeShareNoIndexState()
        {
            var first = new IndexedCollection<Reading>();
            var second = new IndexedCollection<Reading>();
            first.Add(new Reading { Id = "a" });

            Assert.That(second.TryGetByUniqueIndex("Id", "a", out _), Is.False);
            Assert.That(() => second.Add(new Reading { Id = "a" }), Throws.Nothing);
        }
    }
}
