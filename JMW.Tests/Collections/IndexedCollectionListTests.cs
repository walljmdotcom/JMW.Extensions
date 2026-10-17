using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;

namespace JMW.Collections.Tests
{
    // IndexedCollection as an IList: order, removal, and CollectionChanged.
    [TestFixture]
    public class IndexedCollectionListTests
    {
        private sealed class Item : IndexedClass
        {
            private string key = string.Empty;
            private int group;

            [Indexed(true)]
            public string Key { get => key; set => Set(ref key, value); }

            [Indexed]
            public int Group { get => group; set => Set(ref group, value); }

            public override string ToString() => Key;
        }

        private static (IndexedCollection<Item> Collection, Item[] Items) Build(int count)
        {
            var c = new IndexedCollection<Item>();
            var items = Enumerable.Range(0, count).Select(i => new Item { Key = "k" + i, Group = i % 3 }).ToArray();
            foreach (var item in items)
                c.Add(item);
            return (c, items);
        }

        private static List<string> Record(IndexedCollection<Item> c)
        {
            var events = new List<string>();
            c.CollectionChanged += (_, e) => events.Add(e.Action switch
            {
                NotifyCollectionChangedAction.Add => $"add {e.NewItems?[0]} at {e.NewStartingIndex}",
                NotifyCollectionChangedAction.Remove => $"remove {e.OldItems?[0]} at {e.OldStartingIndex}",
                NotifyCollectionChangedAction.Replace => $"replace {e.OldItems?[0]} with {e.NewItems?[0]} at {e.NewStartingIndex}",
                _ => e.Action.ToString().ToLowerInvariant(),
            });
            return events;
        }

        [Test]
        public void CollectionChangedReportsEachOperationWithItsIndex()
        {
            var (c, items) = Build(3);
            var events = Record(c);

            c.Add(new Item { Key = "k3" });
            c.Insert(1, new Item { Key = "x" });
            c.Remove(items[2]);
            c.RemoveAt(0);
            c[0] = new Item { Key = "y" };
            c.Clear();

            Assert.That(events, Is.EqualTo(new[]
            {
                "add k3 at 3",
                "add x at 1",
                "remove k2 at 3",
                "remove k0 at 0",
                "replace x with y at 0",
                "reset",
            }));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(7)]
        [TestCase(50)]
        public void RemovingInAnyOrderKeepsTheRestInOrderAndIndexed(int count)
        {
            foreach (var order in new[] { "oldest", "newest", "middle-out" })
            {
                var (c, items) = Build(count);
                var sequence = order switch
                {
                    "oldest" => items,
                    "newest" => items.Reverse().ToArray(),
                    _ => items.OrderBy(i => Math.Abs(Array.IndexOf(items, i) - count / 2)).ToArray(),
                };

                var remaining = items.ToList();
                foreach (var item in sequence)
                {
                    Assert.That(c.Remove(item), Is.True, order);
                    remaining.Remove(item);

                    Assert.That(c.ToList(), Is.EqualTo(remaining), order);
                    Assert.That(c.TryGetByUniqueIndex("Key", item.Key, out _), Is.False, order);
                }

                Assert.That(c.GetIndex("Group"), Is.Empty, order);
            }
        }

        [Test]
        public void RemovingANonMemberWithAMembersKeyLeavesTheMemberIndexed()
        {
            var (c, items) = Build(2);
            var impostor = new Item { Key = items[0].Key, Group = items[0].Group };

            Assert.That(c.Remove(impostor), Is.False);

            Assert.That(c.Count, Is.EqualTo(2));
            Assert.That(c.GetByUniqueIndex("Key", items[0].Key), Is.SameAs(items[0]));
            Assert.That(c.GetByIndex("Group", items[0].Group), Does.Contain(items[0]));
        }

        [Test]
        public void ARemovedItemCanBeRemovedAgainHarmlessly()
        {
            var (c, items) = Build(2);
            Assert.That(c.Remove(items[0]), Is.True);
            Assert.That(c.Remove(items[0]), Is.False);
            Assert.That(c.ToList(), Is.EqualTo(new[] { items[1] }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RemoveByPredicateGivesTheSameResultWithOrWithoutListeners(bool listening)
        {
            var (c, items) = Build(10);
            var events = listening ? Record(c) : [];

            c.Remove(i => i.Group == 1);

            var expected = items.Where(i => i.Group != 1).ToList();
            Assert.That(c.ToList(), Is.EqualTo(expected));
            Assert.That(c.TryGetByIndex("Group", 1, out _), Is.False);
            foreach (var removed in items.Where(i => i.Group == 1))
            {
                Assert.That(c.TryGetByUniqueIndex("Key", removed.Key, out _), Is.False);

                // a removed item no longer moves inside the collection's index
                removed.Group = 0;
            }

            Assert.That(c.GetByIndex("Group", 0), Is.EquivalentTo(items.Where(i => i.Group == 0 && expected.Contains(i))));
            if (listening)
                Assert.That(events, Has.Count.EqualTo(items.Length - expected.Count));
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void RemoveAtOutOfRangeThrowsAndChangesNothing(int index)
        {
            var (c, items) = Build(2);

            Assert.That(() => c.RemoveAt(index), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(c.ToList(), Is.EqualTo(items));
        }

        [Test]
        public void LookupResultsCannotBeUsedToModifyTheIndex()
        {
            var (c, _) = Build(6);

            Assert.That(c.GetByIndex("Group", 0), Is.Not.InstanceOf<ICollection<Item>>());
            Assert.That(c.TryGetByIndex("Group", 0, out var bucket) ? bucket : null, Is.Not.InstanceOf<ICollection<Item>>());
            Assert.That(c.GetIndex("Group"), Is.Not.InstanceOf<IDictionary<string, IReadOnlyList<Item>>>());
            Assert.That(
                () => ((IDictionary<string, Item>)c.GetUniqueIndex("Key")).Remove("k0"),
                Throws.TypeOf<NotSupportedException>()
            );
            Assert.That(c.GetUniqueIndex("Key"), Has.Count.EqualTo(6));
        }

        [Test]
        public void LookupResultsAreViewsOfTheIndex()
        {
            var (c, items) = Build(6);
            var group0 = c.GetByIndex("Group", 0);
            var byGroup = c.GetIndex("Group");
            var byKey = c.GetUniqueIndex("Key");

            var added = new Item { Key = "new", Group = 0 };
            c.Add(added);
            c.Remove(items[0]);

            Assert.That(group0, Is.EqualTo(new[] { items[3], added }));
            Assert.That(byGroup["0"], Is.EqualTo(new[] { items[3], added }));
            Assert.That(byKey.ContainsKey("new"), Is.True);
            Assert.That(byKey.ContainsKey("k0"), Is.False);
        }

        [Test]
        public void ChangingTheCollectionWhileEnumeratingAViewThrows()
        {
            var (c, _) = Build(6);

            // the copy-free result means this needs .ToList(), as it would over a List<T>
            Assert.That(() =>
            {
                foreach (var item in c.GetByIndex("Group", 0))
                    item.Group = 1;
            }, Throws.InvalidOperationException);

            Assert.That(() =>
            {
                foreach (var item in c.GetByIndex("Group", 1).ToList())
                    item.Group = 2;
            }, Throws.Nothing);
        }

        [Test]
        public void IsReadOnlyIsFalse()
        {
            Assert.That(new IndexedCollection<Item>().IsReadOnly, Is.False);
        }
    }
}
