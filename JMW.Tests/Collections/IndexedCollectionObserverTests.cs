using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace JMW.Collections.Tests
{
    // How items tell the collections holding them about indexed-property changes.
    [TestFixture]
    public class IndexedCollectionObserverTests
    {
        private sealed class Item : IndexedClass
        {
            private string key = string.Empty;
            private string group = string.Empty;
            private string note = string.Empty;

            [Indexed(true)]
            public string Key { get => key; set => Set(ref key, value); }

            [Indexed]
            public string Group { get => group; set => Set(ref group, value); }

            public string Note { get => note; set => Set(ref note, value); }
        }

        [Test]
        public void ChangingAnIndexedPropertyMovesTheItemInTheIndex()
        {
            var c = new IndexedCollection<Item>();
            var item = new Item { Key = "a", Group = "g1" };
            c.Add(item);

            item.Key = "b";
            item.Group = "g2";

            Assert.That(c.TryGetByUniqueIndex("Key", "a", out _), Is.False);
            Assert.That(c.TryGetByUniqueIndex("Key", "b", out var found) && ReferenceEquals(found, item), Is.True);
            Assert.That(c.TryGetByIndex("Group", "g1", out _), Is.False);
            Assert.That(c.GetByIndex("Group", "g2"), Is.EqualTo(new[] { item }));
        }

        [Test]
        public void ItemInTwoCollectionsUpdatesBoth()
        {
            var first = new IndexedCollection<Item>();
            var second = new IndexedCollection<Item>();
            var item = new Item { Key = "a" };
            first.Add(item);
            second.Add(item);

            item.Key = "b";

            Assert.That(first.TryGetByUniqueIndex("Key", "b", out _), Is.True);
            Assert.That(second.TryGetByUniqueIndex("Key", "b", out _), Is.True);
            Assert.That(first.TryGetByUniqueIndex("Key", "a", out _), Is.False);
            Assert.That(second.TryGetByUniqueIndex("Key", "a", out _), Is.False);
        }

        [Test]
        public void RemovedItemNoLongerUpdatesTheCollection()
        {
            var c = new IndexedCollection<Item>();
            var removed = new Item { Key = "a" };
            var kept = new Item { Key = "b" };
            c.Add(removed);
            c.Add(kept);
            c.Remove(removed);

            // would collide with kept's unique key if the collection were still listening
            Assert.That(() => removed.Key = "b", Throws.Nothing);
            Assert.That(c.TryGetByUniqueIndex("Key", "b", out var found) && ReferenceEquals(found, kept), Is.True);
        }

        [Test]
        public void RemovingFromOneCollectionKeepsTheOtherListening()
        {
            var first = new IndexedCollection<Item>();
            var second = new IndexedCollection<Item>();
            var item = new Item { Key = "a" };
            first.Add(item);
            second.Add(item);
            first.Remove(item);

            item.Key = "b";

            Assert.That(second.TryGetByUniqueIndex("Key", "b", out _), Is.True);
            Assert.That(first.TryGetByUniqueIndex("Key", "b", out _), Is.False);
        }

        [Test]
        public void ClearingTheItemsEventsDoesNotDetachItsCollection()
        {
            var c = new IndexedCollection<Item>();
            var item = new Item { Key = "a" };
            c.Add(item);

            item.ClearEvents();
            item.Key = "b";

            Assert.That(c.TryGetByUniqueIndex("Key", "b", out _), Is.True);
        }

        [Test]
        public void UniqueViolationOnChangeThrowsAndLeavesTheValueUnchanged()
        {
            var c = new IndexedCollection<Item>();
            var a = new Item { Key = "a" };
            c.Add(a);
            c.Add(new Item { Key = "b" });

            Assert.That(() => a.Key = "b", Throws.ArgumentException);
            Assert.That(a.Key, Is.EqualTo("a"));
        }

        [Test]
        public void PublicEventsStillFireAlongsideTheCollection()
        {
            var c = new IndexedCollection<Item>();
            var item = new Item { Key = "a", Note = "x" };
            c.Add(item);

            var calls = new List<string>();
            item.IndexedPropertyChanged += (_, e) => calls.Add($"indexed {e.PropertyName} {e.Before}->{e.After}");
            item.PropertyChanging += (_, e) => calls.Add($"changing {e.PropertyName}");
            item.PropertyChanged += (_, e) => calls.Add($"changed {e.PropertyName}");

            item.Key = "b";
            item.Note = "y";

            Assert.That(calls, Is.EqualTo(new[]
            {
                "indexed Key a->b", "changing Key", "changed Key",
                "indexed Note x->y", "changing Note", "changed Note",
            }));
            Assert.That(c.TryGetByUniqueIndex("Key", "b", out _), Is.True);
        }

        [Test]
        public void EventCountsStartAtZeroAndExcludeCollections()
        {
            var item = new Item();
            Assert.That(item.IndexedPropertyChangedEventCount, Is.Zero);
            Assert.That(item.PropertyChangedEventCount, Is.Zero);
            Assert.That(item.PropertyChangingEventCount, Is.Zero);

            new IndexedCollection<Item>().Add(item);

            Assert.That(item.IndexedPropertyChangedEventCount, Is.Zero);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference AddToThrowawayCollection(Item item)
        {
            var c = new IndexedCollection<Item>();
            c.Add(item);
            return new WeakReference(c);
        }

        [Test]
        public void DroppedCollectionCanBeCollectedWhileItsItemsLive()
        {
            var item = new Item { Key = "a" };
            var collection = AddToThrowawayCollection(item);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.That(collection.IsAlive, Is.False);

            // the item notices the dead collection and carries on
            Assert.That(() => item.Key = "b", Throws.Nothing);
            Assert.That(item.Key, Is.EqualTo("b"));
        }
    }
}
