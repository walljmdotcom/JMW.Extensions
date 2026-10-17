using JMW.Collections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;

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
