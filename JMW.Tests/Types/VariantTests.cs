using NUnit.Framework;
using System.Collections.Generic;

namespace JMW.Types.Tests
{
    [TestFixture]
    public class VariantTests
    {
        [Test]
        public void VariantTest1()
        {
            var v1 = new Variant("jason");
            Assert.That(true, Is.EqualTo(v1.IsString));
            Assert.That("jason", Is.EqualTo(v1.StringValue));
            Assert.That("jason" == (string)v1, Is.True);

            Variant tv1 = "jason";
            Assert.That(true, Is.EqualTo(tv1.IsString));
            Assert.That("jason", Is.EqualTo(tv1.StringValue));
            v1.StringValue = "";
            Assert.That("", Is.EqualTo(v1.StringValue));
            Assert.That(v1.GetHashCode() > 0, Is.True);

            var v2 = new Variant(new List<Variant> { "jason" });
            Assert.That(true, Is.EqualTo(v2.IsList));
            Assert.That(new List<Variant> { "jason" }, Is.EqualTo(v2.ListValue));

            Variant tv2 = new List<Variant> { "jason" };
            Assert.That((List<Variant>?)tv2, Has.Count.EqualTo(1));
            Assert.That((List<string>?)tv2, Has.Count.EqualTo(1));
            Assert.That(tv2.IsList, Is.True);
            Assert.That(true, Is.EqualTo(tv2.IsList));
            Assert.That(new List<Variant> { "jason" }, Is.EqualTo(tv2.ListValue));
            Assert.That(tv2.Equals(new List<Variant> { "jason" }), Is.True);
            Assert.That("(List) Count: 1", Is.EqualTo(tv2.ToString()));
            Assert.That(tv2.Equals(null), Is.False);
            Assert.That(((Variant)new List<string> { "jason" }).IsList, Is.True);
            v2.ListValue = [];
            Assert.That(v2.ListValue, Is.Empty);
            Assert.That(v2.GetHashCode() > 0, Is.True);

            var v3 = new Variant(new Dictionary<string, Variant> { { "one", "jason" } });
            Assert.That(true, Is.EqualTo(v3.IsDict));
            Assert.That(new Dictionary<string, Variant> { { "one", "jason" } }, Is.EqualTo(v3.DictValue));

            Variant tv3 = new Dictionary<string, Variant> { { "one", "jason" } };
            Assert.That(true, Is.EqualTo(tv3.IsDict));
            Assert.That(new Dictionary<string, Variant> { { "one", "jason" } }, Is.EqualTo(tv3.DictValue));
            Assert.That(tv3.Equals(new Dictionary<string, Variant> { { "one", "jason" } }), Is.True);
            Assert.That(((Variant)new Dictionary<string, Variant> { { "one", "jason" } }).IsDict, Is.True);
            Assert.That((Dictionary<string, Variant>?)tv3, Has.Count.EqualTo(1));
            v3.DictValue = [];
            Assert.That(v3.DictValue, Is.Empty);
            Assert.That("(Dict) Count: 1", Is.EqualTo(tv3.ToString()));
            Assert.That(v3.GetHashCode() > 0, Is.True);

            var v = new Variant();
            Assert.That("", Is.EqualTo(v.ToString()));
        }
    }
}