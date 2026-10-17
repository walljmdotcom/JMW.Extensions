using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace JMW.Collections.Tests
{
    [TestFixture]
    public class DequeTests
    {
        private static void AssertSame(Deque<string> deque, List<string> model, string context)
        {
            Assert.That(deque.Count, Is.EqualTo(model.Count), context);
            Assert.That(deque.ToList(), Is.EqualTo(model), context);
            for (var i = 0; i < model.Count; i++)
                Assert.That(deque[i], Is.SameAs(model[i]), context);

            var copy = new string[model.Count + 2];
            deque.CopyTo(copy, 1);
            Assert.That(copy.Skip(1).Take(model.Count), Is.EqualTo(model), context);
        }

        // Every operation applied to both a Deque and a List, which must agree after each step. The seeds cover
        // wrap-around, growth while wrapped, and removal from both halves.
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(42)]
        [TestCase(1234)]
        public void MatchesListUnderRandomOperations(int seed)
        {
            var random = new Random(seed);
            var deque = new Deque<string>();
            var model = new List<string>();
            var next = 0;

            for (var step = 0; step < 3000; step++)
            {
                var op = random.Next(6);
                var context = $"seed {seed}, step {step}, op {op}, count {model.Count}";
                switch (op)
                {
                    case 0:
                    case 1:
                        var added = "v" + next++;
                        deque.Add(added);
                        model.Add(added);
                        break;
                    case 2:
                        var inserted = "v" + next++;
                        var insertAt = random.Next(model.Count + 1);
                        deque.Insert(insertAt, inserted);
                        model.Insert(insertAt, inserted);
                        break;
                    case 3:
                    case 4:
                        if (model.Count == 0) break;
                        var removeAt = random.Next(model.Count);
                        deque.RemoveAt(removeAt);
                        model.RemoveAt(removeAt);
                        break;
                    case 5:
                        if (model.Count == 0) break;
                        var target = model[random.Next(model.Count)];
                        Assert.That(deque.IndexOfReference(target), Is.EqualTo(model.IndexOf(target)), context);
                        Assert.That(deque.IndexOf(target), Is.EqualTo(model.IndexOf(target)), context);
                        break;
                }

                if (step % 50 == 0) AssertSame(deque, model, context);
            }

            AssertSame(deque, model, $"seed {seed}, end");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void RemovingFromTheFrontRepeatedlyWrapsTheBuffer(int extra)
        {
            var deque = new Deque<string>();
            var model = new List<string>();
            for (var round = 0; round < 20; round++)
            {
                for (var i = 0; i < 3 + extra; i++)
                {
                    var value = $"r{round}i{i}";
                    deque.Add(value);
                    model.Add(value);
                }

                deque.RemoveAt(0);
                model.RemoveAt(0);
                AssertSame(deque, model, $"round {round}");
            }
        }

        [Test]
        public void TruncateDropsTheTail()
        {
            var deque = new Deque<string>();
            foreach (var s in new[] { "a", "b", "c", "d" }) deque.Add(s);
            deque.RemoveAt(0);
            deque.Add("e");

            deque.Truncate(2);

            Assert.That(deque.ToList(), Is.EqualTo(new[] { "b", "c" }));
            Assert.That(() => deque.Truncate(3), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        public void IndexOutOfRangeThrows(int index)
        {
            var empty = new Deque<string>();
            Assert.That(() => empty[index], Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => empty.RemoveAt(index), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void InsertOutOfRangeThrows(int index)
        {
            var deque = new Deque<string>();
            deque.Add("a");
            Assert.That(() => deque.Insert(index, "x"), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void ModifyingWhileEnumeratingThrows()
        {
            var deque = new Deque<string>();
            deque.Add("a");
            deque.Add("b");

            Assert.That(() =>
            {
                foreach (var _ in deque) deque.Add("c");
            }, Throws.InvalidOperationException);
        }

        [Test]
        public void IndexOfReferenceDistinguishesEqualStrings()
        {
            var deque = new Deque<string>();
            var first = new string('x', 3);
            var second = new string('x', 3);
            deque.Add(first);
            deque.Add(second);

            Assert.That(deque.IndexOfReference(second), Is.EqualTo(1));
            Assert.That(deque.IndexOf(second), Is.Zero);
            Assert.That(deque.IndexOfReference(new string('x', 3)), Is.EqualTo(-1));
        }
    }
}
