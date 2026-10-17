using System;
using System.Linq;
using BenchmarkDotNet.Running;
using JMW.Collections;

namespace JMW.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--footprint"))
        {
            Footprint();
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    // Bytes retained per item: the collection with its items, minus the items alone.
    private static void Footprint()
    {
        const int m = 100_000;
        Console.WriteLine($"empty Device:     {Measure(() => Enumerable.Range(0, m).Select(_ => new Device()).ToArray()) / m} B/item");
        Console.WriteLine($"populated Device: {Measure(() => Enumerable.Range(0, m).Select(Device.Create).ToArray()) / m} B/item");
        Console.WriteLine($"its strings+tags: {Measure(() => Enumerable.Range(0, m).Select(i => (object)new object[] { Device.IdFor(i), Device.SiteFor(i), "Device number " + i, new System.Collections.Generic.List<string> { "tag-" + (i % 20), "tag-" + ((i + 7) % 20), "tag-" + ((i + 13) % 20) } }).ToArray()) / m} B/item (includes a 56 B holder array)");

        foreach (var n in new[] { 1_000, 10_000, 100_000 })
        {
            var itemsOnly = Measure(() => Enumerable.Range(0, n).Select(Device.Create).ToArray());
            var withCollection = Measure(() =>
            {
                var c = new IndexedCollection<Device>();
                for (var i = 0; i < n; i++)
                    c.Add(Device.Create(i));
                return c;
            });

            Console.WriteLine(
                $"N={n,7}: items alone {itemsOnly / n,6} B/item, in collection {withCollection / n,6} B/item, collection overhead {(withCollection - itemsOnly) / n,6} B/item"
            );
        }
    }

    private static long Measure(Func<object> build)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);

        var keep = build();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var after = GC.GetTotalMemory(forceFullCollection: true);
        GC.KeepAlive(keep);
        return after - before;
    }
}
