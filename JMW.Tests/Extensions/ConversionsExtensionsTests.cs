using NUnit.Framework;
using System;

namespace JMW.Extensions.Tests
{
    [TestFixture]
    public class ConversionsExtensionsTests
    {
        [Test]
        public void ToInt32Test()
        {
            "12".MaybeToInt32().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToInt32().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToInt32().Do(val => val, null);
            var v2 = "blah".MaybeToInt32().Do(null, val => -1);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(-1, Is.EqualTo(v2));
        }

        [Test]
        public void ToInt16Test()
        {
            "12".MaybeToInt16().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToInt16().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToInt16().Do(val => val, null);
            var v2 = "blah".MaybeToInt16().Do(null, val => -1);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(-1, Is.EqualTo(v2));
        }

        [Test]
        public void ToInt64Test()
        {
            "12".MaybeToInt64().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToInt64().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToInt64().Do(val => val, null);
            var v2 = "blah".MaybeToInt64().Do(null, val => -1);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(-1, Is.EqualTo(v2));
        }

        [Test]
        public void ToUInt32Test()
        {
            "12".MaybeToUInt32().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToUInt32().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToUInt32().Do(val => val, null);
            var v2 = "blah".MaybeToUInt32().Do(null, val => 0);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(0, Is.EqualTo(v2));
        }

        [Test]
        public void ToUInt16Test()
        {
            "12".MaybeToUInt16().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToUInt16().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToUInt16().Do(val => val, null);
            var v2 = "blah".MaybeToUInt16().Do(null, val => 0);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(0, Is.EqualTo(v2));
        }

        [Test]
        public void ToUInt64Test()
        {
            "12".MaybeToUInt64().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToUInt64().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToUInt64().Do(val => val, null);
            var v2 = "blah".MaybeToUInt64().Do(null, val => 0);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(0, Is.EqualTo(v2));
        }

        [Test]
        public void ToDecimalTest()
        {
            "12".MaybeToDecimal().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToDecimal().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToDecimal().Do(val => val, null);
            var v2 = "blah".MaybeToDecimal().Do(null, val => -1);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(-1, Is.EqualTo(v2));
        }

        [Test]
        public void ToFloatTest()
        {
            "12".MaybeToFloat().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToFloat().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToFloat().Do(val => val, null);
            var v2 = "blah".MaybeToFloat().Do(null, val => -1);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(-1, Is.EqualTo(v2));
        }

        [Test]
        public void ToDoubleTest()
        {
            "12".MaybeToDouble().Do(v => Assert.That(12, Is.EqualTo(v)), null);
            "blah".MaybeToDouble().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "12".MaybeToDouble().Do(val => val, null);
            var v2 = "blah".MaybeToDouble().Do(null, val => -1);

            Assert.That(12, Is.EqualTo(v1));
            Assert.That(-1, Is.EqualTo(v2));
        }

        [Test]
        public void ToBooleanTest()
        {
            "1".MaybeToBoolean().Do(v => Assert.That(true, Is.EqualTo(v)), null);
            "blah".MaybeToBoolean().Do(null,
                v =>
                {
                    Assert.That(typeof(ArgumentException), Is.EqualTo(v.GetType()));
                });

            var v1 = "1".MaybeToBoolean().Do(val => val, null);
            var v2 = "yes".MaybeToBoolean().Do(val => val, null);
            var v3 = "YES".MaybeToBoolean().Do(val => val, null);
            var v4 = "Yes".MaybeToBoolean().Do(val => val, null);
            var v5 = "on".MaybeToBoolean().Do(val => val, null);
            var v6 = "ON".MaybeToBoolean().Do(val => val, null);
            var v7 = "On".MaybeToBoolean().Do(val => val, null);
            var v8 = "true".MaybeToBoolean().Do(val => val, null);
            var v9 = "TRUE".MaybeToBoolean().Do(val => val, null);
            var v10 = "True".MaybeToBoolean().Do(val => val, null);
            var v11 = "T".MaybeToBoolean().Do(val => val, null);
            var v12 = "t".MaybeToBoolean().Do(val => val, null);

            var v13 = "blah".MaybeToBoolean().Do(null, val => false);
            var v14 = "0".MaybeToBoolean().Do(val => val, null);
            var v15 = "no".MaybeToBoolean().Do(val => val, null);
            var v16 = "NO".MaybeToBoolean().Do(val => val, null);
            var v17 = "No".MaybeToBoolean().Do(val => val, null);
            var v18 = "False".MaybeToBoolean().Do(val => val, null);
            var v19 = "false".MaybeToBoolean().Do(val => val, null);
            var v20 = "FALSE".MaybeToBoolean().Do(val => val, null);
            var v21 = "off".MaybeToBoolean().Do(val => val, null);
            var v22 = "Off".MaybeToBoolean().Do(val => val, null);
            var v23 = "OFF".MaybeToBoolean().Do(val => val, null);
            var v24 = "f".MaybeToBoolean().Do(val => val, null);
            var v25 = "F".MaybeToBoolean().Do(val => val, null);

            Assert.That(true, Is.EqualTo(v1));
            Assert.That(true, Is.EqualTo(v2));
            Assert.That(true, Is.EqualTo(v3));
            Assert.That(true, Is.EqualTo(v4));
            Assert.That(true, Is.EqualTo(v5));
            Assert.That(true, Is.EqualTo(v6));
            Assert.That(true, Is.EqualTo(v7));
            Assert.That(true, Is.EqualTo(v8));
            Assert.That(true, Is.EqualTo(v9));
            Assert.That(true, Is.EqualTo(v10));
            Assert.That(true, Is.EqualTo(v11));
            Assert.That(true, Is.EqualTo(v12));

            Assert.That(false, Is.EqualTo(v13));
            Assert.That(false, Is.EqualTo(v14));
            Assert.That(false, Is.EqualTo(v15));
            Assert.That(false, Is.EqualTo(v16));
            Assert.That(false, Is.EqualTo(v17));
            Assert.That(false, Is.EqualTo(v18));
            Assert.That(false, Is.EqualTo(v19));
            Assert.That(false, Is.EqualTo(v20));
            Assert.That(false, Is.EqualTo(v21));
            Assert.That(false, Is.EqualTo(v22));
            Assert.That(false, Is.EqualTo(v23));
            Assert.That(false, Is.EqualTo(v24));
            Assert.That(false, Is.EqualTo(v25));
        }
    }
}