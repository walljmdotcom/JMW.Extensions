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
            "12".MaybeToInt32().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToInt32().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToInt32().Do(val => val, null);
            var v2 = "blah".MaybeToInt32().Do(null, val => -1);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(-1));
        }

        [Test]
        public void ToInt16Test()
        {
            "12".MaybeToInt16().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToInt16().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToInt16().Do(val => val, null);
            var v2 = "blah".MaybeToInt16().Do(null, val => -1);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(-1));
        }

        [Test]
        public void ToInt64Test()
        {
            "12".MaybeToInt64().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToInt64().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToInt64().Do(val => val, null);
            var v2 = "blah".MaybeToInt64().Do(null, val => -1);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(-1));
        }

        [Test]
        public void ToUInt32Test()
        {
            "12".MaybeToUInt32().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToUInt32().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToUInt32().Do(val => val, null);
            var v2 = "blah".MaybeToUInt32().Do(null, val => 0);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(0));
        }

        [Test]
        public void ToUInt16Test()
        {
            "12".MaybeToUInt16().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToUInt16().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToUInt16().Do(val => val, null);
            var v2 = "blah".MaybeToUInt16().Do(null, val => 0);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(0));
        }

        [Test]
        public void ToUInt64Test()
        {
            "12".MaybeToUInt64().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToUInt64().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToUInt64().Do(val => val, null);
            var v2 = "blah".MaybeToUInt64().Do(null, val => 0);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(0));
        }

        [Test]
        public void ToDecimalTest()
        {
            "12".MaybeToDecimal().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToDecimal().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToDecimal().Do(val => val, null);
            var v2 = "blah".MaybeToDecimal().Do(null, val => -1);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(-1));
        }

        [Test]
        public void ToFloatTest()
        {
            "12".MaybeToFloat().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToFloat().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToFloat().Do(val => val, null);
            var v2 = "blah".MaybeToFloat().Do(null, val => -1);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(-1));
        }

        [Test]
        public void ToDoubleTest()
        {
            "12".MaybeToDouble().Do(v => Assert.That(v, Is.EqualTo(12)), null);
            "blah".MaybeToDouble().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
                });

            var v1 = "12".MaybeToDouble().Do(val => val, null);
            var v2 = "blah".MaybeToDouble().Do(null, val => -1);

            Assert.That(v1, Is.EqualTo(12));
            Assert.That(v2, Is.EqualTo(-1));
        }

        [Test]
        public void ToBooleanTest()
        {
            "1".MaybeToBoolean().Do(v => Assert.That(v, Is.EqualTo(true)), null);
            "blah".MaybeToBoolean().Do(null,
                v =>
                {
                    Assert.That(v.GetType(), Is.EqualTo(typeof(ArgumentException)));
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

            Assert.That(v1, Is.EqualTo(true));
            Assert.That(v2, Is.EqualTo(true));
            Assert.That(v3, Is.EqualTo(true));
            Assert.That(v4, Is.EqualTo(true));
            Assert.That(v5, Is.EqualTo(true));
            Assert.That(v6, Is.EqualTo(true));
            Assert.That(v7, Is.EqualTo(true));
            Assert.That(v8, Is.EqualTo(true));
            Assert.That(v9, Is.EqualTo(true));
            Assert.That(v10, Is.EqualTo(true));
            Assert.That(v11, Is.EqualTo(true));
            Assert.That(v12, Is.EqualTo(true));

            Assert.That(v13, Is.EqualTo(false));
            Assert.That(v14, Is.EqualTo(false));
            Assert.That(v15, Is.EqualTo(false));
            Assert.That(v16, Is.EqualTo(false));
            Assert.That(v17, Is.EqualTo(false));
            Assert.That(v18, Is.EqualTo(false));
            Assert.That(v19, Is.EqualTo(false));
            Assert.That(v20, Is.EqualTo(false));
            Assert.That(v21, Is.EqualTo(false));
            Assert.That(v22, Is.EqualTo(false));
            Assert.That(v23, Is.EqualTo(false));
            Assert.That(v24, Is.EqualTo(false));
            Assert.That(v25, Is.EqualTo(false));
        }
    }
}