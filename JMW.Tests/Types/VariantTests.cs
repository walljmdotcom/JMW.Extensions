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

        private static IEnumerable<TestCaseData> EqualityCases()
        {
            yield return new TestCaseData(new Variant("a"), new Variant("a"), true).SetName("strings equal");
            yield return new TestCaseData(new Variant("a"), new Variant("b"), false).SetName("strings differ");
            yield return new TestCaseData(new Variant("a"), new Variant("A"), false).SetName("strings differ by case");
            yield return new TestCaseData(new Variant(""), new Variant(""), true).SetName("empty strings equal");
            yield return new TestCaseData(new Variant(), new Variant(), true).SetName("empty variants equal");

            yield return new TestCaseData(V("a", "b"), V("a", "b"), true).SetName("lists equal");
            yield return new TestCaseData(V(), V(), true).SetName("empty lists equal");
            yield return new TestCaseData(V("a"), V("a", "b"), false).SetName("shorter list differs");
            yield return new TestCaseData(V("a", "b"), V("a"), false).SetName("longer list differs");
            yield return new TestCaseData(V(), V("a"), false).SetName("empty list differs from single");
            yield return new TestCaseData(V("a", "b"), V("b", "a"), false).SetName("list order matters");
            yield return new TestCaseData(V("a", "b"), V("a", "c"), false).SetName("list element differs");

            yield return new TestCaseData(D(("k", "v")), D(("k", "v")), true).SetName("dicts equal");
            yield return new TestCaseData(D(("k", "v"), ("j", "w")), D(("j", "w"), ("k", "v")), true).SetName("dict key order ignored");
            yield return new TestCaseData(D(), D(), true).SetName("empty dicts equal");
            yield return new TestCaseData(D(("k", "v")), D(("k", "v"), ("j", "w")), false).SetName("smaller dict differs");
            yield return new TestCaseData(D(("k", "v"), ("j", "w")), D(("k", "v")), false).SetName("larger dict differs");
            yield return new TestCaseData(D(), D(("k", "v")), false).SetName("empty dict differs from single");
            yield return new TestCaseData(D(("k", "v")), D(("x", "v")), false).SetName("dict key differs");
            yield return new TestCaseData(D(("k", "v")), D(("k", "x")), false).SetName("dict value differs");

            yield return new TestCaseData(new Variant("a"), V("a"), false).SetName("string differs from list");
            yield return new TestCaseData(V(), D(), false).SetName("empty list differs from empty dict");
            yield return new TestCaseData(new Variant(""), V(), false).SetName("empty string differs from empty list");
            yield return new TestCaseData(new Variant(), V(), false).SetName("empty variant differs from empty list");
            yield return new TestCaseData(new Variant(), new Variant(""), false).SetName("empty variant differs from empty string");
        }

        [TestCaseSource(nameof(EqualityCases))]
        public void EqualsAndHashCode(Variant a, Variant b, bool expected)
        {
            Assert.That(a.Equals(b), Is.EqualTo(expected));
            Assert.That(b.Equals(a), Is.EqualTo(expected));
            Assert.That(a.Equals((object)b), Is.EqualTo(expected));
            if (expected)
            {
                Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            }
        }

        private static Variant V(params string[] items)
        {
            var lst = new List<Variant>();
            foreach (var item in items)
            {
                lst.Add(item);
            }
            return lst;
        }

        private static Variant D(params (string Key, string Value)[] items)
        {
            var dict = new Dictionary<string, Variant>();
            foreach (var (key, value) in items)
            {
                dict.Add(key, value);
            }
            return dict;
        }
    }
}