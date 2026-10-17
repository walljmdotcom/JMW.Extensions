using NUnit.Framework;
using System;

namespace JMW.Functional.Tests
{
    [TestFixture]
    public class MaybeTests
    {
        [Test]
        public void OptionalConstructorTest1()
        {
            string? v = null;
            var c = new Maybe<string>(v);
            Assert.That(false, Is.EqualTo(c.Success));
        }

        [Test]
        public void ComparisonsTest1()
        {
            var m1 = new Maybe<string?>("blah");
            var m2 = new Maybe<string?>("blah");

            var m3 = new Maybe<string?>((string?)null);
            var m4 = new Maybe<string?>("blah1");
            var m5 = new Maybe<string?>((Exception?)null);
            Maybe<string?>? m6 = null;

            Assert.That(m1 == m2);
            Assert.That(m1 != m3);
            Assert.That(m1 != "hohum");
            Assert.That(m1 == "blah");
            Assert.That(m1 != m5);
            Assert.That(m1 != m4);
            Assert.That((Maybe<string?>?)null != "woo");
            Assert.That((Maybe<string?>?)null == (string?)null);
            Assert.That("woo" != (Maybe<string?>?)null);
            Assert.That(m6 == (Maybe<string?>?)null);
            Assert.That("blah" == m1);
            Assert.That(!m5.Equals("woo"));
            Assert.That(m1 != m6);
        }

        [Test]
        public void DoTest1()
        {
            var m1 = new Maybe<string>("jason");
            var m2 = new Maybe<string>(new Exception("broke"));

            Assert.That("jason", Is.EqualTo(m1.Do(ifSuccess: v => v, ifException: null)));
            Assert.That("wat!?", Is.EqualTo(m2.Do(ifSuccess: null, ifException: err => "wat!?")));
            Assert.That(() => m2.Do(null, null), Throws.TypeOf<ArgumentException>());
            Assert.That(() => m1.Do(null, null), Throws.TypeOf<ArgumentException>());
        }
    }
}