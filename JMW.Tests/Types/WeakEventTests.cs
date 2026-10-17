using JMW.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JMW.Types.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class WeakEventTests
    {
        private static readonly List<string> staticCalls = [];

        private static void StaticHandler(object? sender, EventArgs e)
        {
            staticCalls.Add("static");
        }

        private static void StaticHandler2(object? sender, EventArgs e)
        {
            staticCalls.Add("static2");
        }

        private sealed class Listener
        {
            public List<string> Calls { get; } = [];

            public void Handler(object? sender, EventArgs e)
            {
                Calls.Add("instance");
            }

            public void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
            {
                Calls.Add("changed:" + e.PropertyName);
            }

            public void OnPropertyChanged2(object? sender, PropertyChangedEventArgs e)
            {
                Calls.Add("changed2:" + e.PropertyName);
            }
        }

        private sealed class Observed : ObservableClass
        {
            private int value;

            public int Value
            {
                get => value;
                set => Set(ref this.value, value);
            }
        }

        [SetUp]
        public void ResetStatic()
        {
            staticCalls.Clear();
        }

        [TestCase(1)]
        [TestCase(3)]
        public void StaticHandlerIsInvokedOnEveryRaise(int raises)
        {
            var source = new WeakEventSource<EventArgs>();
            source.Subscribe(StaticHandler);

            for (var i = 0; i < raises; i++)
                source.Raise(this, EventArgs.Empty);

            Assert.That(staticCalls, Has.Count.EqualTo(raises));
            Assert.That(source.HandlerCount, Is.EqualTo(1));
        }

        [Test]
        public void StaticAndInstanceHandlersTogether()
        {
            var listener = new Listener();
            var source = new WeakEventSource<EventArgs>();
            source.Subscribe(StaticHandler);
            source.Subscribe(listener.Handler);

            source.Raise(this, EventArgs.Empty);

            Assert.That(staticCalls, Is.EqualTo(new[] { "static" }));
            Assert.That(listener.Calls, Is.EqualTo(new[] { "instance" }));
            GC.KeepAlive(listener);
        }

        [Test]
        public void StaticHandlerUnsubscribes()
        {
            var source = new WeakEventSource<EventArgs>();
            source.Subscribe(StaticHandler);
            source.Unsubscribe(StaticHandler);
            source.Raise(this, EventArgs.Empty);

            Assert.That(staticCalls, Is.Empty);
            Assert.That(source.HandlerCount, Is.EqualTo(0));
        }

        [Test]
        public void MulticastSubscribeAndUnsubscribe()
        {
            var source = new WeakEventSource<EventArgs>();
            EventHandler<EventArgs> both = StaticHandler;
            both += StaticHandler2;

            source.Subscribe(both);
            source.Raise(this, EventArgs.Empty);
            Assert.That(staticCalls, Is.EqualTo(new[] { "static", "static2" }));

            source.Unsubscribe(both);
            Assert.That(source.HandlerCount, Is.EqualTo(0));
        }

        [Test]
        public void ObservableClassKeepsEveryHandlerOfAMulticastDelegate()
        {
            var listener = new Listener();
            var observed = new Observed();
            PropertyChangedEventHandler both = listener.OnPropertyChanged;
            both += listener.OnPropertyChanged2;

            observed.PropertyChanged += both;
            Assert.That(observed.PropertyChangedEventCount, Is.EqualTo(2));

            observed.Value = 5;
            Assert.That(listener.Calls, Is.EqualTo(new[] { "changed:Value", "changed2:Value" }));

            observed.PropertyChanged -= both;
            Assert.That(observed.PropertyChangedEventCount, Is.EqualTo(0));
            GC.KeepAlive(listener);
        }

        public enum ClosureKind
        {
            CapturingLambda,
            AnonymousMethod,
            LocalFunction,
            NonCapturingLambda,
        }

        private sealed class LoggingListener(List<string> log)
        {
            public void Handler(object? sender, EventArgs e)
            {
                log.Add("instance");
            }
        }

        private static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SubscribeClosure(WeakEventSource<EventArgs> source, List<string> log, ClosureKind kind)
        {
            var tag = kind.ToString();
            switch (kind)
            {
                case ClosureKind.CapturingLambda:
                    source.Subscribe((s, e) => log.Add(tag));
                    break;

                case ClosureKind.AnonymousMethod:
                    source.Subscribe(delegate (object? s, EventArgs e) { log.Add(tag); });
                    break;

                case ClosureKind.LocalFunction:
                    void Local(object? s, EventArgs e) => log.Add(tag);
                    source.Subscribe(Local);
                    break;

                case ClosureKind.NonCapturingLambda:
                    source.Subscribe((s, e) => staticCalls.Add("noncapturing"));
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeListener(WeakEventSource<EventArgs> source, List<string> log)
        {
            var listener = new LoggingListener(log);
            source.Subscribe(listener.Handler);
            return new WeakReference(listener);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeListenerAndClosure(WeakEventSource<EventArgs> source, List<string> log)
        {
            var listener = new LoggingListener(log);
            EventHandler<EventArgs> both = listener.Handler;
            both += (s, e) => log.Add("closure");
            source.Subscribe(both);
            return new WeakReference(listener);
        }

        [TestCase(ClosureKind.CapturingLambda)]
        [TestCase(ClosureKind.AnonymousMethod)]
        [TestCase(ClosureKind.LocalFunction)]
        [TestCase(ClosureKind.NonCapturingLambda)]
        public void ClosureHandlerSurvivesGarbageCollection(ClosureKind kind)
        {
            var log = new List<string>();
            var source = new WeakEventSource<EventArgs>();
            SubscribeClosure(source, log, kind);

            CollectGarbage();
            source.Raise(this, EventArgs.Empty);
            source.Raise(this, EventArgs.Empty);

            var fired = kind == ClosureKind.NonCapturingLambda ? staticCalls : log;
            Assert.That(fired, Has.Count.EqualTo(2));
            Assert.That(source.HandlerCount, Is.EqualTo(1));
        }

        [Test]
        public void UnreferencedInstanceSubscriberDoesNotFireAfterCollection()
        {
            var log = new List<string>();
            var source = new WeakEventSource<EventArgs>();
            var listener = SubscribeListener(source, log);

            CollectGarbage();
            Assume.That(listener.IsAlive, Is.False, "the runtime kept the listener alive");

            source.Raise(this, EventArgs.Empty);
            Assert.That(log, Is.Empty);
            Assert.That(source.HandlerCount, Is.EqualTo(0));
        }

        [Test]
        public void MulticastKeepsClosurePartAndDropsCollectedInstancePart()
        {
            var log = new List<string>();
            var source = new WeakEventSource<EventArgs>();
            var listener = SubscribeListenerAndClosure(source, log);

            CollectGarbage();
            Assume.That(listener.IsAlive, Is.False, "the runtime kept the listener alive");

            source.Raise(this, EventArgs.Empty);
            Assert.That(log, Is.EqualTo(new[] { "closure" }));
            Assert.That(source.HandlerCount, Is.EqualTo(1));
        }

        [Test]
        public void ReferencedInstanceSubscriberStillFiresAfterCollection()
        {
            var log = new List<string>();
            var listener = new LoggingListener(log);
            var source = new WeakEventSource<EventArgs>();
            source.Subscribe(listener.Handler);

            CollectGarbage();
            source.Raise(this, EventArgs.Empty);

            Assert.That(log, Is.EqualTo(new[] { "instance" }));
            GC.KeepAlive(listener);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void StronglyHeldClosureUnsubscribes(int subscriptions)
        {
            var log = new List<string>();
            var tag = "closure";
            EventHandler<EventArgs> handler = (s, e) => log.Add(tag);
            var source = new WeakEventSource<EventArgs>();
            for (var i = 0; i < subscriptions; i++)
                source.Subscribe(handler);

            source.Unsubscribe(handler);
            Assert.That(source.HandlerCount, Is.EqualTo(subscriptions - 1));

            CollectGarbage();
            source.Raise(this, EventArgs.Empty);
            Assert.That(log, Has.Count.EqualTo(subscriptions - 1));
        }

        [Test]
        public void UnsubscribingADifferentClosureLeavesTheOriginal()
        {
            var log = new List<string>();
            EventHandler<EventArgs> Make(string tag) => (s, e) => log.Add(tag);

            var first = Make("first");
            var second = Make("second");
            var source = new WeakEventSource<EventArgs>();
            source.Subscribe(first);

            // same method, different closure object
            source.Unsubscribe(second);
            source.Raise(this, EventArgs.Empty);

            Assert.That(source.HandlerCount, Is.EqualTo(1));
            Assert.That(log, Is.EqualTo(new[] { "first" }));
        }

        [Test]
        public void ObservableClassSingleHandler()
        {
            var listener = new Listener();
            var observed = new Observed();

            observed.PropertyChanged += listener.OnPropertyChanged;
            observed.Value = 1;
            observed.Value = 1;
            observed.Value = 2;

            Assert.That(listener.Calls, Is.EqualTo(new[] { "changed:Value", "changed:Value" }));

            observed.PropertyChanged -= listener.OnPropertyChanged;
            Assert.That(observed.PropertyChangedEventCount, Is.EqualTo(0));
            GC.KeepAlive(listener);
        }
    }
}
