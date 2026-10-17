using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace JMW.Collections.Tests
{
    [TestFixture]
    public class IndexedCollectionTest
    {
        [Test]
        public void Test1()
        {
            var idx = new IndexedCollection<Foo>();
            var f1 = new Foo { Bar = "jason", BarCollection = ["one", "two"], Baz = "me", BazCollection = [1, 2] };
            var f2 = new Foo { Bar = "wall", BarCollection = ["three", "two"], Baz = "me", BazCollection = [3, 4] };
            idx.Add(f1);
            idx.Add(f2);

            Assert.That(idx[0].Bar == "jason");
            Assert.That(idx[1].Bar == "wall");

            idx[0].Bar = "jeremy";

            Assert.That(idx[0].Bar == "jeremy");
            idx[0].Bar = "jeremy";
            Assert.That(idx[0].Bar == "jeremy");

            idx[0] = f1;

            var arr = new Foo[10];
            idx.CopyTo(arr, 1);
            Assert.That(arr[1] == idx[0]);
            Assert.That(arr[2] == idx[1]);

            Assert.That(idx.Contains(f1));

            f1.Baz = "my";
            f1.BazCollection = [1, 2, 5];
            f1.BarCollection = ["1", "2"];
            f1.BarCollection = ["one", "two"];

            Assert.That(idx.Count == 2);

            Assert.That(idx.Equals(idx));

            Assert.That(idx.First() == f1);
            Assert.That(idx.IndexOf(f2) == 1);

            var indices = idx.MaybeGetUniqueIndex(p => p.Bar);
            var indices2 = idx.MaybeGetIndex(p => p.BarCollection);

            indices.Do(ifSuccess: d => Assert.That(d.Count == 2), ifException: null);
            indices2.Do(ifSuccess: d => Assert.That(d.Count == 3), ifException: null);

            // make sure the indices can be modified without modifying the original.
            indices.Do(ifSuccess: d => d.Remove("jeremy"), ifException: null);
            idx.MaybeGetUniqueIndex(p => p.Bar).Do(ifSuccess: d => Assert.That(d.Count == 2), ifException: null);

            Assert.That(idx.First() == f1);
            Assert.That(idx.First(d => d.Bar == "jeremy") == f1);
            idx.MaybeGetByUniqueIndex(p => p.Bar, "jeremy").Do(ifSuccess: d => Assert.That(d == f1), ifException: null);
            idx.MaybeGetByIndex(p => p.BarCollection, "two").Do(ifSuccess: d => Assert.That(d.Count == 2), ifException: null);

            idx.MaybeGetByUniqueIndex(p => p.BarCollection, "jeremy").Do(ifSuccess: null, ifException: d => Assert.That(d.GetType() == typeof(ArgumentException)));
            idx.MaybeGetByIndex(p => p.Bar, "two").Do(ifSuccess: null, ifException: d => Assert.That(d.GetType() == typeof(ArgumentException)));

            var en = idx.GetEnumerator();
            while (en.MoveNext())
            {
                Assert.That(typeof(Foo) == en.Current.GetType());
            }

            idx.MaybeGetUniqueIndex(p => p.BarCollection).Do(ifSuccess: null, ifException: ex => Assert.That(ex.GetType() == typeof(ArgumentException)));
            idx.MaybeGetIndex(p => p.Bar).Do(ifSuccess: null, ifException: ex => Assert.That(ex.GetType() == typeof(ArgumentException)));

            idx.Remove(f1);
            idx.MaybeGetUniqueIndex(p => p.Bar).Do(ifSuccess: d => Assert.That(d.Count == 1), ifException: null);
            idx.MaybeGetIndex(p => p.BarCollection).Do(ifSuccess: d => Assert.That(d.Count == 2), ifException: null);
            idx.Add(f1);

            Assert.That(idx.TryGetByUniqueIndex(p => p.Bar, "jeremy", out var t1));
            Assert.That(t1 == f1);
            Assert.That(idx.TryGetByIndex(p => p.BarCollection, "one", out var t2));
            Assert.That(t2?[0] == f1);

            Assert.That(!idx.TryGetByUniqueIndex(p => p.BarCollection, "jeremy", out t1));
            Assert.That(!idx.TryGetByIndex(p => p.Bar, "one", out t2));

            idx.Remove(p => p.Bar == "jeremy");
            idx.MaybeGetUniqueIndex(p => p.Bar).Do(ifSuccess: d => Assert.That(d.Count == 1), ifException: null);
            idx.MaybeGetIndex(p => p.BarCollection).Do(ifSuccess: d => Assert.That(d.Count == 2), ifException: null);
            idx.Add(f1);

            idx.RemoveAt(0);
            idx.MaybeGetUniqueIndex(p => p.Bar).Do(ifSuccess: d => Assert.That(d.Count == 1), ifException: null);
            idx.MaybeGetIndex(p => p.BarCollection).Do(ifSuccess: d => Assert.That(d.Count == 2), ifException: null);
            idx.Insert(0, f1);

            try
            {
                idx.Add(f1);
            }
            catch (Exception ex)
            {
                Assert.That(ex.GetType() == typeof(OperationCanceledException));
            }
            idx.RefreshIndices();
            Assert.That(idx.TryAdd(f1) == false);
            idx.RefreshIndices();
            try
            {
                f1.BazCollection = [1, 2, 5, 5];
            }
            catch (Exception ex)
            {
                Assert.That(ex.GetType() == typeof(ArgumentException));
            }
            try
            {
                var f3 = new Foo { Bar = "wall", BarCollection = ["three", "two"], Baz = "me", BazCollection = [6, 7, 7] };
                idx.Add(f3);
            }
            catch
            (Exception ex)
            {
                Assert.That(ex.GetType() == typeof(OperationCanceledException));
            }
            f1.BarCollection = ["1", "2", "3"];

            f1.PropertyChanged += f1_PropertyChanged;
            f1.PropertyChanging += f1_PropertyChanging;
            f1.Bar = "blah";
            f1.PropertyChanged -= f1_PropertyChanged;
            f1.PropertyChanging -= f1_PropertyChanging;

            idx.Clear();
            Assert.That(idx.Count == 0);
            idx.MaybeGetUniqueIndex(p => p.Bar).Do(ifSuccess: d => Assert.That(d.Count == 0), ifException: null);

            idx.CollectionChanged += idx_CollectionChanged;
            idx.Add(f1);
            idx.CollectionChanged -= idx_CollectionChanged;
            Assert.That(idx.IsReadOnly == false);

            idx.Each(item => Assert.That(item.GetType() == typeof(Foo)));

            f1.PropertyChanged += f1_PropertyChanged;
            f1.PropertyChanging += f1_PropertyChanging;
            f1.IndexedPropertyChanged += f1_IndexedPropertyChanged;
            f1.ClearEvents();
            Assert.That(f1.IndexedPropertyChangedEventCount == 0);

            f1.PropertyChanged += f1_PropertyChanged;
            f1.ClearPropertyChanged();
            Assert.That(f1.PropertyChangedEventCount == 0);

            f1.PropertyChanging += f1_PropertyChanging;
            f1.ClearPropertyChanging();
            Assert.That(f1.PropertyChangingEventCount == 0);

            f1.IndexedPropertyChanged += f1_IndexedPropertyChanged;
            f1.ClearIndexedPropertyChanged();
            Assert.That(f1.IndexedPropertyChangedEventCount == 0);
        }

        [Test]
        public void Test2()
        {
            var cnt = 0;
            var msg = "";

            var idx = new IndexedCollection<Foo>();
            idx.IndexViolated += (o, e) => { cnt++;
                msg = e.ErrorMessage;
            };
            var f1 = new Foo { Bar = "jason", BarCollection = ["one", "two"], Baz = "me", BazCollection = [1, 2] };
            var f2 = new Foo { Bar = "wall", BarCollection = ["three", "two"], Baz = "me", BazCollection = [3, 4] };
            var f3 = new Foo { Bar = "wall", BarCollection = ["three", "two"], Baz = "me", BazCollection = [3, 4] };
            idx.Add(f1);
            idx.Add(f2);
            try
            {
                idx.Add(f3);
            }
            catch (OperationCanceledException ex)
            {
                Assert.That("Operation was cancelled due to an index violation. The Index may have been violated.", Is.EqualTo(ex.Message));
            }

            Assert.That(1,Is.EqualTo(cnt));
            Assert.That("Value violated index 'Bar' using key 'wall'", Is.EqualTo(msg));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private void f1_IndexedPropertyChanged(object? sender, IndexedPropertyChangedEventArgs e)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private void idx_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Assert.That(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private void f1_PropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            Assert.That(e.PropertyName == "Bar");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
        private void f1_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Assert.That(e.PropertyName == "Bar");
        }

        private class Foo : IndexedClass
        {
            private string bar = string.Empty;
            [Indexed(IsUnique = true)]
            public string Bar
            {
                get
                {
                    return bar;
                }

                set
                {
                    Set(ref bar, value);
                }
            }

            private List<string> barCollection = [];
            [Indexed]
            public List<string> BarCollection
            {
                get
                {
                    return barCollection;
                }

                set
                {
                    Set(ref barCollection, value);
                }
            }

            private string baz = string.Empty;
            [Indexed]
            public string Baz
            {
                get
                {
                    return baz;
                }

                set
                {
                    Set(ref baz, value);
                }
            }

            private List<int> bazCollection = [];
            [Indexed(IsUnique = true)]
            public List<int> BazCollection
            {
                get
                {
                    return bazCollection;
                }

                set
                {
                    Set(ref bazCollection, value);
                }
            }
        }
    }
}