using System.Collections.Generic;
using JMW.Extensions.Reflection;
using NUnit.Framework;
using System.Linq;
using JMW.Reflection;
using System;
using JMW.Collections;
using JMW.Extensions.Enumerable;

namespace JMW.Extensions.Reflection.Tests
{
    [TestFixture]
    public class ReflectionExtensions
    {
        [Test]
        public void GetPropertyNameTest1()
        {
            Assert.That("Prop1" == Linq.GetPropertyName<Test1>(p => p.Prop1));

            try
            {
                Linq.GetPropertyName<Test1>(p => p.Prop1 + 1);
            }
            catch(Exception ex)
            {
                Assert.That(ex.GetType() == typeof(ArgumentException));
            }
        }

        [Test]
        public void GetPropertiesByAttributeTest1()
        {
            var obj = new Test1 { Prop1 = "Jason" };
            var props = obj.GetType().GetPropertiesByAttribute<Indexed>();
            Assert.That(props.Count() == 1);
            Assert.That(props.First().Name == "Prop1");
            Assert.That(obj.Prop1 == "Jason");
        }

        [Test]
        public void HasInterfaceTest1()
        {
            var obj = new Test1 { Prop1 = "Jason" };
            var t = obj.GetType().HasInterface<ITest>();
            Assert.That(t);
        }

        [Test]
        public void SafeAddTest1()
        {
            var dict = new Dictionary<string, List<string>>();
            dict.SafeAdd("key1", "v1");
            dict.SafeAdd("key2", "v2");
            dict.SafeAdd("key1", "v2");

            Assert.That(dict.Count == 2);
            Assert.That(dict["key1"].Count == 2);
            Assert.That(dict["key2"].Count == 1);
        }

        [Test]
        public void AddIfNotPresentTest1()
        {
            var dict = new Dictionary<string, string>();
            dict.AddIfNotPresent("key1", "v1");
            dict.AddIfNotPresent("key2", "v2");
            dict.AddIfNotPresent("key1", "v2");

            Assert.That(dict.Count == 2);
            Assert.That(dict["key1"] == "v1");
            Assert.That(dict["key2"] == "v2");
        }

        [Test]
        public void GetValueTest1()
        {
            var obj = new Test1 { Prop1 = "Jason" };
            var t = obj.GetType().GetProperty("Prop1") ?? throw new InvalidOperationException("Prop1 not found.");
            var v = t.GetValue(obj);
            Assert.That(v?.ToString() == "Jason");
            Assert.That(t.GetValue<string>(obj) == "Jason");
        }

        [Test]
        public void CastDelegateTest1()
        {
            Jason d1 =
                s => string.Empty;

            var d2 = d1.CastDelegate<Jason>();
            
            Assert.That(d1(string.Empty) == d2(string.Empty));
        }

        private delegate string Jason(string one);

        private interface ITest
        {
            string Prop1 { get; set; }
        }

        private class Test1 : ITest
        {
            [Indexed]
            public string Prop1 { get; set; } = string.Empty;
        }
    }
}